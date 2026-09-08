namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.StandardCompositionContext
{
    public record CompositionResponseDto
    {
        /// <summary>
        /// 成分ID
        /// </summary>
        public int IdComposition { get; set; }

        /// <summary>
        /// 成分名称
        /// </summary>
        public string? CompositionNameEn { get; set; }

        /// <summary>
        /// 成分名称（中文）
        /// </summary>
        public string? CompositionNameChn { get; set; }

        /// <summary>
        /// 成分分类
        /// </summary>
        public string? PrimaryCategoryEn { get; set; }

        /// <summary>
        /// 成分分类（中文）
        /// </summary>
        public string? PrimaryCategoryChn { get; set; }

        /// <summary>
        /// 二级分类
        /// </summary>
        public string? SecondaryClassificationEn { get; set; }

        /// <summary>
        /// 二级分类（中文）
        /// </summary>
        public string? SecondaryClassificationChn { get; set; }

        /// <summary>
        /// 三级分类
        /// </summary>
        public string? TertiaryClassificationEn { get; set; }

        /// <summary>
        /// 三级分类（中文）
        /// </summary>
        public string? TertiaryClassificationChn { get; set; }
    }
}
