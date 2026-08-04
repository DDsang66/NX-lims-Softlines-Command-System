using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Application.Interface.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Service.ParamStructureContext
{
    public class ParamStructureQueryService:IScopedDependency,IParamStructureQueryService
    {
        private readonly IParamStructureRepository _paramStructureRepository;

        public ParamStructureQueryService(IParamStructureRepository paramStructureRepository) 
        {
            _paramStructureRepository = paramStructureRepository;
        }

        /// <summary>
        /// 获取参数结构列表
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<ParamStructureResponseDto>> GetParamStructureAsync(string paramStructureId, CancellationToken ct)
        {
            var paramStructure = await _paramStructureRepository.GetByIdAsync(new ParamStructureId(paramStructureId), ct);

            var dto = paramStructure.Adapt<ParamStructureResponseDto>();

            return Result<ParamStructureResponseDto>.Ok(dto);
        }

        /// <summary>
        /// 获取参数结构列表
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<List<ParamStructureResponseDto>>> GetAllStructureAsync(CancellationToken ct)
        {
            var paramStructure = await _paramStructureRepository.GetAllAsync(ct);

            var dtoList = paramStructure.Adapt<List<ParamStructureResponseDto>>();

            return Result<List<ParamStructureResponseDto>>.Ok(dtoList);
        }

        /// <summary>
        /// 根据标准族获取参数结构列表
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<List<ParamStructureResponseDto>>> GetByFamilyIdAsync(string familyId,CancellationToken ct)
        {
            var paramStructure = await _paramStructureRepository.GetByFamilyIdAsync(new StandardFamilyId(familyId),ct);

            var dtoList = paramStructure.Adapt<List<ParamStructureResponseDto>>();

            return Result<List<ParamStructureResponseDto>>.Ok(dtoList);
        }

        /// <summary>
        /// 根据标准族获取参数结构列表
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<List<ParamStructureResponseDto>>> GetByParamNameAsync(string paramName, CancellationToken ct)
        {
            var paramStructure = await _paramStructureRepository.GetByParamName(paramName, ct);

            var dtoList = paramStructure.Adapt<List<ParamStructureResponseDto>>();

            return Result<List<ParamStructureResponseDto>>.Ok(dtoList);
        }
    }
}
