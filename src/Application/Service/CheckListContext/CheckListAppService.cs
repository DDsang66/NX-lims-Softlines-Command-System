using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.CheckListContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repositories;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Service.CheckListContext
{
    public class CheckListAppService:IScopedDependency
    {
        private readonly IUnitOfWork _unitOfWork;

        public CheckListAppService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// 添加清单
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result> AddCheckList(AddCheckListDto dto,CancellationToken ct) 
        {
            var checkList = dto.Adapt<CheckList>();

            Console.WriteLine(checkList);

            //await  _checkListRepository.AddAsync(checkList,ct);

            await  _unitOfWork.SaveChangesAsync(ct);

            return Result.Ok();
        }
    }
}
