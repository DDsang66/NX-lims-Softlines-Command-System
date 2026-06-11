using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Service
{
    /// <summary>
    /// 纤维计算服务
    /// </summary>
    public class FiberCalculationService : IScopedDependency
    {
        private readonly IFiberDatabaseRepository _fiberRepo;

        // 合成纤维列表
        private static readonly HashSet<string> SyntheticFibers = new(StringComparer.OrdinalIgnoreCase)
        {
            "Acetate", "Polyester", "Polyamide", "Polyurethane", "Polyethylene",
            "Elastane", "Spandex", "Viscose", "Acrylic", "Modal", "Tencel",
            "Meraklon", "Lycra", "Lyocell", "Modacrylic", "Nylon", "Rayon", "Vinylon",
            "聚酯纤维", "锦纶", "尼龙", "氨纶", "腈纶", "粘胶纤维", "莫代尔", "莱赛尔", "醋酯纤维", "丙纶"
        };

        // 天然纤维列表
        private static readonly HashSet<string> NaturalFibers = new(StringComparer.OrdinalIgnoreCase)
        {
            "Cotton", "Wool", "Silk", "Ramie", "Mohair", "Tussah", "Linen", "Asbestos",
            "棉", "羊毛", "桑蚕丝", "苎麻", "亚麻"
        };

        public FiberCalculationService(IFiberDatabaseRepository fiberRepo)
        {
            _fiberRepo = fiberRepo;
        }

        /// <summary>
        /// 计算纤维成分结果
        /// </summary>
        public async Task<FiberCalculationResultDto> CalculateAsync(FiberCalculationRequestDto request)
        {
            var result = new FiberCalculationResultDto();
            var fiberData = await _fiberRepo.GetAllAsync();
            var fiberDict = fiberData
                .GroupBy(f => f.FiberNameEn.ToLower())
                .ToDictionary(g => g.Key, g => g.First());

            // 计算每个纤维的结果
            var itemResults = new List<FiberCalculationItemResultDto>();
            decimal totalDryWeight = 0;

            foreach (var item in request.Items)
            {
                if (string.IsNullOrWhiteSpace(item.Composition)) continue;

                // 计算平均干重
                decimal avgDryWeight = 0;
                int trialCount = 0;

                if (item.Trial1.HasValue)
                {
                    avgDryWeight += item.Trial1.Value;
                    trialCount++;
                }
                if (item.Trial2.HasValue)
                {
                    avgDryWeight += item.Trial2.Value;
                    trialCount++;
                }

                if (trialCount > 0)
                {
                    avgDryWeight /= trialCount;
                }

                totalDryWeight += avgDryWeight;

                // 获取回潮率
                decimal moistureRegain = GetMoistureRegain(item.Composition, request.Standard, fiberDict);

                itemResults.Add(new FiberCalculationItemResultDto
                {
                    Composition = item.Composition,
                    Trial1 = item.Trial1,
                    Trial2 = item.Trial2,
                    AvgDryWeight = avgDryWeight,
                    MoistureRegain = moistureRegain
                });
            }

            // 计算百分比
            if (totalDryWeight > 0)
            {
                decimal totalCombined = 0;

                foreach (var itemResult in itemResults)
                {
                    // 净干含量 = 该纤维干重 / 总干重 * 100
                    itemResult.NetDryContent = Math.Round(itemResult.AvgDryWeight / totalDryWeight * 100, 2);

                    // 结合公定回潮率 = 净干含量 * (100 + 回潮率) / 100
                    itemResult.CombinedPercentage = Math.Round(
                        itemResult.NetDryContent * (100 + itemResult.MoistureRegain) / 100, 2);

                    totalCombined += itemResult.CombinedPercentage;
                }

                // 归一化处理，确保总和为100%
                if (totalCombined > 0)
                {
                    foreach (var itemResult in itemResults)
                    {
                        itemResult.CombinedPercentage = Math.Round(
                            itemResult.CombinedPercentage / totalCombined * 100, 1);
                    }
                }
            }

            result.Items = itemResults;

            // 生成推荐标签
            result.RecommendedLabel = GenerateRecommendedLabel(itemResults);

            // 计算主要成分类型
            result.MainCategory = CalculateMainCategory(itemResults);

            return result;
        }

        /// <summary>
        /// 获取指定标准的回潮率
        /// </summary>
        private decimal GetMoistureRegain(string composition, string standard, Dictionary<string, FiberDatabase> fiberDict)
        {
            var key = composition.ToLower();
            if (!fiberDict.TryGetValue(key, out var fiber))
            {
                // 默认回潮率
                return 0;
            }

            return standard.ToUpper() switch
            {
                "ISO" or "ISO/EN" => fiber.MoistureRegainIso ?? 0,
                "AATCC" or "ASTM" => fiber.MoistureRegainAatcc ?? fiber.MoistureRegainIso ?? 0,
                "CAN" or "CAN/CGSB" => fiber.MoistureRegainCan ?? fiber.MoistureRegainIso ?? 0,
                "KOR" => fiber.MoistureRegainKor ?? fiber.MoistureRegainIso ?? 0,
                "GB" => fiber.MoistureRegainGb ?? fiber.MoistureRegainIso ?? 0,
                "CNS" => fiber.MoistureRegainCns ?? fiber.MoistureRegainIso ?? 0,
                "JIS" => fiber.MoistureRegainJis ?? fiber.MoistureRegainIso ?? 0,
                _ => fiber.MoistureRegainIso ?? 0
            };
        }

        /// <summary>
        /// 生成推荐标签
        /// </summary>
        private string GenerateRecommendedLabel(List<FiberCalculationItemResultDto> items)
        {
            if (items.Count == 0) return string.Empty;

            // 按含量降序排序
            var sortedItems = items
                .Where(i => i.CombinedPercentage > 0)
                .OrderByDescending(i => i.CombinedPercentage)
                .ToList();

            if (sortedItems.Count == 0) return string.Empty;

            var parts = new List<string>();
            foreach (var item in sortedItems)
            {
                // 取整处理
                var percent = Math.Round(item.CombinedPercentage);
                if (percent > 0)
                {
                    parts.Add($"{percent}% {item.Composition}");
                }
            }

            return string.Join(", ", parts);
        }

        /// <summary>
        /// 计算主要成分类型
        /// </summary>
        private string CalculateMainCategory(List<FiberCalculationItemResultDto> items)
        {
            decimal syntheticTotal = 0;
            decimal naturalTotal = 0;
            decimal total = 0;

            foreach (var item in items)
            {
                total += item.CombinedPercentage;

                if (SyntheticFibers.Contains(item.Composition))
                {
                    syntheticTotal += item.CombinedPercentage;
                }
                else if (NaturalFibers.Contains(item.Composition))
                {
                    naturalTotal += item.CombinedPercentage;
                }
            }

            if (total == 0) return string.Empty;

            var syntheticPercent = syntheticTotal / total * 100;
            var naturalPercent = naturalTotal / total * 100;

            return syntheticPercent > 50 ? "Synthetic" : naturalPercent > 50 ? "Natural" : "";
        }
    }
}
