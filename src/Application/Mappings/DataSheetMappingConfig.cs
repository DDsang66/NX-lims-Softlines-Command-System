using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.DataSheetConetxt;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetContext;

namespace NX_lims_Softlines_Command_System.src.Application.Mappings
{
    public class DataSheetMappingConfig: IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<DataSheet, DataSheetResponseDto>()
                .MapWith(src => new DataSheetResponseDto
                {
                    // 假设 DataSheetId 继承自某种强类型 Guid，其内部值属性为 Value
                    Id = src.Id.Value.ToString(),

                    // 注意：DTO 中拼写为 BacthId，领域对象中为 BatchId
                    // 假设 DataSheetBatchId 的内部值属性为 Value
                    BacthId = src.BatchId.Value.ToString(),

                    ReportNumber = src.ReportNumber,

                    // 假设 CheckListId 内部包含 Guid 类型的 Value 属性
                    CheckListId = src.CheckListId.Value,

                    // 假设 TestItemId 的内部值属性为 Value
                    TestItemId = src.TestItemId.Value.ToString(),

                    Url = BuildDownloadUrl(src.Url, src.EditorVersion),

                    EditorVersion = src.EditorVersion,

                    // 处理可空字符串
                    ModelKey = src.ModelKey ?? string.Empty,

                    // 处理可空时间类型：如果 UpdateTime 为 null，则赋默认值
                    // 你也可以根据业务需求改为 DateTime.UtcNow 或其他逻辑
                    UpdateTime = src.UpdateTime ?? default
                }); 
        }
        private static string BuildDownloadUrl(string? rawUrl, int editorVersion)
        {
            if (string.IsNullOrWhiteSpace(rawUrl)) return string.Empty;

            var clean = rawUrl.Replace('\\', '/').TrimStart('/');

            // 如果 rawUrl 里已经带了 ?v=，先剔除旧参数再拼新的
            var qIdx = clean.IndexOf('?');
            if (qIdx >= 0) clean = clean.Substring(0, qIdx);

            return $"/{clean}?v={editorVersion}";
        }
    }
}
