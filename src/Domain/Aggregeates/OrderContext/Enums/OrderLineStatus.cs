namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.Enums
{
    /// <summary>
    /// 订单行状态（对应 LabTestInfos 表的 Status 字段）
    /// 1=进单完成, 2=审单完成, 3=在实验室中, 4=测试完成, 5=报告已出
    /// </summary>
    public enum OrderLineStatus
    {
        /// <summary>进单完成</summary>
        EntryComplete = 1,

        /// <summary>审单完成</summary>
        ReviewComplete = 2,

        /// <summary>在实验室中</summary>
        InLab = 3,

        /// <summary>测试完成</summary>
        TestDone = 4,

        /// <summary>报告已出</summary>
        ReportOut = 5
    }
}
