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
