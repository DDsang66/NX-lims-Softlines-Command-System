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
}

/// <summary>表0 汇总网格行: [Sample, 双单位值]</summary>
public class PhysicalWeightSummaryRowModel
{
    public string Point { get; set; } = string.Empty;
    public decimal Value1 { get; set; }   // 第一种单位(如 g/m²)
    public decimal Value2 { get; set; }   // 第二种单位(如 oz/yd²)
}

/// <summary>模板数据行: [Sample, #1~#5, Average]</summary>
public class PhysicalWeightReportRowModel
{
    public string Point { get; set; } = string.Empty;
    public List<decimal> Values { get; set; } = new();
    public decimal? Average { get; set; }
}
