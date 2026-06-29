namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.Enums
{
    /// <summary>
    /// 急单类型，根据 DueDate 与 LabIn 的天数差计算
    /// </summary>
    public enum OrderExpress
    {
        /// <summary>&gt;4 天</summary>
        Regular = 0,

        /// <summary>3-4 天</summary>
        Express = 1,

        /// <summary>2-3 天</summary>
        Shuttle = 2,

        /// <summary>&lt;=1 天</summary>
        SameDay = 3
    }
}
