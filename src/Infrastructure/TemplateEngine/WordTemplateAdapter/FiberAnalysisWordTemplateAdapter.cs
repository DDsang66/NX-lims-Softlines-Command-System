using NX_lims_Softlines_Command_System.src.Application.Contract;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.TemplateEngine.WordTemplateAdapter
{
    public class FiberAnalysisWordTemplateAdapter : IWordTemplateAdapter, IScopedDependency
    {
        /// <summary>
        /// 从 IngredientAnalysis 计算结果中重构 Data，
        /// 拍平为模板可直接使用的 Dictionary<string, string>
        /// 数组/嵌套结构留空，后续单独处理
        /// </summary>
        public Dictionary<string, string> Adapt(AnalysisResult analysisResult)
        {
            var flatData = new Dictionary<string, string>();
            var Data = new Dictionary<string, string>();

            // 基础字段（直接映射）
            flatData["ReportNumber"] = analysisResult.ReportNumber;
            flatData["Buyer"] = analysisResult.Buyer;
            flatData["CalculateTime"] = analysisResult.CalculateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty;
            flatData["Methods"] = analysisResult.Methods;
            flatData["ComponentType"] = analysisResult.ComponentType;

            // 标签/备注字段（直接映射）
            // 选空时清空 Recommend 单元格，选 Yes 时不填保持原样
            if (string.IsNullOrWhiteSpace(analysisResult.RecommendedLabelString))
                flatData["Recommend"] = "";

            flatData["ResultRemark"] = analysisResult.ResultRemark;
            flatData["LabelRemark"] = analysisResult.LabelRemark;
            flatData["JudgmentLabelRemark"] = analysisResult.JudgmentLabelRemark;
            flatData["LanguageLabelRemark"] = analysisResult.LanguageLabelRemark;
            flatData["DurabilityLabel"] = analysisResult.DurabilityLabel;
            flatData["OtherLabel"] = analysisResult.OtherLabel;
            flatData["Comprehensive"] = analysisResult.Comprehensive;
            flatData["VertifyResult"] = analysisResult.VerifyResult;  // 模板书签名为 VertifyResult
            flatData["FinalResult"] = analysisResult.FinalResult;
            flatData["BurningTest"] = analysisResult.BurningTest;

            // 数组/嵌套结构：留空，后续单独处理
            // 数组展开：Results → TestResult_1, TestResult_2, ...
            var results = analysisResult.Results;
            for (int i = 0; i < results.Count; i++)
            {
                flatData[$"TestResult_{i + 1}"] = results[i];
            }

            // 数组展开：Recommendation → Recommendation_1, Recommendation_2, ...
            // 当推荐标签为空时跳过填入
            if (!string.IsNullOrWhiteSpace(analysisResult.RecommendedLabelString))
            {
                var recommendations = analysisResult.Recommendation;
                for (int i = 0; i < recommendations.Count; i++)
                {
                    flatData[$"Recommendation_{i + 1}"] = recommendations[i];
                }
            }
            // flatData["CalculatedFiberResult"] = ?;  // 复杂对象列表，需逐行展开
            var fiberData = ExpandCalculatedFiberResult(
                analysisResult.CalculatedFiberResult);
            foreach (var kv in fiberData)
            {
                flatData[kv.Key] = kv.Value;
            }

            // ResultRemark 或 LabelRemark 非空时 Sample 后加 *
            if (!string.IsNullOrWhiteSpace(analysisResult.ResultRemark) ||
                !string.IsNullOrWhiteSpace(analysisResult.LabelRemark))
            {
                flatData["Sample"] = (flatData.GetValueOrDefault("Sample") ?? "") + "*";
            }

            // 设备选型字段 — 拼接所有非空设备值
            var equipmentParts = new[]
            {
                analysisResult.Equipment_Microscope,
                analysisResult.Equipment_Oven,
                analysisResult.Equipment_Balance,
                analysisResult.Equipment_WaterBath,
                analysisResult.Equipment_Shaker
            }.Where(e => !string.IsNullOrWhiteSpace(e));
            flatData["Equipment"] = string.Join("    ", equipmentParts);

            // 页脚 MR 汇总
            var mrItems = analysisResult.CalculatedFiberResult
                .OfType<MultiCalculatedFiberItem>()
                .SelectMany(m => m.MultiFiberRowUnits ?? new List<MultiFiberRowUnit>())
                .Where(r => !string.IsNullOrWhiteSpace(r.Sum) && !r.Sum.Contains('/'))
                .Where(r => r.MoistureRegain > 0)
                .Select(r => $"{r.Sum} {r.MoistureRegain:F2}%");
            flatData["MR"] = string.Join("  ", mrItems);

            // 计数字段
            flatData["ComponentsCount"] = analysisResult.ComponentsCount.ToString();

            // 保存到工作单
            foreach (var kv in flatData)
            {
                Data[kv.Key] = kv.Value;
            }

            return Data.ToDictionary(
                kv => kv.Key,
                kv => kv.Value?.ToString() ?? string.Empty); ;
        }

        /// <summary>
        /// 展开 CalculatedFiberResult 到扁平字典
        /// </summary>
        private Dictionary<string, string> ExpandCalculatedFiberResult(
            List<CalculatedFiberResult> calculatedFiberResult)
        {
            var flatData = new Dictionary<string, string>();

            int itemIndex = 1;

            foreach (var item in calculatedFiberResult)
            {
                switch (item)
                {
                    case SingleCalculatedFiberItem single:
                        flatData["Qualitative"] = single.Qualitative;
                        flatData["Reagent"] = single.Reagent;
                        flatData["Sample"] = "-";  // Single 组件 Sample 书签固定为 "-"
                        // 单组分用无后缀书签（模板兼容）
                        flatData["GSMTrail1"] = single.GSMTrail1.ToString("F4");
                        flatData["Rate"] = single.Rate.ToString("F2")+"%";
                        itemIndex++;
                        break;

                    case MultiCalculatedFiberItem multi:
                        flatData["Qualitative"] = multi.Qualitative;
                        flatData["Reagent"] = multi.Reagent;
                        flatData["Sample"] = multi.Sample;  // 页眉 Sample 书签
                        flatData["GSMTrail1"] = multi.GSMTrail1.ToString("F4");
                        flatData["GSMTrail2"] = multi.GSMTrail2.ToString("F4");
                        flatData["RateTrail1"] = multi.RateTrail1.ToString("F2")+"%";
                        flatData["RateTrail2"] = multi.RateTrail2.ToString("F2")+"%";
                        flatData["Rate"] = multi.Rate.ToString("F2")+"%";
                        flatData["Avg"] = multi.Avg.ToString("F2") + "%";

                        // 展开 MultiFiberRowUnits
                        if (multi.MultiFiberRowUnits != null)
                        {
                            var rowData = ExpandMultiFiberRowUnits(multi.MultiFiberRowUnits);
                            foreach (var kv in rowData)
                            {
                                flatData[kv.Key] = kv.Value;
                            }

                            // Bottle / Crucible 编号
                            // Bottle = 唯一 Yarn 名
                            var yarnNames = multi.MultiFiberRowUnits
                                .Select(u => u.Section)
                                .Where(s => !string.IsNullOrWhiteSpace(s) && s != "/")
                                .Distinct()
                                .ToList();

                            // Crucible = Section 变化时开新组，每组组分数-1
                            var crucibleCounts = new List<int>();
                            int groupCount = 0;
                            string lastSection = "";
                            foreach (var unit in multi.MultiFiberRowUnits)
                            {
                                var section = unit.Section ?? "";
                                if (!string.IsNullOrWhiteSpace(section) && section != "/" && section != lastSection)
                                {
                                    if (groupCount > 1)
                                        crucibleCounts.Add(groupCount - 1);
                                    groupCount = 0;
                                    lastSection = section;
                                }
                                if (!string.IsNullOrWhiteSpace(unit.Sum) && !unit.Sum.Contains('/'))
                                    groupCount++;
                            }
                            if (groupCount > 1)
                                crucibleCounts.Add(groupCount - 1);

                            int totalNeeded = yarnNames.Count + crucibleCounts.Sum();
                            if (totalNeeded > 0)
                            {
                                var rng = new Random();
                                var numbers = Enumerable.Range(1, 99)
                                    .OrderBy(_ => rng.Next())
                                    .Take(totalNeeded)
                                    .ToList();
                                var bottleTexts = numbers.Take(yarnNames.Count)
                                    .Select(n => $"Bottle: {n}");
                                var crucibleTexts = numbers.Skip(yarnNames.Count)
                                    .Select(n => $"Crucible: {n}");
                                flatData["Bottle"] = string.Join("    ",
                                    bottleTexts.Concat(crucibleTexts));
                            }

                            // Weighing Bottle 表
                            var weighingData = ExpandWeighingBottleData(multi.MultiFiberRowUnits);
                            foreach (var kv in weighingData)
                                flatData[kv.Key] = kv.Value;
                        }
                        break;
                }
            }

            return flatData;
        }


        /// <summary>
        /// 展开 MultiFiberRowUnits 为带索引的扁平字典
        /// 
        /// 规则：
        /// 1. 相同的 Section（Yarn #x）只出现一次，后续同组行 Section 为空
        /// 2. 下标按实际行数连续编号
        /// 3. 空 Section 不占位，但其他字段正常编号
        /// 
        /// 示例：
        /// 输入: [Yarn#1, Yarn#2, Yarn#3, Yarn#4, Yarn#4, Yarn#4, Yarn#5, Yarn#5]
        /// 输出: Section_1=Yarn#1, Section_2=Yarn#2, Section_3=Yarn#3, Section_4=Yarn#4, 
        ///       Section_5=, Section_6=, Section_7=Yarn#5, Section_8=
        ///       Sum_1=..., Sum_2=..., ... Sum_8=...
        /// </summary>
        private Dictionary<string, string> ExpandMultiFiberRowUnits(List<MultiFiberRowUnit> units)
        {
            var result = new Dictionary<string, string>();
            int rowIndex = 1;

            // 记录上一个 Section，用于判断是否需要显示
            string lastSection = string.Empty;

            foreach (var unit in units)
            {
                // Section 处理：与上一个不同则显示，相同则为空
                string sectionValue;
                if (unit.Section != lastSection)
                {
                    sectionValue = unit.Section;
                    lastSection = unit.Section;
                }
                else
                {
                    sectionValue = string.Empty;
                }

                // 写入当前行的所有字段
                result[$"Section_{rowIndex}"] = sectionValue;
                result[$"Sum_{rowIndex}"] = unit.Sum;
                result[$"GSMTrail1_{rowIndex}"] = unit.GSMTrail1 == 0 ? "" : unit.GSMTrail1.ToString("F4");
                result[$"GSMTrail2_{rowIndex}"] = unit.GSMTrail2 == 0 ? "" : unit.GSMTrail2.ToString("F4");
                result[$"RateTrail1_{rowIndex}"] = unit.RateTrail1 == 0 ? "" : unit.RateTrail1.ToString("F2") + "%";
                result[$"RateTrail2_{rowIndex}"] = unit.RateTrail2 == 0 ? "" : unit.RateTrail2.ToString("F2") + "%";
                result[$"Avg_{rowIndex}"] = unit.Avg == 0 ? "" : unit.Avg.ToString("F2") + "%";
                result[$"Correct_{rowIndex}"] = unit.Correct == 0 ? "" : unit.Correct.ToString("F2");
                result[$"MoistureRegain_{rowIndex}"] = unit.MoistureRegain == 0 ? "" : unit.MoistureRegain.ToString("F2") + "%";
                result[$"Rate_{rowIndex}"] = unit.Rate == 0 ? "" : unit.Rate.ToString("F2") + "%";

                rowIndex++;
            }

            return result;
        }

        /// <summary>
        /// 展开 Weighing Bottle 表数据。
        /// 规则：一个溶解组（相同 Section）共用一个称量瓶，A/B 两平行试验独立随机。
        /// WeighingA/WeighingB 只在 Section 首行填值，TotalA/TotalB 每行都填。
        /// </summary>
        private static Dictionary<string, string> ExpandWeighingBottleData(List<MultiFiberRowUnit> units)
        {
            var result = new Dictionary<string, string>();
            var rng = new Random();
            string lastSection = "";
            decimal weighingA = 0m;
            decimal weighingB = 0m;
            string lastDescription = "";

            for (int i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                var row = i + 1;

                // Section 变 → 新称量瓶
                if (unit.Section != lastSection || string.IsNullOrWhiteSpace(unit.Section))
                {
                    if (!string.IsNullOrWhiteSpace(unit.Section) && unit.Section != "/")
                    {
                        weighingA = (260000m + rng.Next(0, 90001)) / 10000m;
                        weighingB = (260000m + rng.Next(0, 90001)) / 10000m;
                        lastSection = unit.Section;
                        result[$"WeighingA{row}"] = weighingA.ToString("F4");
                        result[$"WeighingB{row}"] = weighingB.ToString("F4");
                    }
                }
                else
                {
                    // 同 Section 后续行留空
                    result[$"WeighingA{row}"] = "";
                    result[$"WeighingB{row}"] = "";
                }

                // Description — Section 去重逻辑（同 ExpandMultiFiberRowUnits）
                if (!string.IsNullOrWhiteSpace(unit.Section) && unit.Section != lastDescription)
                {
                    result[$"Description{row}"] = unit.Section;
                    lastDescription = unit.Section;
                }
                else
                {
                    result[$"Description{row}"] = "";
                }

                // Component
                result[$"Component{row}"] = unit.Sum;

                // sampleA / sampleB — GSMTrail 为 0 则留空
                result[$"sampleA{row}"] = unit.GSMTrail1 == 0 ? "" : unit.GSMTrail1.ToString("F4");
                result[$"sampleB{row}"] = unit.GSMTrail2 == 0 ? "" : unit.GSMTrail2.ToString("F4");

                // TotalA / TotalB — 每行都填（用组瓶重 + 当前行 GSMTrail）
                result[$"TotalA{row}"] = (weighingA + unit.GSMTrail1).ToString("F4");
                result[$"TotalB{row}"] = (weighingB + unit.GSMTrail2).ToString("F4");
            }

            return result;
        }
    }
}
