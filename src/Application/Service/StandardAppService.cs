using NX_lims_Softlines_Command_System.Domain.Aggregeates.Standard;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repositories;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Service
{
    public class StandardAppService: IScopedDependency
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

            var standardFamilyCode = new StandardFamilyId(dto.StandardFamilyCode);

            var standard = Standard.Create(standardId, dto.StandardCode, standardFamilyCode,dto.StandardNameEn,dto.StandardNameCn);
            
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
            return Result.Ok();
        }

        /// <summary>
        /// 更新标准信息
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result> UpdateStandardAsync(string id, CancellationToken ct) 
        {
            return Result.Ok();
        }

        /// <summary>
        /// 激活标准
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result> ActiveStandardAsync(CancellationToken ct) 
        {
            return Result.Ok();
        }
    }
}
