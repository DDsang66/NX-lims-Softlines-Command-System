using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Application.Interface.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;

namespace NX_lims_Softlines_Command_System.src.Application.Service.ParamStructureContext
{
    public class ParamStructureAppService: IParamStructureAppService,IScopedDependency
    {
        private readonly IParamStructureRepository _paramStructureRepository;
        private readonly IParamStructureValidateService _paramStructureValidateService;
        private readonly IFormulaRepository _formulaRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ParamStructureAppService(
            IParamStructureRepository paramStructureRepository,
            IParamStructureValidateService paramStructureValidateService,
            IFormulaRepository formulaRepository,
            IUnitOfWork unitOfWork)
        {
            _paramStructureRepository = paramStructureRepository;
            _paramStructureValidateService = paramStructureValidateService;
            _formulaRepository = formulaRepository;
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

            var paramStructure = dto.Adapt<ParamStructure>();//已于Mapping调用工厂Create聚合根

            await  _paramStructureRepository.AddAsync(paramStructure, ct);

            await _unitOfWork.SaveChangesAsync();

            return Result.Ok();
        }

        /// <summary>
        /// 更新自身参数结构
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result> UpdateParamStructureAsync(UpdateParamStructureDto dto, CancellationToken ct)
        {
            var paramStructureId = new ParamStructureId(dto.ParamStructureId);

            var paramStructure = await _paramStructureRepository.GetByIdAsync(paramStructureId, ct);

            var schema = dto.ParamSchema.Adapt<ParamSchema>();

            paramStructure.Update(dto.ParamName, schema);

            await  _paramStructureRepository.UpdateAsync(paramStructure, ct);

            await  _unitOfWork.SaveChangesAsync(ct);

            return Result.Ok();
        }

        /// <summary>
        /// 删除参数结构
        /// </summary>
        /// <param name="paramStructureId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result> RemoveParamStructureAsync(string paramStructureId, CancellationToken ct) 
        {
            var paramStructure = await _paramStructureRepository.GetByIdAsync(new ParamStructureId(paramStructureId), ct);

            if (paramStructure == null)
            {
                return Result.Fail("参数结构不存在");
            }

            await _paramStructureRepository.RemoveAsync(paramStructure.Id, ct);

            await  _unitOfWork.SaveChangesAsync();

            return Result.Ok();
        }

        /// <summary>
        /// 激活参数结构
        /// </summary>
        /// <param name="paramStructureId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result> ActiveParamStructureAsync(string paramStructureId, CancellationToken ct) 
        {
            var paramStructure = await _paramStructureRepository.GetByIdAsync(new ParamStructureId(paramStructureId), ct);

            var formula = await _formulaRepository.GetByIdAsync(new FormulaId(paramStructure.FormulaId), ct);

            if (paramStructure == null)
            {
                return Result.Fail("参数结构不存在");
            }
            
            var isOk = _paramStructureValidateService.Validate(formula, paramStructure);

            if (!isOk.IsSuccess) 
            {
                return Result.Fail(isOk.Error);
            }

            paramStructure.Active();

            await _paramStructureRepository.UpdateAsync(paramStructure, ct);

            await _unitOfWork.SaveChangesAsync();

            return Result.Ok();
        }
    }
}
