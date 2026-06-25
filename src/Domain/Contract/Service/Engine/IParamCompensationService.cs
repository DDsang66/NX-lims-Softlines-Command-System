using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine
{
    /// <summary>
    /// 补偿服务：对引擎生成的 ParamSet 执行补偿与越界校验，返回最终 ParamSet 或抛出领域异常
    /// </summary>
    public interface IParamCompensationService
    {
        ParamSet ApplyCompensation(ParamSet generated, ParamSchema schema);
    }
}
