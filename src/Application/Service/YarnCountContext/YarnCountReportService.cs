using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.YarnCountContext;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Application.Interface.YarnCountContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Interface;

namespace NX_lims_Softlines_Command_System.src.Application.Service.YarnCountContext;

/// <summary>
/// 纱支报告生成 — 用 PHY_YarnCount.docx 模板填充。
/// 业务计算(平均长度/取位/Tex/方向汇总)在本层, OpenXml 填充委托给 IYarnCountDocxEngine。
/// 不落库: 试样数据随请求体进来, 报告生成完就结束(与耐磨模块一致)。
/// </summary>
public class YarnCountReportService : IYarnCountReportService, IScopedDependency
{
    /// <summary>
    /// 模板文件名 —— 服务端常量。IFileStorageService.CopyTemplate 对路径零校验(直接 File.Copy),
    /// 所以模板名只能从这里来, 前端传的值永不进路径。
    /// </summary>
    private const string TemplateFileName = "PHY_YarnCount.docx";

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

        // 2. 逐方向校验, 同时摊成模板列(读数先舍入再取平均 —— 报告上印的就是舍入后的数)
        var columns = new List<YarnCountColumnModel>();
        foreach (var dir in dto.Directions)
        {
            string? direction = NormalizeDirection(dir?.Direction);
            if (direction == null)
                return Result<DocxUrlResponseDto>.Fail(
                    $"方向取值非法: 「{dir?.Direction}」(只接受 {YarnCountReportRequestDto.DirectionWarp} / {YarnCountReportRequestDto.DirectionWeft})");

            int maxIndex = SpecimenCountOf(direction);
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

        // 3. 构建填充模型 —— 方向汇总 = 该方向各试样 Tex 的算术平均
        var model = new YarnCountReportFillModel
        {
            ReportNumber = dto.ReportNumber.Trim(),
            EnvironmentTemperature = dto.EnvironmentTemperature,
            EnvironmentHumidity = dto.EnvironmentHumidity,
            KnitTex = YarnCountMath.Round(dto.KnitTex, YarnCountReportRequestDto.TexDecimals),
            WarpTex = YarnCountMath.MeanTex(columns.Where(c => c.Direction == YarnCountReportRequestDto.DirectionWarp).Select(c => c.Tex)),
            WeftTex = YarnCountMath.MeanTex(columns.Where(c => c.Direction == YarnCountReportRequestDto.DirectionWeft).Select(c => c.Tex)),
            Columns = columns,
        };

        // 4. 复制模板 + 填充
        // 两件事包在同一个 try 里: 只包 FillReport 的话, CopyTemplate 的 FileNotFoundException
        // 会冒泡成 500, 前端只看到"服务器错误"而拿不到可读原因。
        string fileName = $"{model.ReportNumber}_{DateTime.Now:yyMMddHHmmss}_PHY_YarnCount.docx";
        try
        {
            string targetPath = _fileStorage.CopyTemplate(
                Path.Combine("DocxModel", "Common_PHY", TemplateFileName),
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

    /// <summary>方向白名单归一化: 只认 Warp / Weft(大小写不敏感), 其余返回 null 由调用方拒绝</summary>
    private static string? NormalizeDirection(string? direction)
    {
        if (string.IsNullOrWhiteSpace(direction)) return null;

        return direction.Trim().ToUpperInvariant() switch
        {
            "WARP" => YarnCountReportRequestDto.DirectionWarp,
            "WEFT" => YarnCountReportRequestDto.DirectionWeft,
            _ => null
        };
    }

    /// <summary>该方向模板里有几列试样 —— 经 2、纬 5, 见 YarnCountReportRequestDto 的常量注释</summary>
    private static int SpecimenCountOf(string direction)
        => direction == YarnCountReportRequestDto.DirectionWarp
            ? YarnCountReportRequestDto.WarpSpecimenCount
            : YarnCountReportRequestDto.WeftSpecimenCount;
}
