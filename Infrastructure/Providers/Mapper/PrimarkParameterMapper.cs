using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using System.Text.Json;

namespace NX_lims_Softlines_Command_System.Infrastructure.Providers.Mapper
{
    public class PrimarkParameterMapper
    {
        public static class PrimarkParameterMapperMethod
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
            static PrimarkParameterMapperMethod()
            {
                /* 温度+程序+钢球+SCI */
                Mappings["Colour Fastness to Washing"] = (p, normalJson, dto, sample) =>
                {
                    var wet = BuildWetJson(p, "Temperature", "Program", "SteelBall", "SCI");
                    AddSample(dto, sample, normalJson, wet);
                };

                /* 温度+程序+配重+SCI+干法+洗程+后洗 */
                Mappings["Absorbency of Textiles"] = (p, normalJson, dto, sample) =>
                {
                    var wet = BuildWetJson(p, "Temperature", "Program", "Ballast", "SCI",
                                               "DryProcedure", "WashingProcedure", "AfterWash");
                    AddSample(dto, sample, normalJson, wet);
                };

                /* 温度+干法+后洗 */
                Mappings["Colour Fastness to Hot Pressing"] = (p, normalJson, dto, sample) =>
                {
                    var wet = BuildWetJson(p, "Temperature", "Iron");
                    AddSample(dto, sample, normalJson, wet);
                };

                /* 温度+程序+配重+SCI+干法+洗程+后洗 */
                Mappings["Dimensional and Bra Wire Casing Stability"] = (p, normalJson, dto, sample) =>
                {
                    var wet = BuildWetJson(p, "Temperature", "Program", "Ballast", "SCI",
                                               "DryProcedure", "WashingProcedure", "AfterWash");
                    AddSample(dto, sample, normalJson, wet);
                };

                /* 温度+程序+配重+SCI+干法+洗程+后洗 + param(旧最后一个字段) → 这里 param 进 NormalParam */
                Mappings["Martindale Pilling"] = (p, normalJson, dto, sample) =>
                {
                    var wet = BuildWetJson(p, "Temperature", "Program", "Ballast", "SCI",
                                               "DryProcedure", "WashingProcedure", "AfterWash");
                    AddSample(dto, sample, normalJson, wet);   // param 已在 normalJson 里
                };

                /* 温度+干法+后洗 */
                Mappings["Print / Motif / Flock Durability"] = (p, normalJson, dto, sample) =>
                {
                    var wet = BuildWetJson(p, "Temperature", "DryProcedure", "AfterWash");
                    AddSample(dto, sample, normalJson, wet);
                };

                /* 同上 */
                Mappings["Print Durability"] = (p, normalJson, dto, sample) =>
                {
                    var wet = BuildWetJson(p, "Temperature", "DryProcedure", "AfterWash");
                    AddSample(dto, sample, normalJson, wet);
                };

                /* 温度+程序+配重+SCI+干法+洗程+后洗 */
                Mappings["Shower Resistant Claims Spray Rating"] = (p, normalJson, dto, sample) =>
                {
                    var wet = BuildWetJson(p, "Temperature", "Program", "Ballast", "SCI",
                                               "DryProcedure", "WashingProcedure", "AfterWash");
                    AddSample(dto, sample, normalJson, wet);
                };

                /* 同上模板 */
                Mappings["Spirality"] = (p, normalJson, dto, sample) =>
                {
                    var wet = BuildWetJson(p, "Temperature", "Program", "Ballast", "SCI",
                                               "DryProcedure", "WashingProcedure", "AfterWash");
                    AddSample(dto, sample, normalJson, wet);
                };

                Mappings["Stability to Washing"] = (p, normalJson, dto, sample) =>
                {
                    var wet = BuildWetJson(p, "Temperature", "Program", "Ballast", "SCI",
                                               "DryProcedure", "WashingProcedure", "AfterWash");
                    AddSample(dto, sample, normalJson, wet);
                };

                Mappings["Waterproof Claims Hydrostatic Head"] = (p, normalJson, dto, sample) =>
                {
                    var wet = BuildWetJson(p, "Temperature", "Program", "Ballast", "SCI",
                                               "DryProcedure", "WashingProcedure", "AfterWash");
                    AddSample(dto, sample, normalJson, wet);
                };

                Mappings["Dimensional Stability"] = (p, normalJson, dto, sample) =>
                {
                    var wet = BuildWetJson(p, "Temperature", "Program", "Ballast", "SCI",
                                               "DryProcedure", "WashingProcedure", "AfterWash");
                    AddSample(dto, sample, normalJson, wet);
                };

                /* 仅敏感+后洗 */
                Mappings["Stability to Dry Cleaning"] = (p, normalJson, dto, sample) =>
                {
                    var wet = BuildWetJson(p, "Sensitive", "AfterWash");
                    AddSample(dto, sample, normalJson, wet);
                };

                /* 以下条目旧映射全部字段为 null，只有最后一个 param → 直接复用 normalJson */
                Mappings["Abrasion of Knitted Footwear Garments - Modified Martindale"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                Mappings["Accelerotor"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                Mappings["Bursting Strength"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                Mappings["Colour Fastness to Chlorinated Water"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                Mappings["Colour Fastness to Dry Cleaning"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                Mappings["Colour Fastness to Light"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                Mappings["Colour Fastness to Water"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                Mappings["Martindale Abrasion"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                Mappings["Nap Stability"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                Mappings["Residual Elongation"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                Mappings["Residual Elongation SHAPEWEAR"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                Mappings["Tear Strength"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                Mappings["Tensile Strength"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                Mappings["Seam Strength"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                Mappings["Seam Slippage"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                Mappings["Unrecovered Elongation"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                Mappings["Elastic Extension and Modulus Test"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                Mappings["Vertical Wicking of Textiles"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                Mappings["Back Pocket Application Strength"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                Mappings["Belt Loop Application Strength"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                Mappings["Colour Fastness to Non Chlorine Bleach"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                Mappings["Colour Fastness to Chlorine Bleach"] = (p, normalJson, dto, sample) =>
                    AddSample(dto, sample, normalJson, null);

                Mappings["Quick Dry"] = (p, normalJson, dto, sample) =>
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
        #region
        //private static readonly Dictionary<string, Func<WetParameterIso, string, ParamDto>> Mappings = new()
        //{
        //    ["Colour Fastness to Washing"] = (p, param) => new(p.ContactItem!, p.Standard, p.Temperature + "°C", p.Program, p.SteelBallNum, null, p.SpecialCareInstruction, null, null, null, null, null, param),
        //    ["Absorbency of Textiles"] = (p, param) => new(p.ContactItem!, p.Standard, p.Temperature + "°C", p.Program, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, p.AfterWash, null),
        //    ["Colour Fastness to Hot Pressing"] = (p, param) => new(p.ContactItem!, p.Standard, p.Temperature + "°C", null, null, null, null, null, null, null, null, null, p.Iron),
        //    ["Dimensional and Bra Wire Casing Stability"] = (p, param) => new(p.ContactItem!, p.Standard, p.Temperature + "°C", p.Program, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, p.AfterWash, null),
        //    ["Martindale Pilling"] = (p, param) => new(p.ContactItem!, p.Standard, p.Temperature + "°C", p.Program, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, p.AfterWash, param),
        //    ["Print / Motif / Flock Durability"] = (p, param) => new(p.ContactItem!, p.Standard, p.Temperature + "°C", null, null, null, null, p.DryProcedure, null, null, null, p.AfterWash, null),
        //    ["Print Durability"] = (p, param) => new(p.ContactItem!, p.Standard, p.Temperature + "°C", null, null, null, null, p.DryProcedure, null, null, null, p.AfterWash, null),
        //    ["Shower Resistant Claims Spray Rating"] = (p, param) => new(p.ContactItem!, p.Standard, p.Temperature + "°C", p.Program, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, p.AfterWash, null),
        //    ["Spirality"] = (p, param) => new(p.ContactItem!, p.Standard, p.Temperature + "°C", p.Program, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, p.AfterWash, null),
        //    ["Stability to Washing"] = (p, param) => new(p.ContactItem!, p.Standard, p.Temperature + "°C", p.Program, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, p.AfterWash, null),
        //    ["Waterproof Claims Hydrostatic Head"] = (p, param) => new(p.ContactItem!, p.Standard, p.Temperature + "°C", p.Program, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, p.AfterWash, null),
        //    ["Dimensional Stability"] = (p, param) => new(p.ContactItem!, p.Standard, p.Temperature + "°C", p.Program, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, p.AfterWash, null),
        //    ["Stability to Dry Cleaning"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, p.Sensitive, null, p.AfterWash, null),
        //    ["Abrasion of Knitted Footwear Garments - Modified Martindale"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
        //    ["Accelerotor"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
        //    ["Bursting Strength"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
        //    ["Colour Fastness to Chlorinated Water"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
        //    ["Colour Fastness to Dry Cleaning"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
        //    ["Colour Fastness to Light"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
        //    ["Colour Fastness to Water"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
        //    ["Martindale Abrasion"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
        //    ["Nap Stability"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
        //    ["Residual Elongation"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
        //    ["Residual Elongation SHAPEWEAR"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
        //    ["Tear Strength"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
        //    ["Tensile Strength"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
        //    ["Seam Strength"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
        //    ["Seam Slippage"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
        //    ["Unrecovered Elongation"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
        //    ["Elastic Extension and Modulus Test"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
        //    ["Vertical Wicking of Textiles"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
        //    ["Back Pocket Application Strength"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
        //    ["Belt Loop Application Strength"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
        //    ["Colour Fastness to Non Chlorine Bleach"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
        //    ["Colour Fastness to Chlorine Bleach"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
        //    ["Quick Dry"] = (p, param) => new(p.ContactItem!, p.Standard, null, null, null, null, null, null, null, null, null, null, param),
        //};
        #endregion

    }
}
