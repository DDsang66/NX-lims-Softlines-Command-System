namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.Enums
{
    /// <summary>
    /// 订单行状态（对应 LabTestInfos 表的 Status 字段）
    /// 1=In Lab, 2=Review Finished, 3=Test Done
    /// </summary>
    public enum OrderLineStatus
    {
        /// <summary>在实验室中</summary>
        InLab = 1,

        /// <summary>审核完成</summary>
        ReviewFinished = 2,

        /// <summary>测试完成</summary>
        TestDone = 3
    }
}
