using Microsoft.EntityFrameworkCore.Migrations.Operations;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.StandardContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repositories;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository;

namespace NX_lims_Softlines_Command_System.src.Application.Service.StandardContext
{
    public class StandardFamilyAppService : IScopedDependency
    {
        private readonly IStandardFamilyRepository _standardFamilyRepository;
        private readonly IStandardRepository _standardRepository;
        private readonly IFormulaRepository _formulaRepository;
        private readonly IUnitOfWork _unitOfWork;

        public StandardFamilyAppService(IStandardFamilyRepository standardFamilyRepository, IStandardRepository standardRepository, IFormulaRepository formulaRepository, IUnitOfWork unitOfWork) 
        {
            _standardFamilyRepository = standardFamilyRepository;
            _standardRepository = standardRepository;
            _formulaRepository = formulaRepository;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// 添加标准族
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result> AddStandardFamilyAsync(StandardFamilyAddDto dto,CancellationToken ct) 
        {
            var standardFamilyId = new StandardFamilyId(dto.StandardFamilyId);

            var standardFamily = StandardFamily.Create(standardFamilyId,dto.StandardFamilyCode);

            await _standardFamilyRepository.AddAsync(standardFamily,ct);

            await _unitOfWork.SaveChangesAsync(ct);

            return Result.Ok();
        }

        /// <summary>
        /// 更新标准族自有字段
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result> UpdateStandardFamilyAsync(StandardFamilyUpdateDto dto, CancellationToken ct) 
        {
            var standardFamilyId = new StandardFamilyId(dto.StandardFamilyId);

            var standardFamily = await _standardFamilyRepository.GetByIdAsync(standardFamilyId,ct);


            if (standardFamily == null) return Result.Fail("标准族不存在");

            // 统一更新
            standardFamily.Update(
                standardFamilyCode: dto.StandardFamilyCode,
                effectiveDate: DateTime.UtcNow
            );

            await _standardFamilyRepository.UpdateAsync(standardFamily, ct);
            //standardFamily.Update();

            await _unitOfWork.SaveChangesAsync(ct);

            return Result.Ok();
        }

        /// <summary>
        /// 移除标准族
        /// </summary>
        /// <returns></returns>
        public async Task<Result> RemoveStandardFamilyAsync(string id, CancellationToken ct) 
        {
            var standardFamilyId = new StandardFamilyId(id);

            await _standardFamilyRepository.RemoveAsync(standardFamilyId,ct);

            //通知其他关联的聚合根修改自己的StandardFamilyId
            //1.引用新的StandardFamilyId
            //2.更改状态为草稿态或不可用，待后续重新持有StandardFamilyId后继续使用

            await _unitOfWork.SaveChangesAsync(ct);

            return Result.Ok();
        }

        /// <summary>
        /// 向标准族添加标准
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result> AddStandardToFamilyAsync(StandardFamilyUpdateDto dto, CancellationToken ct)
        {
            var standardFamilyId = new StandardFamilyId(dto.StandardFamilyId);

            // 1. 获取管理方聚合根
            var standardFamily = await _standardFamilyRepository.GetByIdAsync(standardFamilyId, ct);

            if (standardFamily == null) return Result.Fail("标准族不存在");

            // 2. 批量获取被管理方聚合根（解决 N+1 查询问题）
            var standardIds = dto.StandardIds.Select(id => new StandardId(id)).ToList();

            var standards = await _standardRepository.GetByIdsAsync(standardIds, ct);

            // 3. 循环处理每个标准的绑定
            foreach (var standard in standards)
            {
                // 3.1 StandardFamily 记录意图（内部校验是否已存在、数量限制等业务规则）
                standardFamily.AddStandard(standard.Id); // 假设 AddStandard 需要 long/Guid

                // 3.2 Standard 修改自身的状态（更新 StandardFamilyId 外键指向，及自身状态流转）
                standard.BindToStandardFamily(standardFamilyId);
            }

            // 4. StandardFamily 自身的版本更新
            standardFamily.UpdateVersion();

            // 5. 仓储更新（将两个聚合的变更告知 ORM）
            await _standardFamilyRepository.UpdateAsync(standardFamily, ct);

            // 批量更新 Standard (推荐仓储提供批量更新方法，提升性能)
            await _standardRepository.UpdateRangeAsync(standards, ct);

            // 6. 统一提交事务，保证强一致性
            await _unitOfWork.SaveChangesAsync(ct);

            return Result.Ok();
        }

        /// <summary>
        /// 向标准族添加公式
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result> AddFormulaToFamilyAsync(StandardFamilyUpdateDto dto, CancellationToken ct)
        {
            var standardFamilyId = new StandardFamilyId(dto.StandardFamilyId);

            // 1. 获取管理方聚合根
            var standardFamily = await _standardFamilyRepository.GetByIdAsync(standardFamilyId, ct);

            if (standardFamily == null) return Result.Fail("标准族不存在");

            // 2. 获取被管理方聚合根集合 (注意：如果 dto.FormulaIds 很多，建议仓储提供批量查询方法)
            var formulaIds = dto.FormulaIds.Select(id => new FormulaId(id)).ToList();

            var formulas = await _formulaRepository.GetByIdsAsync(formulaIds, ct);

            // 3. 循环处理每个公式的绑定
            foreach (var formula in formulas)
            {
                // 3.1 StandardFamily 记录意图（校验是否已存在等业务规则）
                standardFamily.AddFormula(formula.Id);

                // 3.2 Formula 修改自身的状态（更新外键指向，以及自身状态流转）
                //formula.AddStandardFamily(standardFamilyId);
            }

            // 4. StandardFamily 自身的版本更新
            standardFamily.UpdateVersion();

            // 5. 仓储更新（将两个聚合的变更告知 ORM）
            await _standardFamilyRepository.UpdateAsync(standardFamily, ct);

            // 批量更新 Formula (如果 ORM 支持变更追踪，也可以在循环内逐个 Update)
            await _formulaRepository.UpdateRangeAsync(formulas, ct);

            // 6. 统一提交事务，保证强一致性
            await _unitOfWork.SaveChangesAsync(ct);

            return Result.Ok();
        }

        public async Task<Result> AddStructureToFamilyAsync(StandardFamilyUpdateDto dto, CancellationToken ct)
        {
            return Result.Ok();
        }

        public async Task<Result> AddRuleToFamilyAsync(StandardFamilyUpdateDto dto, CancellationToken ct)
        {
            return Result.Ok();
        }

     }
}
