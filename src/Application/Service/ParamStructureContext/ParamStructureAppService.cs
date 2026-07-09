using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repositories;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Service.ParamStructureContext
{
    public class ParamStructureAppService: IParamStructureAppService,IScopedDependency
    {
        private readonly IParamStructureRepository _paramStructureRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ParamStructureAppService(IParamStructureRepository paramStructureRepository, IUnitOfWork unitOfWork)
        {
            _paramStructureRepository = paramStructureRepository;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// 添加参数结构
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result> AddParamStructureAsync(AddParamStructureDto dto, CancellationToken ct) 
        {
            var paramStructureId = new ParamStructureId(dto.ParamStructureId);

            var paramStructure = dto.Adapt<ParamStructure>();//已与Mapping调用工厂Create聚合根

            await  _paramStructureRepository.AddAsync(paramStructure, ct);

            await _unitOfWork.SaveChangesAsync();

            return Result.Ok();
        }



    }
}
