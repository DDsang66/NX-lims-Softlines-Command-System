using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.YarnCountContext;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Application.Interface.YarnCountContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Interface;

namespace NX_lims_Softlines_Command_System.src.Application.Service.YarnCountContext;

/// <summary>
/// 纱支报告生成 — 按布种选模板填充(PHY_YarnCount_woven.docx / PHY_YarnCount_knit.docx)。
/// 业务计算(平均长度/取位/Tex/方向汇总)在本层, OpenXml 填充委托给 IYarnCountDocxEngine。
/// 不落库: 试样数据随请求体进来, 报告生成完就结束(与耐磨模块一致)。
/// </summary>
public class YarnCountReportService : IYarnCountReportService, IScopedDependency
{
    private readonly IYarnCountDocxEngine _engine;
    private readonly IFileStorageService _fileStorage;

    public YarnCountReportService(IYarnCountDocxEngine engine, IFileStorageService fileStorage)
    {
        _engine = engine;
        _fileStorage = fileStorage;
    }

    /// <summary>月度子目录名 —— 与 YarnCountReportController.ResolveReportFile 共用, 防两边写岔</summary>
    public static string MonthlyFolder() => "YarnCount" + DateTime.Now.ToString("yyyyMM");

    public Result<DocxUrlResponseDto> Generate(YarnCountReportRequestDto dto)
    {
        // 1. 基础校验
        if (dto == null)
            return Result<DocxUrlResponseDto>.Fail("请求数据为空");
        if (string.IsNullOrWhiteSpace(dto.ReportNumber))
            return Result<DocxUrlResponseDto>.Fail("报告号不能为空");
        // 报告号会进输出文件名, 带路径分隔符就能写到别处去
        if (dto.ReportNumber.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return Result<DocxUrlResponseDto>.Fail("报告号含非法文件名字符");
        if (dto.Directions == null || dto.Directions.Count == 0)
            return Result<DocxUrlResponseDto>.Fail("纱支试样数据不能为空");

        // 2. 布种白名单 —— 模板文件名与各方向列数只能来自服务端的口径表。
        //    IFileStorageService.CopyTemplate 对路径零校验(直接 File.Copy), 请求里那个字符串
        //    必须先在这里收敛成 Layout, 之后才允许进 Path.Combine。
        var layout = YarnCountFabricTypes.TryGet(dto.FabricType);
        if (layout == null)
            return Result<DocxUrlResponseDto>.Fail(
                $"不支持的布种: 「{dto.FabricType}」(只接受 {YarnCountFabricTypes.Woven} / {YarnCountFabricTypes.Knit})");

        // 3. 股数 —— 只进 ’s 那一格的算式, 但 0/负数会印出 (cc*0)/0 这种东西, 直接挡掉。
        //    不设上限: 填大了只是那一格印得长一点, 不会算错。
        if (dto.PlyCount < 1)
            return Result<DocxUrlResponseDto>.Fail($"股数必须是正整数, 收到 {dto.PlyCount}");

        // 4. 逐方向校验, 同时摊成模板列(读数先舍入再取平均 —— 报告上印的就是舍入后的数)
        var columns = new List<YarnCountColumnModel>();
        foreach (var dir in dto.Directions)
        {
            string? direction = NormalizeDirection(dir?.Direction);
            if (direction == null)
                return Result<DocxUrlResponseDto>.Fail(
                    $"方向取值非法: 「{dir?.Direction}」(只接受 {YarnCountReportRequestDto.DirectionWarp} / " +
                    $"{YarnCountReportRequestDto.DirectionWeft} / {YarnCountReportRequestDto.DirectionKnit})");

            // 方向合法还不够, 还得是**这个布种**有的方向: Woven 传 Knit 会被静默略过汇总、
            // 数据列也写不进去(引擎 ColumnOf 抛), 不如在这里说清楚
            var slot = layout.Directions.FirstOrDefault(d => d.Direction == direction);
            if (slot == null)
                return Result<DocxUrlResponseDto>.Fail(
                    $"{direction} 不属于 {layout.FabricType} 模板(该模板只有 {layout.DirectionNames})");

            int maxIndex = slot.SpecimenCount;
            foreach (var sp in dir!.Specimens ?? new List<YarnCountSpecimenDto>())
            {
                // 越界会写到模板不存在的格(引擎是静默 return), 在这里就挡住
                if (sp.Index < 1 || sp.Index > maxIndex)
                    return Result<DocxUrlResponseDto>.Fail(
                        $"{direction} 的试样号必须在 1~{maxIndex} 之间(模板该方向只有 {maxIndex} 列), 收到 {sp.Index}");
                if (columns.Any(c => c.Direction == direction && c.SpecimenIndex == sp.Index))
                    return Result<DocxUrlResponseDto>.Fail($"{direction} 试样 #{sp.Index} 重复提交");

                var readings = sp.Lengths ?? new List<decimal?>();
                if (readings.Count > YarnCountReportRequestDto.LengthReadingCount)
                    return Result<DocxUrlResponseDto>.Fail(
                        $"{direction} 试样 #{sp.Index} 的长度读数最多 {YarnCountReportRequestDto.LengthReadingCount} 个, 收到 {readings.Count}");

                var rounded = readings.Select(r => YarnCountMath.Round(r, YarnCountReportRequestDto.LengthDecimals)).ToList();
                var average = YarnCountMath.Average(rounded, YarnCountReportRequestDto.AverageDecimals);
                var mass = YarnCountMath.Round(sp.Mass, YarnCountReportRequestDto.MassDecimals);

                columns.Add(new YarnCountColumnModel
                {
                    Direction = direction,
                    SpecimenIndex = sp.Index,
                    Lengths = rounded,
                    Average = average,
                    Mass = mass,
                    Tex = YarnCountMath.ComputeTex(average, mass),
                });
            }
        }

        if (columns.Count == 0)
            return Result<DocxUrlResponseDto>.Fail("纱支试样数据不能为空");

        // 5. 构建填充模型 —— 本布种的每个方向一行: 汇总 Tex(= 该方向各试样 Tex 的算术平均) + 四个换算单位。
        //    没测的方向 MeanTex 返回 null → 那一行留空, 但行本身照发(模板里这行是存在的)。
        var model = new YarnCountReportFillModel
        {
            FabricType = layout.FabricType,
            ReportNumber = dto.ReportNumber.Trim(),
            EnvironmentTemperature = dto.EnvironmentTemperature,
            EnvironmentHumidity = dto.EnvironmentHumidity,
            Summaries = layout.Directions
                .Select(d => BuildSummary(d.Direction, columns, dto.PlyCount))
                .ToList(),
            Columns = columns,
        };

        // 6. 复制模板 + 填充
        // 两件事包在同一个 try 里: 只包 FillReport 的话, CopyTemplate 的 FileNotFoundException
        // 会冒泡成 500, 前端只看到"服务器错误"而拿不到可读原因。
        // 输出文件名不带布种: 归档连续、控制器按名递归找、报告正文自己写着 Warp/Weft 还是 Knit。
        string fileName = $"{model.ReportNumber}_{DateTime.Now:yyMMddHHmmss}_PHY_YarnCount.docx";
        try
        {
            string targetPath = _fileStorage.CopyTemplate(
                Path.Combine("DocxModel", "Common_PHY", layout.TemplateFileName),
                Path.Combine("DocxModel", "SaveDocx", MonthlyFolder()),
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
            downloadUrl = $"/api/YarnCountReport/{fileName}/download"
        });
    }

    /// <summary>
    /// 摘要表一行: 该方向的汇总 Tex + 由它换算出的四个单位值(其中 ’s 是算式文本, 见 UnitsOf)。
    /// 没测的方向 MeanTex 返回 null → 该行五个格全部留空(引擎写空串, 不写 0)。
    /// </summary>
    private static YarnCountSummaryModel BuildSummary(string direction, List<YarnCountColumnModel> columns, int plyCount)
    {
        var tex = YarnCountMath.MeanTex(columns.Where(c => c.Direction == direction).Select(c => c.Tex));
        var (dtex, denier, cc, countSText) = YarnCountMath.UnitsOf(tex, plyCount);

        return new YarnCountSummaryModel
        {
            Direction = direction,
            Tex = tex,
            Dtex = dtex,
            Denier = denier,
            CottonCount = cc,
            CountSText = countSText,
        };
    }

    /// <summary>方向白名单归一化: 只认 Warp / Weft / Knit(大小写不敏感), 其余返回 null 由调用方拒绝</summary>
    private static string? NormalizeDirection(string? direction)
    {
        if (string.IsNullOrWhiteSpace(direction)) return null;

        return direction.Trim().ToUpperInvariant() switch
        {
            "WARP" => YarnCountReportRequestDto.DirectionWarp,
            "WEFT" => YarnCountReportRequestDto.DirectionWeft,
            "KNIT" => YarnCountReportRequestDto.DirectionKnit,
            _ => null
        };
    }

    // 各方向的试样列数不再在这里 switch: 它随布种变(Woven 纬向 5 列 / Knit 5 列),
    // 由 YarnCountFabricTypes 的 Layout 按方向名给出 —— 查不到方向就是拒绝, 同样没有兜底分支。
}
