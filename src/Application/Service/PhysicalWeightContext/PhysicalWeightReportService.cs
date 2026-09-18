using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.PhysicalWeightContext;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Application.Interface.PhysicalWeightContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Service.PhysicalWeightContext;

/// <summary>
/// 物理克重报告生成 — 按买家选对应模板(Normal→PHY_Weight.docx / Adidas / FOCUS / NEXT)填充测量数据。
/// 业务计算(分组/换算/平均)在本层, OpenXml 填充委托给 IPhysicalWeightDocxEngine。
/// </summary>
public class PhysicalWeightReportService : IPhysicalWeightReportService, IScopedDependency
{
    public const string TypeArea = "area";
    public const string TypeLength = "length";
    public const string TypePiece = "piece";

    /// <summary>
    /// NEXT 单块试样的固定面积 cm² —— 客户方法里试样就是 100 cm², 前端那个面积框在 NEXT 下也固定成这个值,
    /// 所以这里取常数而不是读记录里的 Area(见下面 Ave(g/m²) 的算法)。
    /// </summary>
    private const decimal NextSpecimenArea = 100m;

    private readonly IPhysicalWeightDocxEngine _engine;
    private readonly IFileStorageService _fileStorage;

    public PhysicalWeightReportService(IPhysicalWeightDocxEngine engine, IFileStorageService fileStorage)
    {
        _engine = engine;
        _fileStorage = fileStorage;
    }

