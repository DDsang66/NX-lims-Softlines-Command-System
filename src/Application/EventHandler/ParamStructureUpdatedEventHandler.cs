using MediatR;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Events;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;

namespace NX_lims_Softlines_Command_System.src.Application.EventHandler
{
    public class ParamStructureUpdatedEventHandler
        : INotificationHandler<ParamStructureUpdatedEvent>
    {
        private readonly IFormulaRepository _formulaRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ParamStructureUpdatedEventHandler(IFormulaRepository formulaRepository, IUnitOfWork unitOfWork)
        {
            _formulaRepository = formulaRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(ParamStructureUpdatedEvent notification, CancellationToken ct)
        {
            // 1. 根据 notification.ParamStructureId 查找关联的 Formula 聚合根
            var formulas = new List<Formula>(); 

            //formulas = await _formulaRepository.GetByParamStructureIdAsync(notification.ParamStructureId,ct);

            foreach (var formula in formulas)
            {
                // 2. 在 Formula 聚合根内部执行跨聚合的校验逻辑
                // 例如：校验 Formula 引用的参数是否在新的 Schema 中依然存在

                //formula.ValidateAgainstParamStructure(notification.UpdatedSchema);

                // 如果需要更新 Formula 自身的状态，也在此处调用对应方法
            }

            // 3. 保存变更
            await  _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
