namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.YarnCountContext;

/// <summary>
/// 纱支(Yarn Count)报告生成请求 — 前端提交。
/// 模板按布种分两份(PHY_YarnCount_woven.docx / PHY_YarnCount_knit.docx), 方向与列数见
/// <see cref="YarnCountFabricTypes"/>; 两张表的 Word 坐标见 YarnCountDocxEngine 的坐标常量。
/// </summary>
public class YarnCountReportRequestDto
{
    /// <summary>经向</summary>
    public const string DirectionWarp = "Warp";

    /// <summary>纬向</summary>
    public const string DirectionWeft = "Weft";

    /// <summary>针织</summary>
    public const string DirectionKnit = "Knit";

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
    //   ’s     = 与 cc 同值, 但**印成分数**: (cc×股数算出来的数)/股数 —— 见下面 PlyCount
    // 只在 tex > 0 时换算; tex 缺失或为 0 → 四列全部留空(不写 0)。见 YarnCountMath.UnitsOf。

    /// <summary>换算列(dtex/denier/cc/’s)显示小数位 —— 与 Tex 同取 2 位</summary>
    public const int UnitDecimals = 2;

    /// <summary>dtex = tex × 10(定义式: 10000 m 的克数)</summary>
    public const decimal DtexFactor = 10m;

    /// <summary>denier = tex × 9(定义式: 9000 m 的克数)</summary>
    public const decimal DenierFactor = 9m;

    /// <summary>英制棉支数 cc = 590.5 ÷ tex(ASTM D2260 换算表口径, 840 yd/lb)</summary>
    public const decimal CottonCountConstant = 590.5m;

    /// <summary>股数缺省值 —— 不填就是 1 股(前端输入框的默认值也是它)</summary>
    public const int DefaultPlyCount = 1;

    /// <summary>
    /// 布种: "Woven"(梭织, 经+纬) | "Knit"(针织) —— 服务端白名单·大小写不敏感。
    /// 决定用哪个模板、有哪些方向、每个方向几个试样(见 <see cref="YarnCountFabricTypes"/>)。
    /// 缺省 Woven: 最早的模板就是 Warp 2 + Weft 5, 老客户端不传这个字段时按梭织走。
    /// </summary>
    public string FabricType { get; set; } = YarnCountFabricTypes.Woven;

    /// <summary>
    /// 股数(操作员填, 正整数) —— **只改 ’s 那一格印出来的写法**, 不参与任何换算:
    /// 分子印 cc×股数 算出来的那个数, 分母印股数, 让审核看得见这个股线按几股折的。
    /// </summary>
    public int PlyCount { get; set; } = DefaultPlyCount;

    /// <summary>报告号(前端用"试样编号"sid)</summary>
    public string ReportNumber { get; set; } = string.Empty;

    /// <summary>环境温度 ℃(写入页脚温度格)</summary>
    public decimal? EnvironmentTemperature { get; set; }

    /// <summary>环境湿度 %RH(写入页脚湿度格)</summary>
    public decimal? EnvironmentHumidity { get; set; }

    /// <summary>各方向的试样数据(每个方向一条, 没测的方向不传)。方向必须属于上面 FabricType 那个布种</summary>
    public List<YarnCountDirectionDto> Directions { get; set; } = new();
}

/// <summary>一个方向(Warp / Weft / Knit)下的全部试样</summary>
public class YarnCountDirectionDto
{
    /// <summary>
    /// 方向: "Warp" | "Weft" | "Knit"(服务端白名单校验, 大小写不敏感)。
    /// 还必须属于本次请求的布种 —— Woven 只有 Warp/Weft, Knit 只有 Knit。
    /// </summary>
    public string Direction { get; set; } = YarnCountReportRequestDto.DirectionWarp;

    /// <summary>该方向的试样列表</summary>
    public List<YarnCountSpecimenDto> Specimens { get; set; } = new();
}

/// <summary>单个试样: 若干长度读数 + 一个质量</summary>
public class YarnCountSpecimenDto
{
    /// <summary>
    /// 试样号(1-based) → 模板该方向第几列。
    /// 上界是该方向在本布种里的列数(Woven: 经 2 / 纬 5; Knit: 5, 见 YarnCountFabricTypes)——
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
