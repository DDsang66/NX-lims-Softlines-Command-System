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
                .ConstructUsing(src => ConvertToDomain(src))
                // ⚠️ ConstructUsing 之后 Mapster **仍会**按成员名做一遍常规赋值（用私有 setter / 后备字段），
                // 而下面三个名字与实体恰好同名 —— 不挡住的话它会把 Create() 定好的值再冲一遍。
                //
                // 其中 Id / Buyer 只是重复赋同一个值、无害；**Type 有害**：
                // Mapster 执行的是 `dest.Type = (AnalysisType)src.Type`，而 src.Type 为 NULL 时那是 Single(0)，
                // 正好把 ResolveAnalysisType 的 JSON 探针结果冲掉 —— 于是"列 NULL + JSON 是 multiple"
                // 会退化成 Type=Single 但成分表是多组分的自相矛盾状态。
                // 另外 src.Buyer 为 NULL 时 Mapster 会把 null 写进非空的 Buyer，Create 里的 `?? string.Empty` 也被冲掉。
                .Ignore(dest => dest.Id)
                .Ignore(dest => dest.Buyer)
                .Ignore(dest => dest.Type);
        }

        private static IngredientAnalysisCalculation ConvertToDomain(FiberAnalysis src)
        {
            var methods = ParseMethods(src.Method);
            var type = ResolveAnalysisType(src.Type, src.FiberAnalysis1);
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

        /// <summary>
        /// 判断分析类型。以列 fiber_analysis.type 为准；
        /// 列为 NULL 时按 JSON 里的 type 探针判断，而不是一律当作 <see cref="AnalysisType.Single"/>。
        /// </summary>
        /// <remarks>
        /// 原先写的是 (AnalysisType)(src.Type ?? 0)，NULL 落到 Single，
        /// 于是 JSON 里明明是 multiple 的记录会走 <see cref="DeserializeSingleComponents"/>
        /// 取到空成分表——聚合根的 Create 随后因"成分表为空"抛异常。
        /// 列有值时行为与改动前逐字一致（生产库实测 0 条 NULL）。
        /// </remarks>
        private static AnalysisType ResolveAnalysisType(byte? columnType, string? json)
        {
            if (columnType.HasValue)
                return (AnalysisType)columnType.Value;

            if (string.IsNullOrWhiteSpace(json))
                return AnalysisType.Single;

            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("type", out var typeProp)
                    && typeProp.ValueKind == JsonValueKind.String)
                {
                    var raw = typeProp.GetString();
                    if (string.Equals(raw, "Multiple", StringComparison.OrdinalIgnoreCase))
                        return AnalysisType.Multiple;
                    if (string.Equals(raw, "Single", StringComparison.OrdinalIgnoreCase))
                        return AnalysisType.Single;
                }
            }
            catch (JsonException)
            {
                // 脏 JSON：退回列缺省（Single），与改动前一致
            }

            return AnalysisType.Single;
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

            // `single` 是**字面量 null** 时（多条记录都长这样，见 SerializeBuildAnalysisToJson 的
            // `Single = hasSingle ? ... : null`），TryGetProperty 会抛 InvalidOperationException ——
            // 既不是 JsonException（下面的 catch 接不住），也不是领域校验异常，直接冒到 500。
            if (!root.TryGetProperty("single", out var singleProp)
                || singleProp.ValueKind != JsonValueKind.Object)
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

            // 同 DeserializeSingleComponents —— `multiple` 是字面量 null 时必须提前返回，
            // 否则下面每一处 TryGetProperty 都会抛 InvalidOperationException。
            if (!root.TryGetProperty("multiple", out var multipleProp)
                || multipleProp.ValueKind != JsonValueKind.Object)
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
                        var fiberName = GetStringProperty(row, "fiberName");

                        // 空行是表单里没填完的行，不能占 SplittingOrder ——
                        // 否则后面每个真实行的序号整体前移，溶解列的配对会跟着错位。
                        // 实测生产库 0 条记录含空行（拆分列/溶解列都是）。
                        if (string.IsNullOrWhiteSpace(fiberName))
                            continue;

                        components.Add(new SplittingFiberComponent
                        {
                            FiberName = fiberName,
                            GSMTrail1 = GetFloatProperty(row, "gsmTrail1"),
                            GSMTrail2 = GetFloatProperty(row, "gsmTrail2"),
                            SplittingOrder = order++,
                            CellulosicSubFibers = ParseCellulosicSubFibers(row),
                            BicomponentSubFibers = ParseBicomponentSubFibers(row)
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
                        var fiberName = GetStringProperty(row, "fiberName");

                        // 同拆分列，空行不占 DissolutionStep（否则组内后续行的步骤号整体前移）
                        if (string.IsNullOrWhiteSpace(fiberName))
                            continue;

                        var unit = new MultiDissolvedUnit
                        {
                            FiberName = fiberName,
                            GSMTrail1 = GetFloatProperty(row, "gsmTrail1"),
                            GSMTrail2 = GetFloatProperty(row, "gsmTrail2"),
                            DissolutionStep = globalStep++,
                            CellulosicSubFibers = ParseCellulosicSubFibers(row),
                            BicomponentSubFibers = ParseBicomponentSubFibers(row)
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
            // Trim。纤维名与 token 集合是精确匹配（`*cellulosic fibre` 等），
            // 首尾空白会让谓词全部落空；实测生产库 0 条含首尾空白。
            if (element.TryGetProperty(propertyName, out var prop)
                && prop.ValueKind == JsonValueKind.String)
                return (prop.GetString() ?? string.Empty).Trim();
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

        private static List<CellulosicSubFiber> ParseCellulosicSubFibers(JsonElement row)
        {
            var list = new List<CellulosicSubFiber>();
            if (!row.TryGetProperty("cellulosicSubFibers", out var arr)
                || arr.ValueKind != JsonValueKind.Array)
                return list;

            foreach (var item in arr.EnumerateArray())
            {
                var name = GetStringProperty(item, "fiberName");
                if (string.IsNullOrWhiteSpace(name)) continue;
                var pct = 0m;
                if (item.TryGetProperty("percentage", out var pctProp)
                    && pctProp.ValueKind == JsonValueKind.Number)
                    pct = pctProp.GetDecimal();
                list.Add(new CellulosicSubFiber { FiberName = name, Percentage = pct });
            }
            return list;
        }

        private static List<BicomponentSubFiber> ParseBicomponentSubFibers(JsonElement row)
        {
            var list = new List<BicomponentSubFiber>();
            if (!row.TryGetProperty("bicomponentSubFibers", out var arr)
                || arr.ValueKind != JsonValueKind.Array)
                return list;

            foreach (var item in arr.EnumerateArray())
            {
                var name = GetStringProperty(item, "fiberName");

                // 与 ParseCellulosicSubFibers 对齐——空白子纤维名不入表。
                // 空名会让 bicomponent 的组分拆分多出一个无名成分。
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                list.Add(new BicomponentSubFiber
                {
                    FiberName = name,
                    GSMTrail1 = (decimal)GetFloatProperty(item, "gsmTrail1"),
                    GSMTrail2 = (decimal)GetFloatProperty(item, "gsmTrail2")
                });
            }
            return list;
        }
    }
}
