namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.StandardCompositionContext
{
    public record CompositionCalculateDto
    {
        /// <summary>
        /// 成分名称
        /// </summary>
        public string? CompositionNameEn { get; set; }

        /// <summary>
        /// 成分含量
        /// </summary>
        public float Rate { get; set; }

        /// <summary>
        /// 成分ID
        /// </summary>
        public int IdComposition { get; set; }

        /// <summary>
        /// 成分分类
        /// </summary>
        public string? PrimaryCategoryEn { get; set; }

        /// <summary>
        /// 二级分类
        /// </summary>
        public string? SecondaryClassificationEn { get; set; }

        /// <summary>
        /// 三级分类
        /// </summary>
        public string? TertiaryClassificationEn { get; set; }
    }
}
