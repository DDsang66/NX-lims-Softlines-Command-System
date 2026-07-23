using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.CheckListContext;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Infrastructure.Service;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NX_lims_Softlines_Command_System.src.Application.Mappings
{
    public class ConditionMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            //entity=>responseDto
            config.NewConfig<ConditionPool, ConditionPoolResponseDto>()
                .MapWith(src => new ConditionPoolResponseDto
                {
                    ConditionPoolId = src.Id.Value,
                    Conditions = src.Conditions.ToDictionary()
                    ?? new Dictionary<string, object?>(),
                    CreatedAt = src.CreatedAt,
                    Status = src.Status.ToString()
                });

            //dto=>entity
            config.NewConfig<AddConditionPoolDto, ConditionPool>()
                .MapWith(src => ConditionPool.Create(
                    new CheckListId(src.CheckListId),
                    new Dictionary<string, object?>()
                    ));

            //聚合根=>数据库模型
            config.NewConfig<ConditionPool, src.Infrastructure.Data.Persistence.ConditionPool>()
                .Map(dest => dest.ConditionPoolId, src => src.Id.Value)
                .Map(dest => dest.CheckListId, src => src.CheckListId.Value)
                // 将字典转换为 JSON 字符串保存
                .Map(dest => dest.Conditions, src => JsonSerializer.Serialize(src.Conditions, new JsonSerializerOptions
                {
                    // 建议忽略 null 值，减少存储体积，并根据你的需求选择驼峰命名等
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = { new TypeConverter() } // 添加自定义的类型转换器
                }))
                .Map(dest => dest.TestPoints, src => JsonSerializer.Serialize(src.TestPoints, new JsonSerializerOptions
                {
                    // 使用数组格式存储，更简洁
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    // 使用 HashSet 的排序保证序列化结果的一致性
                    WriteIndented = false
                }))
                .Map(dest => dest.CreatedAt, src => src.CreatedAt)
                .Map(dest => dest.Status, src => (byte)src.Status);

            //数据库模型 => 聚合根 (使用 Reconstitute 重建)
            config.NewConfig<src.Infrastructure.Data.Persistence.ConditionPool, ConditionPool>()
                .MapWith(src => ConditionPool.Reconstitute(
                    new ConditionPoolId(src.ConditionPoolId),
                    new CheckListId(src.CheckListId),
                    // 反序列化 JSON 为字典，并处理空值情况
                    string.IsNullOrWhiteSpace(src.Conditions)
                        ? new Dictionary<string, object?>()
                        : JsonSerializer.Deserialize<Dictionary<string, object?>>(src.Conditions, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true // 忽略大小写，提高兼容性
                        })!,
                    string.IsNullOrWhiteSpace(src.TestPoints)
                        ? new HashSet<string>()
                        : JsonSerializer.Deserialize<HashSet<string>>(src.TestPoints, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true // 忽略大小写，提高兼容性
                        })!,
                    src.CreatedAt,
                    (ConditionPoolStatus)src.Status
                ));
        }
    }
}
