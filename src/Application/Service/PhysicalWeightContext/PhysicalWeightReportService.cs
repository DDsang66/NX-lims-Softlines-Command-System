using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.PhysicalWeightContext;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Application.Interface.PhysicalWeightContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Service.PhysicalWeightContext;

/// <summary>
/// 物理克重报告生成 — 用 PHY_Weight.docx 模板填充测量数据。
/// 业务计算(分组/换算/平均)在本层, OpenXml 填充委托给 IPhysicalWeightDocxEngine。
/// </summary>
public class PhysicalWeightReportService : IPhysicalWeightReportService, IScopedDependency
{
    public const string TypeArea = "area";
    public const string TypeLength = "length";
    public const string TypePiece = "piece";

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

        string fileName = $"{dto.ReportNumber}_{DateTime.Now:yyMMddHHmmss}_PHY_Weight.docx";
        // 模板已移入 Common_PHY/ (与干燥速率等共用目录); 新模板表1 带 Measure 列
        string targetPath = _fileStorage.CopyTemplate(
            Path.Combine("DocxModel", "Common_PHY", "PHY_Weight.docx"),
            Path.Combine("DocxModel", "SaveDocx"),
            fileName);

        // 表0 汇总网格: 每测点一行(两种单位均值)
        var summaryRows = dto.Records
            .GroupBy(r => r.Point?.Trim() ?? "")
            .Select(g => new PhysicalWeightSummaryRowModel
            {
                Point = string.IsNullOrEmpty(g.Key) ? (g.First().SampleId ?? "-") : g.Key,
                Value1 = g.Average(r => Value1Of(r, dto.TestType)),
                Value2 = g.Average(r => Value2Of(r, dto.TestType))
            })
            .ToList();

        // 表1 数据行: 按测点分组(保持首次出现顺序), 值=类型主单位
        var groups = dto.Records
            .GroupBy(r => r.Point?.Trim() ?? "")
            .Select(g => new
            {
                Point = string.IsNullOrEmpty(g.Key) ? (g.First().SampleId ?? "-") : g.Key,
                Records = g.ToList()
            })
            .ToList();

        // 同测点每 5 条一行: 超过 5 条拆到下一行 (新行仍标同一测点)。
        // Measure 列显示文本见下方循环(尺寸文本优先, 无则退回面积/长度数值)。
        var rows = new List<PhysicalWeightReportRowModel>();
        foreach (var g in groups)
        {
            for (int offset = 0; offset < g.Records.Count; offset += 5)
            {
                var chunk = g.Records.Skip(offset).Take(5).ToList();

                // Measure 显示文本: 优先用前端尺寸文本(长×宽模式 "5×5"); 该行各条通常同尺寸, 取首个非空。
                // 没有尺寸文本时退回数值: 面积直填→Area cm² / 长度→LengthCm cm(条重无 → 留空)。
                string? measure = chunk.Select(r => r.Dimension).FirstOrDefault(d => !string.IsNullOrWhiteSpace(d));
                if (measure == null)
                {
                    var src = chunk.FirstOrDefault(r => DefaultMeasureValue(r, dto.TestType).HasValue);
                    var mv = src == null ? null : DefaultMeasureValue(src, dto.TestType);
                    if (mv.HasValue) measure = mv.Value.ToString("F2");
                }

                rows.Add(new PhysicalWeightReportRowModel
                {
                    Point = g.Point,
                    Measure = measure,
                    Values = chunk.Select(r => ToDataValue(r, dto.TestType)).ToList(),
                    Average = chunk.Count > 0 ? chunk.Average(r => ToDataValue(r, dto.TestType)) : null
                });
            }
        }

        var model = new PhysicalWeightReportFillModel
        {
            ReportNumber = dto.ReportNumber,
            TestMethod = dto.TestMethod,
            TestType = dto.TestType,
            DataUnit = DataUnitOf(dto.TestType),
            EnvironmentTemperature = dto.EnvironmentTemperature,
            EnvironmentHumidity = dto.EnvironmentHumidity,
            SummaryRows = summaryRows,
            Rows = rows
        };

        try
        {
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

    /// <summary>Measure 列退回数值源: 面积→Area cm², 长度→LengthCm cm, 条重→无(无 Dimension 文本时用)</summary>
    private static decimal? DefaultMeasureValue(PhysicalWeightReportRecordDto r, string type) => type switch
    {
        TypeArea => r.Area,
        TypeLength => r.LengthCm,
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
