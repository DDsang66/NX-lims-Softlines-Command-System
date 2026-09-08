namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.PhysicalWeightContext;

/// <summary>
/// 物理克重报告填充模型 — 报告服务计算后的纯数据载体。
/// 引擎(IPhysicalWeightDocxEngine)只接收此模型, 不依赖 OpenXml 类型。
/// </summary>
public class PhysicalWeightReportFillModel
{
    /// <summary>报告号</summary>
    public string ReportNumber { get; set; } = string.Empty;

    /// <summary>测试方法(可选)</summary>
    public string? TestMethod { get; set; }

    /// <summary>测试类型: "area"(面积克重) | "length"(长度克重) | "piece"(条重)</summary>
    public string TestType { get; set; } = "area";

    /// <summary>表1 表头单位文字: "g/m²" | "g/m" | "g/piece"(写入 Specimen/Average 表头)</summary>
    public string DataUnit { get; set; } = "g/m²";

    /// <summary>环境温度 ℃(写入页脚温度格)</summary>
    public decimal? EnvironmentTemperature { get; set; }

    /// <summary>环境湿度 %RH(写入页脚湿度格)</summary>
    public decimal? EnvironmentHumidity { get; set; }

    /// <summary>表0 汇总网格行(每测点一行 + 末尾"平均"行, Sample=测点, Value1/Value2=两种单位的均值)</summary>
    public List<PhysicalWeightSummaryRowModel> SummaryRows { get; set; } = new();

    /// <summary>表1 数据行(每行=模板一行, Sample=测点, 最多5个值, Average=平均, 值为主单位)</summary>
    public List<PhysicalWeightReportRowModel> Rows { get; set; } = new();

    /// <summary>表2 文档末登记行(每行=一次测量, 与导出原始数据表前 5 列一致)</summary>
    public List<PhysicalWeightTrailerRowModel> TrailerRows { get; set; } = new();
}

/// <summary>
/// 表2 文档末登记行(每行=一次测量): [No, Report Number, Sample(测点), 重量(g), 尺寸]。
/// 语义 = 前端导出的原始数据表前 5 列: 次数 | 试样编号 | 试样测点 | 重量(g) | 第5列尺寸;
/// 第5列按类型: area→面积(cm²), length→长度(cm), piece→称重条数。
/// area 长×宽录入时用 MeasureText 直写尺寸文本("5×5"), 不再显示换算后的 cm²。
/// </summary>
public class PhysicalWeightTrailerRowModel
{
    /// <summary>No: 本次测量的序号(1 起, 递增)</summary>
    public int No { get; set; }

    /// <summary>Report Number: 试样编号(报告号); 该记录无试样编号时回退整份报告的报告号</summary>
    public string ReportNumber { get; set; } = string.Empty;

    /// <summary>Sample: 试样测点</summary>
    public string Sample { get; set; } = string.Empty;

    /// <summary>重量(g)</summary>
    public decimal? Weight { get; set; }

    /// <summary>第5列尺寸值: area→面积cm², length→长度cm, piece→条数(引擎按测试类型定格式)</summary>
    public decimal? Measure { get; set; }

    /// <summary>第5列文本覆盖(仅 area 长×宽模式, 如 "5×5"): 非空时引擎直写此文本, 忽略 Measure 数值</summary>
    public string? MeasureText { get; set; }
}

/// <summary>表0 汇总网格行: [Sample, 各单位值]</summary>
public class PhysicalWeightSummaryRowModel
{
    public string Point { get; set; } = string.Empty;
    public decimal Value1 { get; set; }   // 第一种单位(如 g/m² / g/piece)
    public decimal Value2 { get; set; }   // 第二种单位(如 oz/yd² / lb/dozen)
    public decimal Value3 { get; set; }   // 第三种单位(仅条重 oz/dozen; 面积/长度不用)
}

/// <summary>模板数据行: [Sample, #1~#5, Average]</summary>
public class PhysicalWeightReportRowModel
{
    public string Point { get; set; } = string.Empty;

    public List<decimal> Values { get; set; } = new();
    public decimal? Average { get; set; }
}
