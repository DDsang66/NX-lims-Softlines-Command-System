using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.CheckListContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TestItemContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.Enums;
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
                    TestPointParams = JsonSerializer.Serialize(
                        src.TestPointParams.ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value.Values// 如果 ParamSet 有 Values 属性
                        ),
                        new JsonSerializerOptions { WriteIndented = true }
                    ),
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
                .MapWith(src => CheckListItem.Reconstitute(
                    src.CheckListItemId,
                    new CheckListId(src.CheckListId),
                    string.IsNullOrEmpty(src.TestItemId) 
                    ? null 
                    : new TestItemId(src.TestItemId),
                    string.IsNullOrEmpty(src.StandardId)
                        ? new List<StandardId>()
                        : src.StandardId
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => new StandardId(s))
                            .ToList(),
                    src.BuyerModifiedTestItem ?? string.Empty,
                    src.BuyerModifiedTestStandard ?? string.Empty,
                    (TestGroup)src.TestGroup,
                    ReconstructTestPointParams(src.TestPointParams),
                    string.IsNullOrEmpty(src.Samples)
                        ? new List<string>()
                        : src.Samples.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                    (CheckListStatus)src.Status
                ));
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

        /// <summary>
        /// 辅助方法：重建 TestPointParams 字典
        /// </summary>
        private static Dictionary<string, ParamSet?> ReconstructTestPointParams(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new Dictionary<string, ParamSet?>();

            try
            {
                var dict = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object?>>>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (dict == null)
                    return new Dictionary<string, ParamSet?>();

                // 创建一个新的字典，使用 AddOrUpdateTestPointParam 方法
                var result = new Dictionary<string, ParamSet?>();
                foreach (var kvp in dict)
                {
                    var paramSet = kvp.Value == null
                        ? null
                        : ParamSet.Reconstruct(kvp.Value);
                    result.Add(kvp.Key, paramSet);
                }

                return result;
            }
            catch (JsonException)
            {
                // 如果 JSON 格式损坏，返回空字典
                return new Dictionary<string, ParamSet?>();
            }
        }
    }
}
