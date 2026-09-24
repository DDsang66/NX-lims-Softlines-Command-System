namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.YarnCountContext;

/// <summary>
/// 纱支报告填充模型 — 报告服务计算后的纯数据载体。
/// 引擎(IYarnCountDocxEngine)只接收此模型, 不依赖 OpenXml 类型。
/// 服务端已经算好并**按模板列序排好**(Columns), 引擎只按坐标落格, 不做任何业务计算。
/// </summary>
public class YarnCountReportFillModel
{
    /// <summary>报告号 → 表0 R0 值格</summary>
    public string ReportNumber { get; set; } = string.Empty;

    /// <summary>环境温度 ℃(写入页脚温度格)</summary>
    public decimal? EnvironmentTemperature { get; set; }

    /// <summary>环境湿度 %RH(写入页脚湿度格)</summary>
    public decimal? EnvironmentHumidity { get; set; }

    /// <summary>表0 Warp (Tex) = 经向各试样 Tex 的算术平均(各试样等权)。为 null 则该格留空</summary>
    public decimal? WarpTex { get; set; }

    /// <summary>表0 Weft (Tex) = 纬向各试样 Tex 的算术平均。为 null 则该格留空</summary>
    public decimal? WeftTex { get; set; }

    /// <summary>
    /// 表0 Knit (Tex) = 针织各试样 Tex 的算术平均(与 Warp/Weft 同口径)。
    /// 2026-09 模板改版前这是页面手工输入的值, 现在由服务端按试样算出; 为 null 则该格留空。
    /// </summary>
    public decimal? KnitTex { get; set; }

    /// <summary>
    /// 表1 数据网格的列 — 每列一个试样。列序由引擎按 Direction+SpecimenIndex 映射到模板列号,
    /// 这里的顺序无所谓; 只提交了的试样才在列表里(没提交的列整列不碰)。
    /// </summary>
    public List<YarnCountColumnModel> Columns { get; set; } = new();
}

/// <summary>
/// 表1 的一个试样列: 长度读数(自上到下填 "1." ~ "10." 行) + Average / Mass / Tex 三行。
/// 任一项为 null 表示该格留空(不写), 不做 0 填充 —— 报告上留空才是"没测", 写 0 会被读成"测出来是 0"。
/// </summary>
public class YarnCountColumnModel
{
    /// <summary>方向: "Warp" | "Weft" | "Knit"(服务端白名单校验过)</summary>
    public string Direction { get; set; } = string.Empty;

    /// <summary>试样号(1-based), 已按方向校验过范围</summary>
    public int SpecimenIndex { get; set; }

    /// <summary>长度读数(cm), 已按 LengthDecimals 舍入。索引 i 对应模板 "1."+i 行</summary>
    public List<decimal?> Lengths { get; set; } = new();

    /// <summary>Average(cm), 已按 AverageDecimals 舍入</summary>
    public decimal? Average { get; set; }

    /// <summary>Mass(g/50), 已按 MassDecimals 舍入</summary>
    public decimal? Mass { get; set; }

    /// <summary>Tex, 已按 TexDecimals 舍入</summary>
    public decimal? Tex { get; set; }
}
