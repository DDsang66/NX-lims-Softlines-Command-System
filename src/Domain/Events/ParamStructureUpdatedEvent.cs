using MediatR;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;

namespace NX_lims_Softlines_Command_System.src.Domain.Events
{
    /// <summary>
    /// 领域事件：参数结构已更新
    /// </summary>
    public record ParamStructureUpdatedEvent : DomainEvent, INotification
    {
        public ParamStructureId ParamStructureId { get; init; }
        public string ParamName { get; init; }
        public ParamSchema UpdatedSchema { get; init; }

        public ParamStructureUpdatedEvent(
            ParamStructureId paramStructureId,
            string paramName,
            ParamSchema updatedSchema)
            : base(paramStructureId)
        {
            ParamStructureId = paramStructureId;
            ParamName = paramName;
            UpdatedSchema = updatedSchema;
        }
    }
}
