namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.Enums;

/// <summary>
/// OrderExpress 枚举的扩展方法
/// </summary>
public static class OrderExpressExtensions
{
    public static string ToDisplayString(this OrderExpress express) => express switch
    {
        OrderExpress.SameDay => "Same Day",
        OrderExpress.Shuttle => "Shuttle",
        OrderExpress.Express => "Express",
        _ => "Regular"
    };

    public static OrderExpress ToOrderExpress(this string? s) => s switch
    {
        "Same Day" => OrderExpress.SameDay,
        "Shuttle" => OrderExpress.Shuttle,
        "Express" => OrderExpress.Express,
        _ => OrderExpress.Regular
    };
}
