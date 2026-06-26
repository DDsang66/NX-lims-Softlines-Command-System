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

            //var standard = await _standardRepository.GetByIdAsync(standardId, ct);

            return Result<StandardResponseDto>.Ok(new StandardResponseDto());
        }

        /// <summary>
        /// 查询所有标准
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<StandardResponseDto>> GetStandardsAsync(CancellationToken ct) 
        {
            //var standards = await _standardRepository.GetStandardAsync(ct);

            return Result<StandardResponseDto>.Ok(new StandardResponseDto());
        }

        /// <summary>
        /// 根据条件查询标准
        /// </summary>
        /// <param name="QueryCondition"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<StandardResponseDto>> GetStandardByCodeAsync(StandardQueryConditionDto queryCondition, CancellationToken ct) 
        {
            //var standards = await _standardRepository.GetByConditionAsync(ct);

            return Result<StandardResponseDto>.Ok(new StandardResponseDto());
        }
    }
}
