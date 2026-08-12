using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.TemplateContext;
using NX_lims_Softlines_Command_System.src.Application.Interface.TemplateContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Service.TemplateContext
{
    public class TemplateQueryService: IScopedDependency,ITemplateQueryService
    {
        private readonly ITemplateRepository _templateRepository;

        public TemplateQueryService(ITemplateRepository templateRepository)
        {
            _templateRepository = templateRepository;
        }

        /// <summary>
        /// 获取所有模板信息
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<List<TemplateResponseDto>>> GetAllTemplateAsync(CancellationToken ct) 
        {
            // 1. 从仓储中获取数据 (假设仓储已经直接返回了 DTO 列表)
            var templates = await _templateRepository.GetAllAsync(ct);

            // 2. 判断是否有数据
            if (templates == null || !templates.Any())
            {
                // 根据你项目中 Result 的定义，返回成功但带空集合，或返回特定 NotFound
                return Result<List<TemplateResponseDto>>.Ok(new List<TemplateResponseDto>());
            }
            var templatesDto = templates.Select(t => t.Adapt<TemplateResponseDto>()).ToList();

            // 3. 返回成功结果
            return Result<List<TemplateResponseDto>>.Ok(templatesDto);
        }
    }
}
