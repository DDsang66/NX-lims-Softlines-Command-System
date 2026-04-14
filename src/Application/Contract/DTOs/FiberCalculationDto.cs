using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs
{
    /// <summary>
    /// 纤维计算请求
    /// </summary>
    public class FiberCalculationRequestDto
    {
        /// <summary>
        /// 测试标准：ISO, ASTM, GB, JIS
        /// </summary>
        public string Standard { get; set; } = "ISO";

        /// <summary>
        /// 纤维数据列表
        /// </summary>
        public List<FiberCalculationItemDto> Items { get; set; } = new();
    }

    public class FiberCalculationItemDto
    {
        /// <summary>
        /// 纤维名称
        /// </summary>
        public string Composition { get; set; } = null!;

        /// <summary>
        /// Trial #1 干重
        /// </summary>
        public decimal? Trial1 { get; set; }

        /// <summary>
        /// Trial #2 干重
        /// </summary>
        public decimal? Trial2 { get; set; }

        /// <summary>
        /// 表头 Trial #1（处理前干重）
        /// </summary>
        public decimal? HeaderTrial1 { get; set; }

        /// <summary>
        /// 表头 Trial #2（处理前干重）
        /// </summary>
        public decimal? HeaderTrial2 { get; set; }
    }

    /// <summary>
    /// 纤维计算结果
    /// </summary>
    public class FiberCalculationResultDto
    {
        /// <summary>
        /// 各纤维计算结果
        /// </summary>
        public List<FiberCalculationItemResultDto> Items { get; set; } = new();

        /// <summary>
        /// 推荐标签
        /// </summary>
        public string RecommendedLabel { get; set; } = string.Empty;

        /// <summary>
        /// 主要成分类型
        /// </summary>
        public string MainCategory { get; set; } = string.Empty; // Synthetic, Natural
    }

    public class FiberCalculationItemResultDto
    {
        public string Composition { get; set; } = null!;
        public decimal? Trial1 { get; set; }
        public decimal? Trial2 { get; set; }
        public decimal AvgDryWeight { get; set; }
        public decimal NetDryContent { get; set; }  // 净干含量 %
        public decimal MoistureRegain { get; set; } // 公定回潮率
        public decimal CombinedPercentage { get; set; }  // 结合公定回潮率 %
    }
}
