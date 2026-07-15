using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Service.ParamRuleAppService
{
    public class RuleActiveValidateService:IScopedDependency
    {
        private readonly IParamRuleRepository _paramRuleRepository;
        private readonly IParamRuleValidateService  _paramRuleValidateDomainService;

        public RuleActiveValidateService(
            IParamRuleRepository paramRuleRepository,
            IParamRuleValidateService paramRuleValidateDomainService)
        {
            _paramRuleRepository = paramRuleRepository;
            _paramRuleValidateDomainService = paramRuleValidateDomainService;
        }

        /// <summary>
        /// 激活校验用例集合
        /// 编排领域服务用例，调用领域服务进行规则激活校验
        /// </summary>
        /// <param name="ruleId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<Result> ValidateRuleActivationAsync(ParamRuleId ruleId, CancellationToken ct)
        {

            return Result.Ok();
        }
    }
}
