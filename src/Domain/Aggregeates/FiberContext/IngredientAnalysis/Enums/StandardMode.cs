namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis.Enums
{
    /// <summary>
    /// 报告标准的**取值口径**。
    /// </summary>
    public enum StandardMode
    {
        /// <summary>
        /// 常规 —— 各标准的**当前版本**（`成分标准终.xlsx` 各表的 A 列）。这是默认值。
        /// </summary>
        Current,

        /// <summary>
        /// 2C —— 只印 **CNAS 与 CMA 双资质**范围内的版本。
        ///
        /// 依据同一份 xlsx 的 CNAS / CMA 列：两列打架取旧的，写着 `/` 的分部落空不印。
        /// </summary>
        Dual,
    }
}
