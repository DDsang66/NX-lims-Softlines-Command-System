using MediatR;
using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Events;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

namespace NX_lims_Softlines_Command_System.src.Application.EventHandler
{
    public class DataSheetCompletedEventHandler : INotificationHandler<DataSheetCompletedEvent>
    {
        private readonly dbContext _db;
        private readonly IDataSheetRepository _dataSheetRepository;
        private readonly ICheckListRepository  _checkListRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DataSheetCompletedEventHandler> _logger;

        //用仓储


        public DataSheetCompletedEventHandler(
            dbContext db, 
            IDataSheetRepository dataSheetRepository,
            ICheckListRepository checkListRepository,
            IUnitOfWork unitOfWork,
            ILogger<DataSheetCompletedEventHandler> logger
            )
        {
            _db = db;
            _dataSheetRepository = dataSheetRepository;
            _checkListRepository = checkListRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task Handle(DataSheetCompletedEvent notification, CancellationToken ct)
        {
            var testItemId = notification.TestItemId;

            // 快速幂等查询：这个 TestItemId 下是否还有未完成的 DataSheet
            var hasUncompleted = await _db.DataSheets
                .AnyAsync(d => d.TestItemId == testItemId 
                            && d.CheckListId == notification.CheckListId
                            && d.Status != (int)DataSheetStatus.Completed, ct);

            if (hasUncompleted)
            {
                // 还有没完成的，什么都不做
                return;
            }

            // 2. 找到该 TestItemId 对应的 Checklist
            //    路径：ChecklistItem.TestItemId -> ChecklistItem.ChecklistId -> Checklist
            var checklist = await _checkListRepository.GetByIdAsync(new CheckListId(notification.CheckListId), ct);

            if (checklist is null)
            {
                _logger.LogWarning(
                    "ChecklistItem not found for TestItemId={TestItemId}", testItemId);
                return;
            }

            // 4. 调聚合根方法，由 Checklist 决定怎么改 item 状态
            checklist.MarkItemCompleted(testItemId);

            await _checkListRepository.UpdateAsync(checklist,ct);

            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
