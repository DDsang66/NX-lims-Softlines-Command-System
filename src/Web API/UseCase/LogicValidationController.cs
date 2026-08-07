using DocumentFormat.OpenXml.Bibliography;
using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.CheckListContext;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.UseCase;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Application.Service.CheckListContext;
using NX_lims_Softlines_Command_System.src.Application.UseCase;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Web_API
{
    [ApiController]
    [Route("api/[controller]")] 
    public class LogicValidationController : ControllerBase
    {
        private readonly LogicTestUseCaseService _logicTestUseCaseService;

        // 1. 通过构造函数依赖注入 AppService
        public LogicValidationController(LogicTestUseCaseService logicTestUseCaseService)
        {
            _logicTestUseCaseService = logicTestUseCaseService;
        }

        /// <summary>
        /// 添加测试列表
        /// </summary>
        /// <returns></returns>
        [HttpPut("test")]
        public async Task<Result> TestLogic(TestLogicSubmitDto dto,CancellationToken ct)
        {
            var result = await _logicTestUseCaseService.TestLogicAsync(dto, ct);

            return Result.Ok();
        }

        /// <summary>
        /// 更新条件列表
        /// </summary>
        /// <returns></returns>
        [HttpPost("condition-update")]
        public async Task<Result<ConditionPoolResponseDto>> UpdateTestCondition(LogicTestConditionUpdateDto dto,CancellationToken ct)
        {
            var result = await _logicTestUseCaseService.UpdateConditionPoolAsync(dto, ct);

            return result;
        }


    }
}