    public Result<DocxUrlResponseDto> Generate(PhysicalWeightReportRequestDto dto)
    {
        if (dto == null || dto.Records == null || dto.Records.Count == 0)
            return Result<DocxUrlResponseDto>.Fail("无称重数据");
        if (string.IsNullOrWhiteSpace(dto.ReportNumber))
            return Result<DocxUrlResponseDto>.Fail("报告号不能为空");
        if (!IsSupportedType(dto.TestType))
            return Result<DocxUrlResponseDto>.Fail("不支持的测试类型: " + dto.TestType);

        // 买家白名单 → 模板所在目录 + 文件名。前端传的值永不进 Path.Combine:
        // FileStorageService.CopyTemplate 对模板路径零校验且直接 File.Copy, 拼进去就是路径穿越。
        var template = TemplateOf(dto.Buyer);
        if (template == null)
            return Result<DocxUrlResponseDto>.Fail("不支持的买家: " + dto.Buyer);
        if (!SupportsTestType(dto.Buyer, dto.TestType))
            return Result<DocxUrlResponseDto>.Fail($"{dto.Buyer} 报告模板不支持 {dto.TestType} 类型测试");

        string fileName = $"{dto.ReportNumber}_{DateTime.Now:yyMMddHHmmss}_PHY_Weight.docx";

        // NEXT 的模板里两套表并存, 行**按录入方式**各归各的(一份报告里可能两种都有 —— 操作员录到一半
        // 换过开关):
        //   长×宽录的行(Dimension 非空) → 数据表 #1~#5 逐条 + 3 格登记表(每次测量一行, 尺寸写 "6.5×6.4");
        //   直接填面积录的行            → 每测点汇总表(样品数 / 重量合计 / g/m² 均值)。
        // 其余买家不分模式: 数据表与 3 格登记表都取全部行。
        var detailRecords = dto.Buyer == PhysicalWeightReportRequestDto.BuyerNext
            ? dto.Records.Where(r => !string.IsNullOrWhiteSpace(r.Dimension)).ToList()
            : dto.Records;

        // 表0 汇总网格: 每测点一行(两种单位均值) —— 这张表不分模式, 全部记录都算
        var summaryRows = dto.Records
            .GroupBy(r => r.Point?.Trim() ?? "")
            .Select(g => new PhysicalWeightSummaryRowModel
            {
                Point = string.IsNullOrEmpty(g.Key) ? (g.First().SampleId ?? "-") : g.Key,
                Value1 = g.Average(r => Value1Of(r, dto.TestType)),
                Value2 = g.Average(r => Value2Of(r, dto.TestType)),
                Value3 = g.Average(r => Value3Of(r, dto.Buyer, dto.TestType)),
                // 布边长度(仅 FOCUS 的 Sample | Selvage Length 表用): 每测点一行, 同测点各条取第一条有值的
                SelvageLength = g.Select(r => r.LengthCm).FirstOrDefault(v => v.HasValue)
            })
            .ToList();

        // NEXT 每测点汇总表: 只统计"直接填面积"录的行(其余买家没有这张表, 引擎按买家跳过)。
        // Ave(g/m²) 取的是**池化**口径, 不是各条 g/m² 再平均:
        //   一次称重称了 N 块, 重量格录的是这 N 块的总重, 单块固定 100 cm²
        //   → 总面积 = 样品数合计 × 100 cm²
        //   → Ave = 重量合计 ÷ 总面积 × 10000   (= 重量合计 ÷ 样品数合计 × 100)
        // 等权平均("称 3 块"和"称 1 块"权重相同)在样品数不一时会与这个数对不上。
        var perPointRows = dto.Records
            .Where(r => string.IsNullOrWhiteSpace(r.Dimension))
            .GroupBy(r => r.Point?.Trim() ?? "")
            .Select(g => new PhysicalWeightPerPointRowModel
            {
                Point = string.IsNullOrEmpty(g.Key) ? (g.First().SampleId ?? "-") : g.Key,
                SampleCount = g.Sum(r => r.SampleCount ?? 1),
                TotalWeight = g.Sum(r => r.Weight),
                AverageGsm = g.Sum(r => r.Weight ?? 0m)
                             / (g.Sum(r => r.SampleCount ?? 1) * NextSpecimenArea) * 10000m
            })
            .ToList();

        // 表1 数据行: 按测点分组(保持首次出现顺序), 值=类型主单位
        var groups = detailRecords
            .GroupBy(r => r.Point?.Trim() ?? "")
            .Select(g => new
            {
                Point = string.IsNullOrEmpty(g.Key) ? (g.First().SampleId ?? "-") : g.Key,
                Records = g.ToList()
            })
            .ToList();

        // 同测点每 5 条一行: 超过 5 条拆到下一行 (新行仍标同一测点)。
        var rows = new List<PhysicalWeightReportRowModel>();
        foreach (var g in groups)
        {
            for (int offset = 0; offset < g.Records.Count; offset += 5)
            {
                var chunk = g.Records.Skip(offset).Take(5).ToList();
                rows.Add(new PhysicalWeightReportRowModel
                {
                    Point = g.Point,
                    Values = chunk.Select(r => ToDataValue(r, dto.TestType)).ToList(),
                    Average = chunk.Count > 0 ? chunk.Average(r => ToDataValue(r, dto.TestType)) : null
                });
            }
        }

        // 表2 文档末登记: 每次测量一行(模板 2026-09-10 改为 3 格: Sample | Weight (g) | 尺寸):
        //   试样测点 | 重量(g) | 尺寸(面积/长度/条数)。按行源原序即前端行序。
        //   area 长×宽录入 → 第3列直写尺寸文本 "5×5"(MeasureText), 不再显示换算面积 cm²。
        var trailerRows = detailRecords.Select(r => new PhysicalWeightTrailerRowModel
        {
            Sample = r.Point?.Trim() ?? "",
            Weight = r.Weight,
            Measure = TrailerMeasureOf(r, dto.TestType),
            MeasureText = dto.TestType == TypeArea && !string.IsNullOrWhiteSpace(r.Dimension) ? r.Dimension : null
        }).ToList();

        var model = new PhysicalWeightReportFillModel
        {
            ReportNumber = dto.ReportNumber,
            TestMethod = dto.TestMethod,
            Buyer = dto.Buyer,
            TestType = dto.TestType,
            DataUnit = DataUnitOf(dto.TestType),
            EnvironmentTemperature = dto.EnvironmentTemperature,
            EnvironmentHumidity = dto.EnvironmentHumidity,
            SummaryRows = summaryRows,
            PerPointRows = perPointRows,
            Rows = rows,
            TrailerRows = trailerRows
        };

        // 拷贝模板也在 try 内: 买家一多, "选到一份没放好的模板"就是现实故障路径。
        // 放在外面时 FileNotFoundException 会直穿成 500(前端只看到"网络错误"), 放进来才是可读的 Result.Fail。
        //
        // 模板位置见 TemplateOf: Normal 与干燥速率等共用 Common_PHY/, 三个买家各有自己的目录
        // (Adidas_PHY / Focus_PHY / Next_PHY —— 买家模板由客户单独维护, 混在一个目录里容易串)。
        // 报告按月归档: SaveDocx/Weight{yyyyMM}/ —— 单月报告多了以后 SaveDocx 根目录会被塞爆, 按月分便于清理/查找。
        // 目录不存在时 CopyTemplate 会自建; 下载侧(PhysicalWeightReportController)按月目录 → 根目录顺序找回。
        string? targetPath = null;
        try
        {
            targetPath = _fileStorage.CopyTemplate(
                Path.Combine("DocxModel", template.Value.Dir, template.Value.File),
                Path.Combine("DocxModel", "SaveDocx", "Weight" + DateTime.Now.ToString("yyyyMM")),
                fileName);

            _engine.FillReport(targetPath, model);
        }
        catch (Exception ex)
        {
            return Result<DocxUrlResponseDto>.Fail("生成报告失败: " + ex.Message);
        }

        return Result<DocxUrlResponseDto>.Ok(new DocxUrlResponseDto
        {
            fileKey = fileName,
            fileName = fileName,
            downloadUrl = $"/api/PhysicalWeightReport/{fileName}/download"
        });
    }

