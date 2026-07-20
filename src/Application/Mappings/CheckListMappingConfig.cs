using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.CheckListContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TestItemContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;
using System.Reflection;
using System.Text.Json;
using CheckList = NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.CheckList;
using CheckListItem = NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.CheckListItem;

namespace NX_lims_Softlines_Command_System.src.Application.Mappings
{
    public class CheckListMappingConfig : IRegister
    {

        public void Register(TypeAdapterConfig config)
        {
            //dto=>entity
            config.NewConfig<AddCheckListDto, CheckList>()
                .MapWith(src => CheckList.Create(
                    src.SourceId == null
                    ? null
                    : new OrderId(src.SourceId.Value),
                     src.Items.Select(i => i.Adapt<CheckListItem>()).ToList(),
                     src.Remark
                    ));

            config.NewConfig<CheckListItemDto, CheckListItem>()
                .Map(dest => dest.TestItemId, src => new TestItemId(src.TestItemId))
                .Map(dest => dest.StandardIds, src => src.StandardIds.Select(i => new StandardId(i)).ToList())
                .Map(dest => dest.TestGroup, src => (TestGroup)src.TestGroup);

            //entity=>数据库模型
            config.NewConfig<CheckList, src.Infrastructure.Data.Persistence.CheckList>()
                .MapWith(src => new src.Infrastructure.Data.Persistence.CheckList
                {
                    OrderId = src.OderId == null ? Guid.NewGuid() : src.OderId,
                    CheckListId = src.Id,
                    CreatedTime = src.CreatedTime,
                    Status = (byte)src.Status
                });

            // ========== CheckListItem 映射 ==========
            config.NewConfig<CheckListItem, src.Infrastructure.Data.Persistence.CheckListItem>()
                .MapWith(src => new src.Infrastructure.Data.Persistence.CheckListItem
                {
                    CheckListId = src.CheckListId,
                    CheckListItemId = src.Id,
                    TestItemId = src.TestItemId == null ? string.Empty : src.TestItemId,
                    StandardId = string.Join(",", src.StandardIds.Select(id => id)),
                    BuyerModifiedTestItem = src.BuyerModifiedTestItemId,
                    BuyerModifiedTestStandard = src.BuyerModifiedTextMethodId,
                    TestGroup = (byte)src.TestGroup,
                    Param = src.Param == null
                    ? null
                    : JsonSerializer.Serialize(src.Param.Values, new JsonSerializerOptions { WriteIndented = true }),
                    Samples = string.Join(",", src.Samples),
                    Status = (byte)src.Status
                });

            // ========== CheckList 反向映射 (数据库模型 => Entity) ==========
            //config.NewConfig<src.Infrastructure.Data.Persistence.CheckList, CheckList>()
            //    .MapWith(src => new CheckList
            //    {
            //        // 数据库的 CheckListId 映射回实体的 Id
            //        Id = src.CheckListId,

            //        // 数据库的 OrderId 映射回实体的 OderId (注意：你原代码拼写是 OderId)
            //        OderId = src.OrderId,

            //        CreatedTime = src.CreatedTime,

            //        // 将 byte 强转回枚举类型
            //        Status = (CheckListStatus)src.Status
            //    });

            // ========== CheckListItem 反向映射 (数据库模型 => Entity) ==========
            config.NewConfig<src.Infrastructure.Data.Persistence.CheckListItem, CheckListItem>()
                .MapWith(src => new CheckListItem
                {
                    CheckListId = new CheckListId(src.CheckListId),

                    // 还原空字符串为 null
                    TestItemId = string.IsNullOrEmpty(src.TestItemId) ? null : new TestItemId(src.TestItemId),

                    // 将逗号拼接的字符串拆分为集合 (假设 StandardIds 是 List<string> 或 IEnumerable<string>)
                    StandardIds = string.IsNullOrEmpty(src.StandardId)
                    ? Enumerable.Empty<StandardId?>()
                    : src.StandardId
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => (StandardId?)new StandardId(s)),

                    BuyerModifiedTestItemId = src.BuyerModifiedTestItem,
                    BuyerModifiedTextMethodId = src.BuyerModifiedTestStandard,

                    // 将 byte 强转回枚举类型
                    TestGroup = (TestGroup)src.TestGroup,

                    // 将 Json 字符串反序列化回对象 (假设 Param.Values 的类型是 Dictionary<string, string> 或其他具体类型)
                    // 请根据实际类型替换 Dictionary<string, object>
                    Param = ReconstructParamSet(src.Param),
                    // 将逗号拼接的字符串拆分为集合
                    Samples = string.IsNullOrEmpty(src.Samples)
                        ? new List<string>()
                        : src.Samples.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),

                    // 将 byte 强转回枚举类型
                    Status = (CheckListStatus)src.Status
                });

        }

        /// <summary>
        /// 辅助方法：从 JSON 字符串重建 ParamSet 值对象
        /// </summary>
        private static ParamSet? ReconstructParamSet(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                // 1. 将 JSON 反序列化为 Dictionary
                var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (dict == null || dict.Count == 0)
                    return null;

                // 方案 A：如果你在 ParamSet 中添加了 Reconstruct 方法，直接调用：
                return ParamSet.Reconstruct(dict);
            }
            catch (JsonException)
            {
                // 如果 JSON 格式损坏，根据业务需求决定是抛出异常还是返回空
                return null;
            }
        }
    }
}
