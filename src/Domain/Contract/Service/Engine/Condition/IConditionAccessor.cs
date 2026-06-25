using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine.Condition
{
    public interface IConditionAccessor
    {
        /// <summary>
        /// 尝试从 ConditionPool 中按路径取值（支持嵌套路径 "A.B.C"）
        /// 返回 false 表示不存在或无法访问。
        /// </summary>
        bool TryGet(ConditionPool pool, string path, out object? value);
    }
}
