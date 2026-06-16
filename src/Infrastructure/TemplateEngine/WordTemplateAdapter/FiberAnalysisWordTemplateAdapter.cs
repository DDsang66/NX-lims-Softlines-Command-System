using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.TemplateEngine.WordTemplateAdapter
{
    public class FiberAnalysisWordTemplateAdapter:IScopedDependency
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
            flatData["RecommendedLabel"] = analysisResult.RecommendedLabelString;
            flatData["ResultRemark"] = analysisResult.ResultRemark;
            flatData["LabelRemark"] = analysisResult.LabelRemark;
            flatData["JudgmentLabelRemark"] = analysisResult.JudgmentLabelRemark;
            flatData["LanguageLabelRemark"] = analysisResult.LanguageLabelRemark;
            flatData["DurabilityLabel"] = analysisResult.DurabilityLabel;
            flatData["OtherLabel"] = analysisResult.OtherLabel;
            flatData["Comprehensive"] = analysisResult.Comprehensive;
            flatData["VerifyResult"] = analysisResult.VerifyResult;
            flatData["FinalResult"] = analysisResult.FinalResult;

            // 数组/嵌套结构：留空，后续单独处理
            // 数组展开：Results → TestResult_1, TestResult_2, ...
            var results = analysisResult.Results;
            for (int i = 0; i < results.Count; i++)
            {
                flatData[$"TestResult_{i + 1}"] = results[i];
            }

            // 数组展开：Recommendation → Recommendation_1, Recommendation_2, ...
            var recommendations = analysisResult.Recommendation;
            for (int i = 0; i < recommendations.Count; i++)
            {
                flatData[$"Recommendation_{i + 1}"] = recommendations[i];
            }
            // flatData["CalculatedFiberResult"] = ?;  // 复杂对象列表，需逐行展开
            var fiberData = ExpandCalculatedFiberResult(analysisResult.CalculatedFiberResult);
            foreach (var kv in fiberData)
            {
                flatData[kv.Key] = kv.Value;
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
        private Dictionary<string, string> ExpandCalculatedFiberResult(List<CalculatedFiberResult> calculatedFiberResult)
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
                        flatData["Sample"] = single.Sample;  // 页眉 Sample 书签（无后缀）
                        flatData[$"MoistureRegain_{itemIndex}"] = single.MoistureRegain.ToString("F2")+"%";
                        flatData[$"GSMTrail1_{itemIndex}"] = single.GSMTrail1.ToString("F4");
                        flatData[$"Rate_{itemIndex}"] = single.Rate.ToString("F2")+"%";
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
    }
}