    private static bool IsSupportedType(string type) => type switch
    {
        TypeArea or TypeLength or TypePiece => true,
        _ => false
    };

    /// <summary>
    /// 买家 → 模板在 DocxModel 下的 [目录, 文件名]。返回 null 即非法买家。
    /// 只有本方法的字符串常量会进 Path.Combine —— 前端传的买家值先过这道白名单。
    /// Normal 与干燥速率等共用 Common_PHY/; 三个买家模板各自一个目录, 客户改模板时不会互相碰到。
    /// </summary>
    private static (string Dir, string File)? TemplateOf(string buyer) => buyer switch
    {
        PhysicalWeightReportRequestDto.BuyerNormal => ("Common_PHY", "PHY_Weight.docx"),
        PhysicalWeightReportRequestDto.BuyerAdidas => ("Adidas_PHY", "PHY_Weight - ADI.docx"),
        PhysicalWeightReportRequestDto.BuyerFocus => ("Focus_PHY", "PHY_Weight - FOCUS.docx"),
        PhysicalWeightReportRequestDto.BuyerNext => ("Next_PHY", "PHY_Weight - NEXT.docx"),
        _ => null
    };

    /// <summary>
    /// 买家支持的测试类型 —— 与各模板表0 的实际列数一一对应。
    /// 新三家模板里既没有 oz/yd 也没有条重的列, 只做面积克重: 硬发一份类型不匹配的请求过去,
    /// 引擎会因格数不够而静默丢列, 所以这里先拒掉。
    /// </summary>
    private static bool SupportsTestType(string buyer, string type) => (buyer, type) switch
    {
        (PhysicalWeightReportRequestDto.BuyerNormal, _) => IsSupportedType(type),
        (PhysicalWeightReportRequestDto.BuyerAdidas
            or PhysicalWeightReportRequestDto.BuyerFocus
            or PhysicalWeightReportRequestDto.BuyerNext, TypeArea) => true,
        _ => false
    };

    /// <summary>表0 汇总第一种单位值(主单位)</summary>
    private static decimal Value1Of(PhysicalWeightReportRecordDto r, string type) => type switch
    {
        TypeArea => r.Gsm,
        TypeLength => r.GPerM,
        TypePiece => r.GPerPiece,
        _ => 0
    };

    /// <summary>表0 汇总第二种单位值(副单位)</summary>
    private static decimal Value2Of(PhysicalWeightReportRecordDto r, string type) => type switch
    {
        TypeArea => r.Oz,
        TypeLength => r.OzPerYd,
        TypePiece => r.LbPerDozen,
        _ => 0
    };

    /// <summary>
    /// 表0 汇总第三种单位值: 条重→oz/dozen; FOCUS 面积→g/m(汇总表第三格, 由面积卡片里新加的长度框算出);
    /// 其余买家/类型没有第三列, 不用。
    /// </summary>
    private static decimal Value3Of(PhysicalWeightReportRecordDto r, string buyer, string type) => (buyer, type) switch
    {
        (PhysicalWeightReportRequestDto.BuyerFocus, TypeArea) => r.GPerM,
        (_, TypePiece) => r.OzPerDozen,
        _ => 0
    };

    /// <summary>表2 第5列尺寸值: 面积→面积cm², 长度→长度cm, 条重→称重条数(与导出原始数据表第5列一致)</summary>
    private static decimal? TrailerMeasureOf(PhysicalWeightReportRecordDto r, string type) => type switch
    {
        TypeArea => r.Area,
        TypeLength => r.LengthCm,
        TypePiece => r.PieceCount,
        _ => null
    };

    /// <summary>表1 数据值 = 类型主单位(面积→g/m², 长度→g/m, 条重→g/piece)</summary>
    private static decimal ToDataValue(PhysicalWeightReportRecordDto r, string type) => type switch
    {
        TypeArea => r.Gsm,
        TypeLength => r.GPerM,
        TypePiece => r.GPerPiece,
        _ => 0
    };

    /// <summary>表1 表头单位文字</summary>
    private static string DataUnitOf(string type) => type switch
    {
        TypeArea => "g/m²",
        TypeLength => "g/m",
        TypePiece => "g/piece",
        _ => "g/m²"
    };
}
