namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.YarnCountContext;

/// <summary>
/// 纱支(Yarn Count)报告生成请求 — 前端提交。
/// 模板 PHY_YarnCount.docx 的两张表见 YarnCountDocxEngine 的坐标常量。
/// </summary>
public class YarnCountReportRequestDto
{
    /// <summary>经向</summary>
    public const string DirectionWarp = "Warp";

    /// <summary>纬向</summary>
    public const string DirectionWeft = "Weft";

    /// <summary>针织</summary>
    public const string DirectionKnit = "Knit";

    /// <summary>
    /// 经向试样列数。
    ///
    /// 三个方向的列数必须与模板数据表 R0 的三组标题(Warp | Weft | Knit)**按序**一一对应 ——
    /// R0 每组的 gridSpan 就是这里的列数, 引擎拿这条关系校验模板(见 YarnCountDocxEngine 的 R0 校验)。
    /// 不按序/数目不符都会在生成前抛异常, 不会静默错列。
    /// 服务端拿它校验试样号范围, 引擎拿它算列偏移。
    ///
    /// 2026-09 模板改版: 原为 Warp 2 + Weft 5, 现为 Warp 2 + Weft 2 + Knit 2;
    /// Knit 也从"页面手工输入的单个汇总值"改为**与经纬向同口径按试样测**。
    /// 数据列号(0-based): Warp 1~2 / Weft 3~4 / Knit 5~6。
    /// </summary>
    public const int WarpSpecimenCount = 2;

    /// <summary>纬向试样列数(见 WarpSpecimenCount 注释; 2026-09 模板由 5 列改为 2 列)</summary>
    public const int WeftSpecimenCount = 2;

    /// <summary>针织试样列数(见 WarpSpecimenCount 注释)</summary>
    public const int KnitSpecimenCount = 2;

    /// <summary>模板 Length 行的读数个数(数据表 R2..R11 的 "1." ~ "10.")</summary>
    public const int LengthReadingCount = 10;

    // ==================== 报告显示取位 ====================
    // 页面(前端)与服务端共用这一组常量, 保证"页面即时显示的 = 报告里落格的"。
    // Tex 用**已舍入的** Average 与 Mass 参与计算, 审核拿报告上印的数手算能复现同一个 Tex。

    /// <summary>长度读数(cm)显示小数位</summary>
    public const int LengthDecimals = 2;

    /// <summary>Average(cm) 显示小数位</summary>
    public const int AverageDecimals = 2;

    /// <summary>Mass(g/50) 显示小数位 —— 天平原生就是 3 位</summary>
    public const int MassDecimals = 3;

    /// <summary>Tex 显示小数位</summary>
    public const int TexDecimals = 2;

    // ==================== 摘要表的换算单位 ====================
    // 摘要表的单位表头行(R5)是: '' | tex | dtex | denier | cc | ’s,
    // 每个方向一行, 四个换算值都由**该方向已舍入的汇总 Tex**推出 ——
    // 和 Tex 一样自成闭环: 审核拿报告上印的 tex 手算, 能复现印出来的 dtex/denier/cc。
    // 口径(ASTM D2260-03(2013) / ISO 2947; dtex 与 denier 是定义式):
    //   dtex   = tex × 10
    //   denier = tex × 9
    //   cc     = 590.5 ÷ tex   (英制棉支数 Ne, 840 yd/lb)
    //   ’s     = 与 cc 同值    (表头 ’s 只是把同一个支数写成 "xx’s" 的记法, 不是另一个计数制)
    // 只在 tex > 0 时换算; tex 缺失或为 0 → 四列全部留空(不写 0)。见 YarnCountMath.UnitsOf。

    /// <summary>换算列(dtex/denier/cc/’s)显示小数位 —— 与 Tex 同取 2 位</summary>
    public const int UnitDecimals = 2;

    /// <summary>dtex = tex × 10(定义式: 10000 m 的克数)</summary>
    public const decimal DtexFactor = 10m;

    /// <summary>denier = tex × 9(定义式: 9000 m 的克数)</summary>
    public const decimal DenierFactor = 9m;

    /// <summary>英制棉支数 cc = 590.5 ÷ tex(ASTM D2260 换算表口径, 840 yd/lb)</summary>
    public const decimal CottonCountConstant = 590.5m;

    /// <summary>报告号(前端用"试样编号"sid)</summary>
    public string ReportNumber { get; set; } = string.Empty;

    /// <summary>环境温度 ℃(写入页脚温度格)</summary>
    public decimal? EnvironmentTemperature { get; set; }

    /// <summary>环境湿度 %RH(写入页脚湿度格)</summary>
    public decimal? EnvironmentHumidity { get; set; }

    /// <summary>经向/纬向/针织的试样数据(每个方向一条, 没测的方向不传)</summary>
    public List<YarnCountDirectionDto> Directions { get; set; } = new();
}

/// <summary>一个方向(Warp / Weft / Knit)下的全部试样</summary>
public class YarnCountDirectionDto
{
    /// <summary>方向: "Warp" | "Weft" | "Knit"(只接受这三个, 服务端白名单校验)</summary>
    public string Direction { get; set; } = YarnCountReportRequestDto.DirectionWarp;

    /// <summary>该方向的试样列表</summary>
    public List<YarnCountSpecimenDto> Specimens { get; set; } = new();
}

/// <summary>单个试样: 若干长度读数 + 一个质量</summary>
public class YarnCountSpecimenDto
{
    /// <summary>
    /// 试样号(1-based) → 模板该方向第几列。
    /// 三个方向都是 1~2(见 YarnCountReportRequestDto 的三个 SpecimenCount 常量)——
    /// 越界会写到不存在的格, 服务端直接拒绝。
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// 长度读数(cm), 按录入顺序对应模板的 "1." ~ "10." 行。
    /// 少于 10 个时后面几行留空(操作员只读了 8 个就是 8 个, 不补齐造数);
    /// 中间的 null 表示该槽位没读数, 同样留空, 且不参与 Average。
    /// </summary>
    public List<decimal?> Lengths { get; set; } = new();

    /// <summary>质量 g/50(天平读数或手输)。为空则 Average/Mass/Tex 三行里只有 Average 有值</summary>
    public decimal? Mass { get; set; }
}
