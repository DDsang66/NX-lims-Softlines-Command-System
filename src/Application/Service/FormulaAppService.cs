using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repositories;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using System.Drawing.Printing;

namespace NX_lims_Softlines_Command_System.src.Application.Service
{
    public class FormulaAppService
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
                dto.ConditionFields,
                new StandardFamilyId(dto.StandardFamilyId),
                dto.ExpressionTemplate,
                dto.Description);

            await _formulaRepository.AddAsync(formula, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            return Result.Ok();
        }




     }
}
