using DocumentFormat.OpenXml.InkML;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace NX_lims_Softlines_Command_System.Infrastructure.Providers.Mapper
{
    public class OvsParameterMapper
    {
        public static class OvsParameterMapperMethod
        {
            public static ICollection<ParamResponseDto> GetAllDtos()
  => _cache.Values;

            public static void ClearCache() => _cache.Clear();

            /* 缓存：key = (itemName, standard) */
            private static readonly ConcurrentDictionary<(string itemName, string standard), ParamResponseDto> _cache = new();

            /* 新映射签名 */
            private static readonly Dictionary<string, Action<WetParameterIso, JsonObject, ParamResponseDto, string>> Mappings
                = new();

            /* 静态构造函数：一次性填表 */
            static OvsParameterMapperMethod()
            {
                /* 温度+程序+钢球 */
                Mappings["Colour Fastness to Washing"] = (p, normalJson, dto, sample) =>
                {
                    var wet = BuildWetJson(p, "Temperature", "Program", "SteelBallNum");
                    AddSample(dto, sample, normalJson, wet);
                };

                /* 温度+配重+SCI+干法+洗程 */
                Mappings["Dimensional Stability to Washing"] = (p, normalJson, dto, sample) =>
                {
                    var wet = BuildWetJson(p, "Temperature", "Ballast", "SpecialCareInstruction", "DryProcedure", "WashingProcedure");
                    AddSample(dto, sample, normalJson, wet);
                };

                /* 敏感+param(旧最后一个字段) → param 进 WetParam */
                Mappings["Dimensional Stability to Dry-Cleaning"] = (p, normalJson, dto, sample) =>
                {
                    var wet = BuildWetJson(p, "Sensitive");
                    AddSample(dto, sample, normalJson, wet);  // param 已在 normalJson 里，但原逻辑 param 是最后一个字段，这里按原逻辑处理
                };

                /* 温度+配重+SCI+干法+洗程 */
                Mappings["Accelerated Ageing(Stroage) Test"] = (p, normalJson, dto, sample) =>
                {
                    var wet = BuildWetJson(p, "Temperature", "Ballast", "SpecialCareInstruction", "DryProcedure", "WashingProcedure");
                    AddSample(dto, sample, normalJson, wet);
                };

                /* 温度+配重+SCI+干法+洗程+param(旧最后一个字段) */
                Mappings["Moisture Management"] = (p, normalJson, dto, sample) =>
                {
                    var wet = BuildWetJson(p, "Temperature", "Ballast", "SpecialCareInstruction", "DryProcedure", "WashingProcedure");
                    AddSample(dto, sample, normalJson, wet);  // param 已在 normalJson 里
                };

                /* 同上 */
                Mappings["Pilling Resistance"] = (p, normalJson, dto, sample) =>
                {
                    var wet = BuildWetJson(p, "Temperature", "Ballast", "SpecialCareInstruction", "DryProcedure", "WashingProcedure");
                    AddSample(dto, sample, normalJson, wet);
                };

                /* 温度+配重+SCI+干法+洗程+param(旧最后一个字段) */
                Mappings["Bursting Strength"] = (p, normalJson, dto, sample) =>
                {
                    var wet = BuildWetJson(p, "Temperature", "Ballast", "SpecialCareInstruction", "DryProcedure", "WashingProcedure");
                    AddSample(dto, sample, normalJson, wet);
                };

                /* 同上 */
                Mappings["Seam Slippage"] = (p, normalJson, dto, sample) =>
                {
                    var wet = BuildWetJson(p, "Temperature", "Ballast", "SpecialCareInstruction", "DryProcedure", "WashingProcedure");
                    AddSample(dto, sample, normalJson, wet);
                };

                /* 同上 */
                Mappings["Vertical Wicking"] = (p, normalJson, dto, sample) =>
                {
                    var wet = BuildWetJson(p, "Temperature", "Ballast", "SpecialCareInstruction", "DryProcedure", "WashingProcedure");
                    AddSample(dto, sample, normalJson, wet);
                };

                /* 仅 param(旧最后一个字段) → 全部 null，只有 param */
                Mappings["Colour Fastness to Rubbing on Leather"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                /* 仅 param(旧最后一个字段) */
                Mappings["Colour Fastness to Light"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                /* 仅 param(旧最后一个字段) */
                Mappings["Colour Fastness to Chlorinated Water"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                /* 仅 param(旧最后一个字段) */
                Mappings["Appearance after Washing/Dry-Cleaning"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                /* 仅 param(旧最后一个字段) */
                Mappings["Calculation of Color Differences"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                /* 仅 param(旧最后一个字段) */
                Mappings["Movement after Washing"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                /* 仅 param(旧最后一个字段) */
                Mappings["Water Permeability/Hydrostatic Head"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                /* 仅 param(旧最后一个字段) */
                Mappings["Water Repellency"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                /* 仅 param(旧最后一个字段) */
                Mappings["Air Permeability"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                /* 仅 param(旧最后一个字段) */
                Mappings["Absorbency"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                /* 仅 param(旧最后一个字段) */
                Mappings["Abrasion Resistance"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                /* 仅 param(旧最后一个字段) - 注意：原文件中有重复定义 */
                Mappings["Bursting Strength"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                /* 仅 param(旧最后一个字段) */
                Mappings["Stretch & Recovery"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                /* 仅 param(旧最后一个字段) */
                Mappings["Tensile Strength"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                /* 仅 param(旧最后一个字段) */
                Mappings["Tear Strength"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                /* 仅 param(旧最后一个字段) - 注意：原文件中有重复定义 */
                Mappings["Bursting Strength"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                /* 仅 param(旧最后一个字段) */
                Mappings["Drying Rate"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);
            }



            /* 统一入口 */
            public static ParamResponseDto Map(string itemName,
                                           string standard,
                                           string sample,
                                           WetParameterIso p,
                                           NormalParameter param)
            {
                JsonObject? normalJson = JsonNode.Parse(param.ExtraParam ?? "{}")?.AsObject();
                var key = (itemName, standard);
                var dto = _cache.GetOrAdd(key, k =>
                    new ParamResponseDto(k.itemName, k.standard, new List<SampleParam>()));

                if (Mappings.TryGetValue(itemName, out var branch))
                    branch(p, normalJson!, dto, sample);
                else
                    DefaultMapping(normalJson!, dto, sample);

                return dto;
            }

            /* 兜底分支 */
            private static void DefaultMapping(JsonObject normalJson, ParamResponseDto dto, string sample)
            {
                AddSample(dto, sample, normalJson, null);
            }

            /* 线程安全追加 */
            private static void AddSample(ParamResponseDto dto, string sample, JsonObject normal, JsonObject? wet)
            {
                lock (dto.Param)
                {
                    dto.Param.Add(new SampleParam
                    {
                        Sample = sample,
                        NormalParam = normal,
                        WetParam = wet
                    });
                }
            }

            /* 根据参数名，返回对应的 JSON 对象 */
            private static JsonObject BuildWetJson(WetParameterIso p, params string[] keys)
            {
                var jo = new JsonObject();
                foreach (var k in keys)
                {
                    object? val = k switch
                    {
                        "Temperature" => p.Temperature,
                        "Program" => p.Program,
                        "SteelBall" => p.SteelBallNum,
                        "Ballast" => p.Ballast,
                        "SCI" => p.SpecialCareInstruction,
                        "DryProcedure" => p.DryProcedure,
                        "WashingProcedure" => p.WashingProcedure,
                        "Sensitive" => p.Sensitive,
                        "AfterWash" => p.AfterWash,
                        "Iron" => p.Iron,
                        _ => null
                    };
                    if (val != null)
                        jo[k] = JsonNode.Parse(JsonSerializer.Serialize(val));
                }
                return jo;
            }

        }
      
    }
}
