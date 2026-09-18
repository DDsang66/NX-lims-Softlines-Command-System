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

    /// <summary>
    /// 买家: 决定引擎按哪份模板的布局填("Normal" | "Adidas" | "FOCUS" | "NEXT")。
    /// 取值与 PhysicalWeightReportRequestDto.Buyer* 常量一致 —— 由服务端白名单校验过才到得了这里。
    /// </summary>
    public string Buyer { get; set; } = PhysicalWeightReportRequestDto.BuyerNormal;

    /// <summary>
    /// NEXT 每测点汇总表(Specimen | Number of Sample | Total(g) | Ave(g/m²))的行。
    /// 与 Rows / TrailerRows 是**按录入方式分流**的: 长×宽录的行有逐条的尺寸, 走数据表(#1~#5)与
    /// 3 格登记表; 直接填面积录的行一条只有一个面积, 摊不进 #1~#5, 汇总到这里每测点一行。
    /// 一份报告里两种录入都有时, 两张表各自都有数; 某一种没有时对应那张表整张不动。
    /// 其余买家没有这张表, 引擎按买家跳过(本列表算了也不读)。
    /// </summary>
    public List<PhysicalWeightPerPointRowModel> PerPointRows { get; set; } = new();

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

    /// <summary>表2 文档末登记行(每行=一次测量: 测点 | 重量g | 尺寸)</summary>
    public List<PhysicalWeightTrailerRowModel> TrailerRows { get; set; } = new();
}

/// <summary>
/// 表2 文档末登记行(每行=一次测量): [Sample(测点), 重量(g), 尺寸]。
/// 对应导出的原始数据表列: 试样测点 | 重量(g) | 尺寸; 尺寸按类型: area→面积(cm²), length→长度(cm), piece→称重条数。
/// area 长×宽录入时用 MeasureText 直写尺寸文本("5×5"), 不再显示换算后的 cm²。
/// </summary>
public class PhysicalWeightTrailerRowModel
{
    /// <summary>Sample: 试样测点</summary>
    public string Sample { get; set; } = string.Empty;

    /// <summary>重量(g)</summary>
    public decimal? Weight { get; set; }

    /// <summary>第3列尺寸值: area→面积cm², length→长度cm, piece→条数(引擎按测试类型定格式)</summary>
    public decimal? Measure { get; set; }

    /// <summary>第3列文本覆盖(仅 area 长×宽模式, 如 "5×5"): 非空时引擎直写此文本, 忽略 Measure 数值</summary>
    public string? MeasureText { get; set; }
}

/// <summary>表0 汇总网格行: [Sample, 各单位值]</summary>
public class PhysicalWeightSummaryRowModel
{
    public string Point { get; set; } = string.Empty;
    public decimal Value1 { get; set; }   // 第一种单位(如 g/m² / g/piece)
    public decimal Value2 { get; set; }   // 第二种单位(如 oz/yd² / lb/dozen)
    public decimal Value3 { get; set; }   // 第三种单位(条重 oz/dozen; FOCUS 面积 g/m; 其余不用)

    /// <summary>该测点用的布边长度 cm(仅 FOCUS 的 Sample | Selvage Length 表读它) —— 面积模式下长度是
    /// 单独录的, 同测点各条长度应相同, 取第一条有值的; 全没录则为 null(该格留空)。</summary>
    public decimal? SelvageLength { get; set; }
}

/// <summary>
/// NEXT 每测点汇总行: [Specimen, Number of Sample, Total(g), Ave(g/m²)] —— 与表0 汇总行不是一回事:
/// 表0 是全部记录的均值网格, 这张表只统计"直接填面积"录的那些行(见 PhysicalWeightReportFillModel.PerPointRows)。
/// </summary>
public class PhysicalWeightPerPointRowModel
{
    /// <summary>Specimen: 试样测点</summary>
    public string Point { get; set; } = string.Empty;

    /// <summary>Number of Sample: 该测点样品数 = 各条记录 SampleCount 之和(不填按 1 算)</summary>
    public int SampleCount { get; set; }

    /// <summary>Total(g): 该测点重量合计</summary>
    public decimal? TotalWeight { get; set; }

    /// <summary>
    /// Ave(g/m²): 该测点 g/m²(报告里保留一位小数, 与表0、数据表同精度)。
    /// **不是各条 g/m² 的等权平均**: 单块试样固定 100 cm², 该次称重的重量是 N 块的总重,
    /// 所以 = 重量合计 ÷ (样品数合计 × 100 cm²) × 10000 —— 池化口径, 算法在
    /// PhysicalWeightReportService 的 perPointRows。
    /// </summary>
    public decimal AverageGsm { get; set; }
}

/// <summary>模板数据行: [Sample, #1~#5, Average]</summary>
public class PhysicalWeightReportRowModel
{
    public string Point { get; set; } = string.Empty;

    public List<decimal> Values { get; set; } = new();
    public decimal? Average { get; set; }
}
