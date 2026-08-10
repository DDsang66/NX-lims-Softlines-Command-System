namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.PhysicalWeightContext;

/// <summary>物理克重报告生成请求 — 前端提交</summary>
public class PhysicalWeightReportRequestDto
{
    /// <summary>报告号(前端用"试样编号"sid)</summary>
    public string ReportNumber { get; set; } = string.Empty;

    /// <summary>测试类型: "area"(面积克重) | "length"(长度克重) | "piece"(条重)</summary>
    public string TestType { get; set; } = "area";

    /// <summary>测试方法(可选, 填模板 Test Method)</summary>
    public string? TestMethod { get; set; }

    /// <summary>环境温度 ℃(写入页脚温度格)</summary>
    public decimal? EnvironmentTemperature { get; set; }

    /// <summary>环境湿度 %RH(写入页脚湿度格)</summary>
    public decimal? EnvironmentHumidity { get; set; }

    /// <summary>测量记录列表(每次测量一条)</summary>
    public List<PhysicalWeightReportRecordDto> Records { get; set; } = new();
}

/// <summary>单次测量记录</summary>
public class PhysicalWeightReportRecordDto
{
    /// <summary>试样测点 → 模板 Sample 列</summary>
    public string? Point { get; set; }

    /// <summary>试样编号(参考/溯源)</summary>
    public string? SampleId { get; set; }

    /// <summary>克重 g/m²(前端已算)</summary>
    public decimal Gsm { get; set; }

    /// <summary>克重 oz/yd²(前端已算)</summary>
    public decimal Oz { get; set; }

    /// <summary>重量 g(可选)</summary>
    public decimal? Weight { get; set; }

    /// <summary>面积 cm²(可选)</summary>
    public decimal? Area { get; set; }

    /// <summary>长度克重 g/m(前端已算)</summary>
    public decimal GPerM { get; set; }

    /// <summary>长度克重 oz/yd(前端已算)</summary>
    public decimal OzPerYd { get; set; }

    /// <summary>条重 g/piece(前端已算)</summary>
    public decimal GPerPiece { get; set; }

    /// <summary>条重 lb/dozen(前端已算)</summary>
    public decimal LbPerDozen { get; set; }
}
