using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.StandardContext;
using NX_lims_Softlines_Command_System.src.Application.Interface.StandardContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Service.StandardContext
{
    public class StandardFamilyQueryService:IStandardFamilyQueryService,IScopedDependency
    {
        private readonly IStandardFamilyRepository _standardFamilyRepository;

        public StandardFamilyQueryService(IStandardFamilyRepository standardFamilyRepository) 
        {
            _standardFamilyRepository = standardFamilyRepository;
        }

        /// <summary>
        /// 查询单条标准
        /// </summary>
        /// <returns></returns>
        public async Task<Result<StandaradFamilyResponseDto>> GetStandardFamilyAsync(string id, CancellationToken ct)
        {
            var standardFamilyId = new StandardFamilyId(id);

            var standardFamily = await _standardFamilyRepository.GetByIdAsync(standardFamilyId, ct);

            var standardFamilyDto = standardFamily.Adapt<StandaradFamilyResponseDto>();

            return Result<StandaradFamilyResponseDto>.Ok(standardFamilyDto);
        }

        /// <summary>
        /// 查询所有标准族
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<List<StandaradFamilyResponseDto>>> GetStandardFamiliesAsync(CancellationToken ct)
        {
            var standardFamilies = await _standardFamilyRepository.GetAllStandardFamilyAsync(ct);

            var standardFamilyDtoList = standardFamilies.Adapt<List<StandaradFamilyResponseDto>>();

            return Result<List<StandaradFamilyResponseDto>>.Ok(standardFamilyDtoList);
        }

    }
}
