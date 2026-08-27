using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.BuyerContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share.Enums;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;
using System.Globalization;
using System.Text.Json;

namespace NX_lims_Softlines_Command_System.src.Application.Mappings
{
    public class ParamStructureMappingConfig:IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // DTO → 领域模型
            config.NewConfig<AddParamStructureDto, ParamStructure>()
                .MapWith(src => ParamStructure.Create(
                    new ParamStructureId(src.ParamStructureId),
                    src.StandardFamilyIds == null
                    ? new List<StandardFamilyId>()
                    : src.StandardFamilyIds.Select(id => new StandardFamilyId(id)).ToList(),
                    new FormulaId(src.FormulaId),
                    src.ParamName,
                    src.ParamSchema.Adapt<ParamSchema>(),  // Mapster 递归映射
                    src.RuleIds == null
                    ? new List<ParamRuleId>()
                    : src.RuleIds.Select(id => new ParamRuleId(id)).ToList(),
                    src.BuyerIds == null
                    ? new List<BuyerId>()
                    : src.BuyerIds.Select(id => new BuyerId(id)).ToList(),
                    ParseEngineLayer(src.EngineLayer),
                    src.EffectiveDate
                ));

            // SchemaDto → ParamSchema
            config.NewConfig<SchemaDto, ParamSchema>()
                .MapWith(src => ParamSchema.Create(
                    src.RequiredParam.Adapt<ParamDefinition>(),
                    src.ConditionRequirements.Select(c => c.Adapt<ConditionRequirement>()),
                    src.Limitations.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Adapt<ParamLimitation>()
                    )
                ));

            // ParamDefinitionDto → ParamDefinition
            config.NewConfig<ParamDefinitionDto, ParamDefinition>()
                .Map(dest => dest.Name, src => src.Name)
                .Map(dest => dest.ValueTypeName, src => NormalizeTypeName(src.ValueType)) // 替换此处
                .Map(dest => dest.Description, src => src.Description)
                .Map(dest => dest.IsNullable, src => src.IsNullable)
                .Map(dest => dest.DefaultValue, src => ConvertToStrongType(src.DefaultValue, NormalizeTypeName(src.ValueType)));

            // ConditionRequirementDto → ConditionRequirement
            config.NewConfig<ConditionRequirementDto, ConditionRequirement>()
                .Map(dest => dest.FieldName, src => src.FieldName)
                .Map(dest => dest.ValueTypeName, src => NormalizeTypeName(src.FieldType)) // 替换此处
                .Map(dest => dest.IsRequired, src => src.IsRequired)
                .Map(dest => dest.AllowedValues, src => src.AllowedValues != null
                    ? ConvertToStrongTypeList(src.AllowedValues, NormalizeTypeName(src.FieldType))
                    : new List<object?>());

            // ParamLimitationDto → ParamLimitation
            config.NewConfig<ParamLimitationDto, ParamLimitation>()
                .Map(dest => dest.ValueTypeName, src => NormalizeTypeName(src.ValueType)) // 替换此处

                .Map(dest => dest.AllowedValues, src => src.AllowedValues != null
                    ? ConvertToStrongTypeList(src.AllowedValues, NormalizeTypeName(src.ValueType))
                    : new List<object?>())
                .Map(dest => dest.Min, src => ConvertToStrongType(src.Min, NormalizeTypeName(src.ValueType)))
                .Map(dest => dest.Max, src => ConvertToStrongType(src.Max, NormalizeTypeName(src.ValueType)));

            // 领域模型 → 数据库模型
            config.NewConfig<ParamStructure, BasicParamStructure>()
                .Map(dest => dest.ParamStructureId, src => src.Id.Value)
                .Map(dest => dest.ParamName, src => src.ParamName)
                .Map(dest => dest.Schema,
                src => JsonSerializer.Serialize(
                    src.Schema,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }))
                .Map(dest => dest.EffectiveDate, src => src.EffectiveDate);


            // ==========================================
            // 新增配置：聚合根 → ParamStructureResponseDto (反向转化)
            // ==========================================
            config.NewConfig<ParamStructure, ParamStructureResponseDto>()
                // 基础属性提取
                .Map(dest => dest.ParamStructureId, src => src.Id.Value)
                .Map(dest => dest.ParamName, src => src.ParamName)
                .Map(dest => dest.FormulaId, src => src.FormulaId.Value)
                .Map(dest => dest.EffectiveDate, src => src.EffectiveDate)
                .Map(dest => dest.EngineLayer, src => src.EngineLayer.ToString())
                // 集合属性提取并转化为 List<Guid>
                .Map(dest => dest.StandardFamilyIds,
                     src => src.StandardFamilyIds != null
                            ? src.StandardFamilyIds.Select(id => id.Value).ToList()
                            : new List<string>())
                .Map(dest => dest.RuleIds,
                     src => src.ApplicableRuleIds != null
                            ? src.ApplicableRuleIds.Select(id => id.Value).ToList()
                            : new List<string>())
                .Map(dest => dest.BuyerCodes,
                     src => src.BuyerIds != null
                            ? src.BuyerIds.Select(id => id.Value).ToList()
                            : new List<string>())   
                // 将 ParamSchema 值对象映射回 SchemaDto
                .Map(dest => dest.ParamSchema, src => src.Schema.Adapt<SchemaDto>());


            // 领域值对象 ParamSchema → SchemaDto (反向映射)
            config.NewConfig<ParamSchema, SchemaDto>()
                .Map(dest => dest.RequiredParam, src => src.RequiredParam.Adapt<ParamDefinitionDto>())
                .Map(dest => dest.ConditionRequirements, src => src.ConditionRequirements.Select(c => c.Adapt<ConditionRequirementDto>()).ToList())
                .Map(dest => dest.Limitations, src => src.Limitations.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Adapt<ParamLimitationDto>()
                    ));

            // 领域值对象 ParamDefinition → ParamDefinitionDto (反向映射)
            config.NewConfig<ParamDefinition, ParamDefinitionDto>()
                .Map(dest => dest.Name, src => src.Name)
                .Map(dest => dest.ValueType, src => src.ValueTypeName) // 注意反向映射字段名对应
                .Map(dest => dest.Description, src => src.Description)
                .Map(dest => dest.IsNullable, src => src.IsNullable)
                .Map(dest => dest.DefaultValue, src => src.DefaultValue);

            // 领域值对象 ConditionRequirement → ConditionRequirementDto (反向映射)
            config.NewConfig<ConditionRequirement, ConditionRequirementDto>()
                .Map(dest => dest.FieldName, src => src.FieldName)
                .Map(dest => dest.FieldType, src => src.ValueTypeName) // 注意反向映射字段名对应
                .Map(dest => dest.IsRequired, src => src.IsRequired)
                .Map(dest => dest.AllowedValues, src => src.AllowedValues);

            // 领域值对象 ParamLimitation → ParamLimitationDto (反向映射)
            config.NewConfig<ParamLimitation, ParamLimitationDto>()
                .Map(dest => dest.ValueType, src => src.ValueTypeName) // 注意反向映射字段名对应
                .Map(dest => dest.AllowedValues, src => src.AllowedValues)
                .Map(dest => dest.Min, src => src.Min)
                .Map(dest => dest.Max, src => src.Max);

            // ==========================================
            // 补充：数据库模型 → 聚合根 ParamStructure (如果查询时需要)
            // ==========================================
            // 如果你的查询逻辑是通过 EF Core 读出 BasicParamStructure 再转为聚合根，可以配置这段
            /*
            config.NewConfig<BasicParamStructure, ParamStructure>()
                .MapWith(src => ParamStructure.Create(
                    new ParamStructureId(src.ParamStructureId),
                    new List<StandardFamilyId>(), // 数据库表若未存储关联，则默认空集合
                    new FormulaId(Guid.Empty),    // 视数据库结构而定
                    src.ParamName,
                    DeserializeSchema(src.Schema), // 反序列化 JSON
                    new List<ParamRuleId>(),
                    src.EffectiveDate
                ));
            */
        }

        private static string NormalizeTypeName(string? typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                return "System.String"; // 默认回退为 String

            // 去除前后空格并转为小写进行匹配
            var lowerType = typeName.Trim().ToLowerInvariant();

            // 映射常见 C# 关键字到对应的 System 类型全名
            return lowerType switch
            {
                "string" or "str" => "System.String",
                "int" or "integer" => "System.Int32",
                "long" => "System.Int64",
                "float" => "System.Single",
                "double" => "System.Double",
                "decimal" => "System.Decimal",
                "bool" or "boolean" => "System.Boolean",
                "datetime" or "date" => "System.DateTime",
                "guid" => "System.Guid",
                "byte" => "System.Byte",
                "short" => "System.Int16",
                "uint" => "System.UInt32",
                "ulong" => "System.UInt64",
                "ushort" => "System.UInt16",
                "sbyte" => "System.SByte",
                "char" => "System.Char",
                "timespan" => "System.TimeSpan",
                "datetimeoffset" => "System.DateTimeOffset",
                // 如果传入的已经是 System.xxx 格式，或者是不在上述列表中的自定义类型，直接返回原值（需确保首字母大写等规范可由业务容忍）
                _ => typeName.Trim()
            };
        }

        /// <summary>
        /// 将 object 值根据目标类型全名转换为强类型 object
        /// </summary>
        private static object? ConvertToStrongType(object? value, string? typeName)
        {
            if (value == null) return null;
            if (string.IsNullOrWhiteSpace(typeName)) return value; // 无类型声明，保持原样

            try
            {
                var targetType = Type.GetType(typeName);
                if (targetType == null) return value; // 找不到 CLR 类型，保持原样

                // 如果类型已经匹配，直接返回（避免不必要的转换）
                if (value.GetType() == targetType) return value;

                // 处理布尔值的特殊大小写（如果前端传过来的是字符串 "true"）
                if (targetType == typeof(bool) && value is string strBool && bool.TryParse(strBool, out var boolVal))
                    return boolVal;

                // 利用 Convert.ChangeType 进行基础类型转换
                // 注意：如果 value 是 string，ChangeType 会自动解析为 targetType
                // 如果 value 是其他类型（如前端反序列化出的 double），也会尝试转换
                return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
            catch
            {
                // 解析失败（例如字符串 "abc" 无法转为 int），降级返回原值
                // 领域层的校验服务会负责处理这种不一致并抛出业务异常
                return value;
            }
        }

        /// <summary>
        /// 将 object 列表根据目标类型全名转换为强类型 object 列表
        /// </summary>
        private static List<object?> ConvertToStrongTypeList(IEnumerable<object?>? values, string? typeName)
        {
            if (values == null) return new List<object?>();

            return values.Select(v => ConvertToStrongType(v, typeName)).ToList();
        }



        /// <summary>
        /// 辅助方法：将数据库中的 JSON 字符串反序列化为 ParamSchema 值对象
        /// </summary>
        private static ParamSchema DeserializeSchema(string schemaJson)
        {
            if (string.IsNullOrWhiteSpace(schemaJson))
                return default; // 视 ParamSchema.Create 逻辑而定，可能需要返回默认值或抛异常

            var schemaDto = JsonSerializer.Deserialize<SchemaDto>(schemaJson, 
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            return schemaDto.Adapt<ParamSchema>();
        }

        /// <summary>
        /// 辅助方法：将字符串安全解析为 EngineLayer 枚举
        /// </summary>
        private static EngineLayer ParseEngineLayer(string engineLayerStr)
        {
            return Enum.TryParse<EngineLayer>(engineLayerStr, out var layer)
                ? layer
                : EngineLayer.Standard;
        }
    }
}
