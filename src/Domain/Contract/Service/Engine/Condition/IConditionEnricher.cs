using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine.Condition
{
    public interface IConditionEnricher
    {
        /// <summary>
        /// 将原始（一级）数据丰富为二级原子条件，并返回新的 ConditionPool
        /// </summary>
        ConditionPool Enrich(IDictionary<string, object?> rawData);
    }
}
