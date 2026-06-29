using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Service.QueryService
{
    public class StandardQueryService:IScopedDependency
    {
        private readonly IStandardRepository _standardRepository;
        public StandardQueryService(IStandardRepository standardRepository) 
        {
            _standardRepository = standardRepository;
        }

        /// <summary>
        /// 查询单条标准
        /// </summary>
        /// <returns></returns>
        public async Task<Result<StandardResponseDto>> GetStandardAsync (string id,CancellationToken ct)
        {
            var standardId = new StandardId(id);

            var standard = await _standardRepository.GetByIdAsync(standardId, ct);

            var standardDto = standard.Adapt<StandardResponseDto>();

            return Result<StandardResponseDto>.Ok(standardDto);
        }

        /// <summary>
        /// 查询所有标准
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<List<StandardResponseDto>>> GetStandardsAsync(CancellationToken ct) 
        {
            var standards = await _standardRepository.GetStandardListAsync(ct);
            
            var standardDtoList = standards.Adapt<List<StandardResponseDto>>();

            return Result<List<StandardResponseDto>>.Ok(standardDtoList);
        }

        /// <summary>
        /// 根据条件查询标准
        /// </summary>
        /// <param name="QueryCondition"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<List<StandardResponseDto>>> GetStandardByCodeAsync(StandardQueryConditionDto queryCondition, CancellationToken ct) 
        {
            //var standards = await _standardRepository.GetByConditionAsync(ct);

            //通过中间件将queryCondition转换为标准查询条件

            return Result<List<StandardResponseDto>>.Ok(new List<StandardResponseDto>());
        }
    }
}
