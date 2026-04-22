using Mapster;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.IngredientAnalysis;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.IngredientAnalysis.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.IngredientAnalysis.ValueObj;
using System.Text.Json;

namespace NX_lims_Softlines_Command_System.src.Application.Mappings
{
    public class FiberAnalysisToIngredientAnalysisAdapter : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<FiberAnalysis, IngredientAnalysis>()
                .ConstructUsing(src => ConvertToDomain(src));
        }

        private static IngredientAnalysis ConvertToDomain(FiberAnalysis src)
        {
            var methods = ParseMethods(src.Method);
            var type = (AnalysisType)(src.Type ?? 0);
            var components = DeserializeComponents(src.FiberAnalysis1, type);

            return IngredientAnalysis.Create(
                src.Id,
                src.ReportNumber ?? string.Empty,
                src.Buyer ?? string.Empty,
                methods,
                type,
                components
            );
        }

        private static List<string> ParseMethods(string? method)
        {
            if (string.IsNullOrWhiteSpace(method))
                return new List<string>();

            return method
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(m => m.Trim())
                .Where(m => !string.IsNullOrEmpty(m))
                .ToList();
        }

        private static List<FiberComponent> DeserializeComponents(string? json, AnalysisType type)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new List<FiberComponent>();

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                return type switch
                {
                    AnalysisType.Single => DeserializeSingleComponents(root),
                    AnalysisType.Multiple => DeserializeMultipleComponents(root),
                    _ => new List<FiberComponent>()
                };
            }
            catch (JsonException)
            {
                return new List<FiberComponent>();
            }
        }

        private static List<FiberComponent> DeserializeSingleComponents(JsonElement root)
        {
            var components = new List<FiberComponent>();

            if (!root.TryGetProperty("single", out var singleProp))
                return components;

            if (!singleProp.TryGetProperty("singleFiberRows", out var rowsProp))
                return components;

            foreach (var row in rowsProp.EnumerateArray())
            {
                components.Add(new SingleFiberComponent
                {
                    Sample = GetStringProperty(row, "sample"),
                    FiberName = GetStringProperty(row, "fiberName"),
                    GSMTrail1 = GetFloatProperty(row, "gsmTrail1")
                });
            }

            return components;
        }

        private static List<FiberComponent> DeserializeMultipleComponents(JsonElement root)
        {
            var components = new List<FiberComponent>();

            if (!root.TryGetProperty("multiple", out var multipleProp))
                return components;

            // 拆分行
            if (multipleProp.TryGetProperty("fiberSplittingList", out var splittingListProp))
            {
                int order = 0;
                foreach (var list in splittingListProp.EnumerateArray())
                {
                    if (!list.TryGetProperty("splittingRows", out var rowsProp))
                        continue;

                    foreach (var row in rowsProp.EnumerateArray())
                    {
                        components.Add(new SplittingFiberComponent
                        {
                            FiberName = GetStringProperty(row, "fiberName"),
                            GSMTrail1 = GetFloatProperty(row, "gsmTrail1"),
                            GSMTrail2 = GetFloatProperty(row, "gsmTrail2"),
                            SplittingOrder = order++
                        });
                    }
                }
            }

            // 溶解行
            if (multipleProp.TryGetProperty("fiberDissolvedList", out var dissolvedListProp))
            {
                int step = 0;
                foreach (var list in dissolvedListProp.EnumerateArray())
                {
                    var originalGsm1 = GetFloatProperty(list, "originalGSMTrail1");
                    var originalGsm2 = GetFloatProperty(list, "originalGSMTrail2");

                    if (!list.TryGetProperty("dissolvedRows", out var rowsProp))
                        continue;

                    foreach (var row in rowsProp.EnumerateArray())
                    {
                        components.Add(new DissolvedFiberComponent
                        {
                            OriginalGSMTrail1 = originalGsm1,
                            OriginalGSMTrail2 = originalGsm2,
                            FiberName = GetStringProperty(row, "fiberName"),
                            GSMTrail1 = GetFloatProperty(row, "gsmTrail1"),
                            GSMTrail2 = GetFloatProperty(row, "gsmTrail2"),
                            DissolutionStep = step++
                        });
                    }
                }
            }

            return components;
        }

        private static string GetStringProperty(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop))
                return prop.GetString() ?? string.Empty;
            return string.Empty;
        }

        private static float GetFloatProperty(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number)
                    return prop.GetSingle();

                if (prop.ValueKind == JsonValueKind.String &&
                    float.TryParse(prop.GetString(), out var result))
                    return result;
            }
            return 0f;
        }
    }
}
