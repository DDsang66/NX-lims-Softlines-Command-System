using NX_lims_Softlines_Command_System.Domain.Aggregeates.Standard;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.StandardContext;
using NX_lims_Softlines_Command_System.src.Application.Interface.StandardContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repositories;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Domain.Share.Enums;

namespace NX_lims_Softlines_Command_System.src.Application.Service.StandardContext
{
    public class StandardAppService: IStandardAppService, IScopedDependency
    {
        private readonly IStandardRepository _standardRepository;
        private readonly IUnitOfWork _unitOfWork;
        public StandardAppService(IStandardRepository standardRepository, IUnitOfWork unitOfWork)
        {
            _standardRepository = standardRepository;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// 新增标准
        /// </summary>
        /// <returns></returns>
        public async Task<Result> AddStandardAsync(StandardAddDto dto, CancellationToken ct)
        {
            var standardId = new StandardId(dto.StandardId);

            var standardFamilyCode = string.IsNullOrEmpty(dto.StandardFamilyCode)
                ? null
                : new StandardFamilyId(dto.StandardFamilyCode);

            // 将前端传入的状态字符串转为枚举，解析失败时默认使用 Draft（防止非法输入导致异常）
            var status = Enum.TryParse<Status>(dto.Status, out var parsed) ? parsed : Status.Draft;
            // 调用聚合根工厂方法创建 Standard 实体，传入解析后的状态值
            var standard = Standard.Create(standardId, dto.StandardCode, standardFamilyCode,dto.StandardNameEn,dto.StandardNameCn,status);
            
            await _standardRepository.AddAsync(standard, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            return Result.Ok();
        }

        /// <summary>
        /// 移除标准
        /// </summary>
        /// <returns></returns>
        public async Task<Result> RemoveStandardAsync(string id,CancellationToken ct) 
        {
            var standardId = new StandardId(id);

            await _standardRepository.RemoveAsync(standardId, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            return Result.Ok();
        }

        /// <summary>
        /// 更新标准信息
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result> UpdateStandardAsync(StandardUpdateDto dto, CancellationToken ct) 
        {
            var standardId = new StandardId(dto.StandardId);

            var standard = await _standardRepository.GetByIdAsync(standardId, ct);

            var standardFamilyCode = string.IsNullOrEmpty(dto.StandardFamilyCode)
                ? null
                : new StandardFamilyId(dto.StandardFamilyCode);

            standard.Update(dto.StandardCode,standardFamilyCode, dto.StandardNameEn,dto.StandardNameCn);

            // 处理状态变更
            if (!string.IsNullOrEmpty(dto.Status) && Enum.TryParse<Status>(dto.Status, out var newStatus) && newStatus != standard.Status)
            {
                switch (newStatus)
                {
                    case Status.Active: standard.Activate(); break;
                    case Status.Draft: standard.Draft(); break;
                    case Status.Deprecated: standard.Deprecated(); break;
                    case Status.Superseded: standard.Superseded(); break;
                    case Status.Pending: standard.Pending(); break;
                }
            }

            await _standardRepository.UpdateAsync(standard, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            return Result.Ok();
        }

        /// <summary>
        /// 激活标准
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result> ActiveStandardAsync(string id,CancellationToken ct) 
        {
            var standardId = new StandardId(id);

            var standard = await _standardRepository.GetByIdAsync(standardId, ct);

            standard.Activate();

            await _standardRepository.UpdateAsync(standard, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            return Result.Ok();
        }

        /// <summary>
        /// 将标准转变为草稿
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result> DraftStandardAsync(string id, CancellationToken ct)
        {
            var standardId = new StandardId(id);

            var standard = await _standardRepository.GetByIdAsync(standardId, ct);

            standard.Draft();

            await _standardRepository.UpdateAsync(standard, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            return Result.Ok();
        }

        /// <summary>
        /// 将标准转变为草稿
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result> DeprecatedStandardAsync(string id, CancellationToken ct)
        {
            var standardId = new StandardId(id);

            var standard = await _standardRepository.GetByIdAsync(standardId, ct);

            standard.Deprecated();

            await _standardRepository.UpdateAsync(standard, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            return Result.Ok();
        }
    }
}
