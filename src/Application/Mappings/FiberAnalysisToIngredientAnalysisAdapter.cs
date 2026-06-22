using Mapster;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis.ValueObj;
using System.Text.Json;

namespace NX_lims_Softlines_Command_System.src.Application.Mappings
{
    public class FiberAnalysisToIngredientAnalysisCalculationAdapter : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config.NewConfig<FiberAnalysis, IngredientAnalysisCalculation>()
                .ConstructUsing(src => ConvertToDomain(src));
        }

        private static IngredientAnalysisCalculation ConvertToDomain(FiberAnalysis src)
        {
            var methods = ParseMethods(src.Method);
            var type = (AnalysisType)(src.Type ?? 0);
            var components = DeserializeComponents(src.FiberAnalysis1, type);
            var remarkLabel = DeserializeRemark(src.Remark);

            return IngredientAnalysisCalculation.Create(
                src.Id,
                src.ReportNumber ?? string.Empty,
                src.Buyer ?? string.Empty,
                methods,
                type,
                components,
                remarkLabel
            );
        }

        private static RemarkLabel DeserializeRemark(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new RemarkLabel();

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                return new RemarkLabel
                {
                    RecommendedLabel = GetStringListProperty(root, "recommendedLabel"),
                    ResultRemark = GetStringProperty(root, "resultRemark"),
                    LabelRemark = GetStringProperty(root, "labelRemark"),
                    JudgmentLabelRemark = GetStringProperty(root, "judgmentLabelRemark"),
                    LanguageLabelRemark = GetStringProperty(root, "languageLabelRemark"),
                    DurabilityLabel = GetStringProperty(root, "durabilityLabel"),
                    OtherLabel = GetStringProperty(root, "otherLabel"),
                    Comprehensive = GetStringProperty(root, "comprehensive"),
                    VerifyResult = GetStringProperty(root, "verifyResult"),
                    FinalResult = GetStringProperty(root, "finalResult")
                };
            }
            catch (JsonException)
            {
                return new RemarkLabel();
            }
        }

        private static List<string> GetStringListProperty(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Array)
            {
                var list = new List<string>();
                foreach (var item in prop.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                        list.Add(item.GetString()!);
                }
                return list;
            }
            return new List<string>();
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

            // 读取 Sample（来自前端多组分 sampleInput 框）
            var multiSample = GetStringProperty(multipleProp, "sample");

            // 溶解行
            if (multipleProp.TryGetProperty("fiberDissolvedList", out var dissolvedListProp))
            {
                int step = 0;
                foreach (var list in dissolvedListProp.EnumerateArray())
                {
                    int globalStep = 0;
                    var originalGsm1 = GetFloatProperty(list, "originalGSMTrail1");
                    var originalGsm2 = GetFloatProperty(list, "originalGSMTrail2");

                    if (!list.TryGetProperty("dissolvedRows", out var rowsProp))
                        continue;
                    var units = new List<MultiDissolvedUnit>();
                    foreach (var row in rowsProp.EnumerateArray())
                    {
                        var unit = new MultiDissolvedUnit
                        {
                            FiberName = GetStringProperty(row, "fiberName"),
                            GSMTrail1 = GetFloatProperty(row, "gsmTrail1"),
                            GSMTrail2 = GetFloatProperty(row, "gsmTrail2"),
                            DissolutionStep = globalStep++
                        };
                        units.Add(unit);
                    }

                    // 将 units 作为一个 DissolvedFiberComponent 的值对象集合
                    var component = new DissolvedFiberComponent
                    {
                        FiberName = units.FirstOrDefault()?.FiberName ?? string.Empty,
                        DissolutionUnits = units,
                        OriginalGSMTrail1 = originalGsm1,
                        OriginalGSMTrail2 = originalGsm2,
                        Sample = multiSample
                    };

                    components.Add(component);
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
