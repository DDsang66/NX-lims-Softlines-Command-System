namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.YarnCountContext;

/// <summary>
/// 纱支报告的**布种口径** —— 一个布种 = 一个模板文件 + 一组按序排列的取样方向。
///
/// 2026-10 模板拆成两份后方向集不再固定: Woven 是 Warp 2 + Weft 5, Knit 只有 Knit 5。
/// 这里放的是**取样口径**(哪个方向、几个试样)与**用哪个模板** —— 服务端要拿它校验试样号范围、
/// 拒绝不属于该布种的方向。**Word 表格行号不在这里**: 那是排版坐标, 留在引擎里
/// (见 YarnCountDocxEngine 的摘要表行号字典, 两张表按方向名 join)。
///
/// 列数与模板数据表 R0 各组跨列标题**按序**一一对应 —— R0 每组的 gridSpan 就是这里的列数,
/// 引擎拿这条关系校验模板(见 YarnCountDocxEngine.ValidateDirectionHeaderRow)。
/// 不按序/数目不符都会在生成前抛异常, 不会静默错列。
/// </summary>
public static class YarnCountFabricTypes
{
    /// <summary>梭织: 经向 + 纬向</summary>
    public const string Woven = "Woven";

    /// <summary>针织: 只有针织方向</summary>
    public const string Knit = "Knit";

    /// <summary>经向试样列数(只有 Woven 有经向)</summary>
    public const int WarpSpecimenCount = 2;

    /// <summary>Woven 的纬向试样列数 —— 与经向刻意不对称, 模板本来就是 2 + 5</summary>
    public const int WovenWeftSpecimenCount = 5;

    /// <summary>针织试样列数</summary>
    public const int KnitSpecimenCount = 5;

    /// <summary>一个方向槽: 方向名 + 试样列数</summary>
    public sealed record DirectionSlot(string Direction, int SpecimenCount);

    /// <summary>一个布种: 规范名 + 模板文件名 + 有序方向槽(与模板数据表 R0 的跨列标题按序对应)</summary>
    public sealed record Layout(string FabricType, string TemplateFileName, DirectionSlot[] Directions)
    {
        /// <summary>
        /// 数据列的最大列号(数据列从格1 起, 格0 恒为行标签) —— 各方向列数之和。
        /// 推导而不是写死 7/5: 改列数时这里跟着走, 不留第二处手写坐标。
        /// </summary>
        public int MaxDataColumn => Directions.Sum(d => d.SpecimenCount);

        /// <summary>方向名列表 —— 报错文案要能让操作员看出「这个模板有哪些方向」</summary>
        public string DirectionNames => string.Join(" / ", Directions.Select(d => d.Direction));
    }

    public static readonly Layout WovenLayout = new(Woven, "PHY_YarnCount_woven.docx", new[]
    {
        new DirectionSlot(YarnCountReportRequestDto.DirectionWarp, WarpSpecimenCount),
        new DirectionSlot(YarnCountReportRequestDto.DirectionWeft, WovenWeftSpecimenCount),
    });

    public static readonly Layout KnitLayout = new(Knit, "PHY_YarnCount_knit.docx", new[]
    {
        new DirectionSlot(YarnCountReportRequestDto.DirectionKnit, KnitSpecimenCount),
    });

    /// <summary>
    /// 布种白名单(去空格 + 大小写不敏感)。查不到返回 null, 由调用方决定怎么报错 ——
    /// 服务端走 Result.Fail(面向操作员), 引擎走抛异常(说明服务端漏了校验)。
    /// 模板文件名与试样列数只能从这里的 Layout 里取, 请求里那个字符串永不进路径(见 YarnCountReportService)。
    /// </summary>
    public static Layout? TryGet(string? fabricType)
    {
        if (string.IsNullOrWhiteSpace(fabricType)) return null;

        return fabricType.Trim().ToUpperInvariant() switch
        {
            "WOVEN" => WovenLayout,
            "KNIT" => KnitLayout,
            _ => null
        };
    }

    /// <summary>查不到即抛(与引擎 ColumnOf / SummaryRowOf 同一原则: 绝不兜底到某个布种)</summary>
    public static Layout Of(string? fabricType)
        => TryGet(fabricType) ?? throw new InvalidOperationException(
            $"未知布种「{fabricType}」—— 只接受 {Woven} / {Knit}");
}
