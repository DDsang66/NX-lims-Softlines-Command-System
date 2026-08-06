using NX_lims_Softlines_Command_System.Domain.Share.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using System.Threading.Tasks.Dataflow;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetContext
{
    public sealed class DataSheet:AggregateRoot<DataSheetId,Guid>
    {
        /// <summary>
        /// 当前datasheetUrl
        /// </summary>
        public string Url { get; private set; } = string.Empty;

        /// <summary>
        /// 引用模板的url
        /// </summary>
        public string ContactTemplateUrl { get; private set; } = string.Empty;

        /// <summary>
        /// 当前datasheet状态
        /// </summary>
        public DataSheetStatus Status { get; private set; } = DataSheetStatus.Unknown;

        /// <summary>
        /// 当前datasheet创建时间
        /// </summary>
        public DateTime CreateTime { get; private set; }

        /// <summary>
        /// checklistId
        /// </summary>
        public CheckListId CheckListId { get; private set; } 
    }
}
