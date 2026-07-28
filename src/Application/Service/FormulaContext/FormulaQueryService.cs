using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamFormulaContext;
using NX_lims_Softlines_Command_System.src.Application.Interface.FormulaContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Service.FormulaContext
{
    public class FormulaQueryService:IFormulaQueryService,IScopedDependency
    {
        private readonly IFormulaRepository _formulaRepository;
        public FormulaQueryService(IFormulaRepository formulaRepository) 
        {
            _formulaRepository = formulaRepository;
        }

        /// <summary>
        /// 根据id查询公式
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<FormulaResponseDto>> GetFormulaByIdAsync(string id, CancellationToken ct) 
        {
            var formulaId = new FormulaId(id);

            var formula = await _formulaRepository.GetByIdAsync(formulaId, ct);

            var formulaDto = formula.Adapt<FormulaResponseDto>();

            return Result<FormulaResponseDto>.Ok(formulaDto);
        }

        /// <summary>
        /// 根据id查询公式
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<List<FormulaResponseDto>>> GetFormulasByIdsAsync(IEnumerable<string> ids, CancellationToken ct)
        {
            // 验证输入参数
            if (ids == null || !ids.Any())
            {
                return Result<List<FormulaResponseDto>>.Ok(new List<FormulaResponseDto>());
            }
            // 将字符串ID转换为FormulaId对象
            var formulaIds = ids.Select(id => new FormulaId(id)).ToList();

            // 从仓储获取所有匹配的公式
            var formulas = await _formulaRepository.GetByIdsAsync(formulaIds, ct);

            // 如果没有找到任何公式，返回空列表
            if (formulas == null || !formulas.Any())
            {
                return Result<List<FormulaResponseDto>>.Ok(new List<FormulaResponseDto>());
            }

            // 将实体映射为DTO
            var formulaDtos = formulas.Adapt<List<FormulaResponseDto>>();

            return Result<List<FormulaResponseDto>>.Ok(formulaDtos);
        }

        /// <summary>
        /// 查询所有公式
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<List<FormulaResponseDto>>> GetAllFormulaAsync(CancellationToken ct)
        {
            // 从仓储获取所有公式
            var formulas = await _formulaRepository.GetAllAsync(ct);

            // 如果没有找到任何公式，返回空列表
            if (formulas == null || !formulas.Any())
            {
                return Result<List<FormulaResponseDto>>.Ok(new List<FormulaResponseDto>());
            }

            // 将实体映射为DTO
            var formulaDtos = formulas.Adapt<List<FormulaResponseDto>>();

            return Result<List<FormulaResponseDto>>.Ok(formulaDtos);
        }
    }
}
