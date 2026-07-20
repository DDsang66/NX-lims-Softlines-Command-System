using DocumentFormat.OpenXml.Bibliography;
using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.CheckListContext;
using NX_lims_Softlines_Command_System.src.Application.Service.CheckListContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Web_API
{
    [ApiController]
    [Route("api/[controller]")] 
    public class CheckListController : ControllerBase
    {
        private readonly CheckListAppService _checkListAppService;

        // 1. 通过构造函数依赖注入 AppService
        public CheckListController(CheckListAppService checkListAppService)
        {
            _checkListAppService = checkListAppService;
        }

        /// <summary>
        /// 添加测试列表
        /// </summary>
        /// <returns></returns>
        [HttpPost("add")]
        public async Task<Result> AddCheckLIst(AddCheckListDto dto,CancellationToken ct)
        {
            var result = await _checkListAppService.AddCheckList(dto,ct);

            return Result.Ok();
        }

        /// <summary>
        /// 修改测试列表
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpPut("update")]
        public async Task<Result> UpdateCheckList(UpdateCheckListDto dto, CancellationToken ct) 
        {
            var result = await _checkListAppService.UpdateCheckList(dto,ct);

            return Result.Ok();
        }

        /// <summary>
        /// 根据ID查询测试列表
        /// </summary>
        /// <param name="checkListId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("{checkListId}")]
        public async Task<Result<CheckListResponseDto>> GetCheckListById(Guid checkListId, CancellationToken ct) 
        {
            var result = await _checkListAppService.GetCheckListAsync(checkListId,ct);

            return result;
        }

        /// <summary>
        /// 计算参数
        /// </summary>
        /// <param name="checkListId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpPut("{checkListId}")]
        public async Task<Result> GenrateParam(Guid checkListId, CancellationToken ct) 
        {
            var result = await _checkListAppService.CalculateParamAsync(checkListId,ct);

            return result;
        }

    }
}
