namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.PhysicalWeightContext;

/// <summary>物理克重报告生成请求 — 前端提交</summary>
public class PhysicalWeightReportRequestDto
{
    /// <summary>买家 Normal(默认, 现有 PHY_Weight.docx)</summary>
    public const string BuyerNormal = "Normal";

    /// <summary>买家 Adidas(仅面积克重, 汇总表只有 g/m² 一列)</summary>
    public const string BuyerAdidas = "Adidas";

    /// <summary>买家 FOCUS(仅面积克重, 汇总表 g/m²|oz/yd²|g/m 三列)</summary>
    public const string BuyerFocus = "FOCUS";

    /// <summary>买家 NEXT(仅面积克重, 无数据表, 文末登记表按测点汇总)</summary>
    public const string BuyerNext = "NEXT";

    /// <summary>报告号(前端用"试样编号"sid)</summary>
    public string ReportNumber { get; set; } = string.Empty;

    /// <summary>
    /// 买家: 决定用哪份模板。默认 Normal(现有 PHY_Weight.docx)。
    /// 取值只在服务端白名单里映射成模板文件名, 前端传的值永不直接进路径。
    /// </summary>
    public string Buyer { get; set; } = BuyerNormal;

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

    /// <summary>面积 cm²(可选, area 类型的录入尺寸, 溯源保留)</summary>
    public decimal? Area { get; set; }

    /// <summary>长×宽模式的前端尺寸文本, 如 "5×5"(可选, area 类型录入尺寸, 溯源保留)</summary>
    public string? Dimension { get; set; }

    /// <summary>试样长度 cm(可选, length 类型的录入尺寸, 溯源保留)。
    /// FOCUS 的面积报告也用它: 面积卡片里那个长度框既算 g/m, 又进模板的 Sample | Selvage Length 表。</summary>
    public decimal? LengthCm { get; set; }

    /// <summary>长度克重 g/m(前端已算; FOCUS 面积报告也用它 —— 汇总表 g/m 那一格)</summary>
    public decimal GPerM { get; set; }

    /// <summary>长度克重 oz/yd(前端已算)</summary>
    public decimal OzPerYd { get; set; }

    /// <summary>条重 g/piece(前端已算)</summary>
    public decimal GPerPiece { get; set; }

    /// <summary>条重 lb/dozen(前端已算)</summary>
    public decimal LbPerDozen { get; set; }

    /// <summary>条重 oz/dozen(前端已算, 表0 汇总第三格用)</summary>
    public decimal OzPerDozen { get; set; }

    /// <summary>称重条数(条重, 前端默认 12=1打; lb/dozen·oz/dozen 换算的 dozen 基准, 溯源保留)</summary>
    public decimal? PieceCount { get; set; }

    /// <summary>
    /// 样品数(仅 NEXT 用: 该次称重覆盖了几块试样 → 登记表 Number of sample); 不填按 1 算。
    /// 与 PieceCount 分开: 那是条重的"称重条数", 语义不同, 混用会让表2 尺寸列的取值逻辑变脆。
    /// </summary>
    public int? SampleCount { get; set; }
}
