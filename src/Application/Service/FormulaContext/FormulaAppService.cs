using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamFormulaContext;
using NX_lims_Softlines_Command_System.src.Application.Interface.FormulaContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;
using System.Drawing.Printing;

namespace NX_lims_Softlines_Command_System.src.Application.Service.FormulaContext
{
    public class FormulaAppService: IFormulaAppService,IScopedDependency
    {
        private readonly IFormulaRepository _formulaRepository;
        private readonly IUnitOfWork _unitOfWork;
        public FormulaAppService(IFormulaRepository formulaRepository,IUnitOfWork unitOfWork) 
        {
            _formulaRepository = formulaRepository;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Add new formula
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result> AddFormulaAsync(AddFormulaDto dto,CancellationToken ct)
        {
            var formulaId = new FormulaId(dto.FormulaId);

            var formula = Formula.Create(
                formulaId,
                dto.Name,
                dto.ParamName,
                dto.StandardFamilyIds?
                .Select(id => new StandardFamilyId(id))
                ?? new List<StandardFamilyId?>(),
                dto.ParamStructureIds?
                .Select(id => new ParamStructureId(id)) 
                ?? new List<ParamStructureId?>(),
                dto.ConditionFields,
                dto.ExpressionTemplate,
                dto.Description);

            await _formulaRepository.AddAsync(formula, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            return Result.Ok();
        }

        /// <summary>
        /// 更新公式
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result> UpdateFormulaAsync(UpdateFormulaDto dto, CancellationToken ct) 
        {
            var formulaId = new FormulaId(dto.FormulaId);

            var formula = await _formulaRepository.GetByIdAsync(formulaId, ct);

            formula.Update(dto.Name,dto.ParamName,dto.ConditionFields,dto.ExpressionTemplate,dto.Description);

            await _formulaRepository.UpdateAsync(formula, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            return Result.Ok();
        }

        /// <summary>
        /// 激活公式
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result> ActiveFormulaAsync(string id, CancellationToken ct) 
        {
            var formulaId = new FormulaId(id);

            var formula = await _formulaRepository.GetByIdAsync(formulaId, ct);

            //注入领域服务进行验证

            formula.Activate();

            await _formulaRepository.UpdateAsync(formula, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            return Result.Ok();
        }

        /// <summary>
        /// 删除公式
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result> DeleteFormulaAsync(string id, CancellationToken ct) 
        {
            return Result.Ok();
        }

     }
}
