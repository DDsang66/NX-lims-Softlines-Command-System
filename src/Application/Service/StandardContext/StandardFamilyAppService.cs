using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repositories;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Service.StandardContext
{
    public class StandardFamilyAppService : IScopedDependency
    {
        private readonly IStandardFamilyRepository _standardFamilyRepository;
        private readonly IUnitOfWork _unitOfWork;

        public StandardFamilyAppService(IStandardFamilyRepository standardFamilyRepository,IUnitOfWork unitOfWork) 
        {
            _standardFamilyRepository = standardFamilyRepository;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// 添加标准族
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result> AddStandardFamilyAsync(CancellationToken ct) 
        {
            return Result.Ok();
        }

        public async Task<Result> UpdateStandardFamilyAsync(CancellationToken ct)
        {
            return Result.Ok();
        }
    }
}
