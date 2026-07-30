using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.TestItemContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TestItemContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TestItemContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share.Enums;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;
using System.Text.Json;

namespace NX_lims_Softlines_Command_System.src.Application.Mappings
{
    public class TestItemMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // ========== 新增 DTO => 领域模型 ==========
            // 将前端传入的 AddTestItemDto 转化为领域聚合根 TestItem
            config.NewConfig<AddTestItemDto, TestItem>()
                // 使用 MapWith 指定聚合根的工厂方法/构造函数，避免 Mapster 直接反射赋值破坏领域封装
                .MapWith(src => TestItem.Create(
                    new TestItemId(src.TestItemId),
                    src.TestItemNameEn,
                    src.TestItemNameChn,   
                    src.Description,
                    src.IsFeasible,
                    (TestGroup)src.Group,
                    (Status)src.Status
                ));

            config.NewConfig<TestItem, BasicItem>()
                .Map(dest => dest.IdItem, src => src.Id.Value)
                .Map(dest => dest.ItemNameEn, src => src.NameEn)
                .Map(dest => dest.ItemNameChn, src => src.NameChn)
                .Map(dest => dest.Description, src => src.Description)
                .Map(dest => dest.Status, src => (byte)src.Status)
                .Map(dest => dest.TestGroup, src => src.Group)
                .Map (dest => dest.IsFeasible, src => src.IsFeasible)
                .Map(dest => dest.Status, src => src.Status)
                .Map(dest => dest.ParamRequireDenfinition, src => JsonSerializer.Serialize(src.ParamRequireDefinitions, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = false // 作为单个字段存储，通常不需要缩进
                }));


            config.NewConfig<BasicItem, TestItem>()
                .MapWith(src => TestItem.Reconstitute(
                    new TestItemId(src.IdItem),
                    src.ItemNameEn,
                    src.ItemNameChn,
                    string.IsNullOrEmpty(src.Description) ? string.Empty : src.Description,
                    src.IsFeasible,
                    (TestGroup)src.TestGroup,
                    (Status)src.Status,
                    string.IsNullOrWhiteSpace(src.ParamRequireDenfinition)
                    ? new List<ParamRequireDefinition>()
                    : JsonSerializer.Deserialize<List<ParamRequireDefinition>>(src.ParamRequireDenfinition, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<ParamRequireDefinition>()
                ));

        }
    }
}
