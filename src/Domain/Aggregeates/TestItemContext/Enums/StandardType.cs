namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TestItemContext.Enums
{
    public enum StandardType
    {
        /// <summary>
        /// 国际标准化组织 (International Organization for Standardization)
        /// 如：ISO 6330, ISO 105, ISO 1833, ISO 3071, ISO 3801
        /// </summary>
        ISO,

        /// <summary>
        /// 美国纺织化学家和染色家协会 (American Association of Textile Chemists and Colorists)
        /// 如：AATCC 61, AATCC 8, AATCC 124
        /// </summary>
        AATCC,

        /// <summary>
        /// 欧洲标准化委员会 (European Committee for Standardization)
        /// 如：EN ISO 105, EN 343
        /// </summary>
        EN,

        /// <summary>
        /// 英国标准协会 (British Standards Institution)
        /// 如：BS EN ISO 105, BS 3424
        /// </summary>
        BS,

        /// <summary>
        /// 德国标准化学会 (Deutsches Institut für Normung)
        /// 如：DIN 53866, DIN 54231
        /// </summary>
        DIN,

        /// <summary>
        /// 日本工业标准 (Japanese Industrial Standards)
        /// 如：JIS L 0844, JIS L 1902
        /// </summary>
        JIS,

        /// <summary>
        /// 中国国家标准 (Guobiao / National Standard)
        /// 如：GB/T 3921, GB/T 3922, GB 18401
        /// </summary>
        GB,

        /// <summary>
        /// 中国纺织行业标准 (Fangzhi / Textile Industry Standard)
        /// 如：FZ/T 01057, FZ/T 73020
        /// </summary>
        FZ,

        /// <summary>
        /// 美国材料与试验协会 (American Society for Testing and Materials)
        /// 如：ASTM D1230, ASTM D5034
        /// </summary>
        ASTM,

        /// <summary>
        /// 国际羊毛纺织品组织 (International Wool Textile Organisation)
        /// 如：IWTO-12, IWTO-28
        /// </summary>
        IWTO,

        /// <summary>
        /// 美国消费品安全委员会 (Consumer Product Safety Commission)
        /// 法规标准，如：CPSC 16 CFR 1610
        /// </summary>
        CPSC,

        /// <summary>
        /// 欧盟法规/指令 (European Regulation/Directive)
        /// 如：REACH, OEKO-TEX Standard 100
        /// </summary>
        EU,

        /// <summary>
        /// 其他/自定义标准
        /// </summary>
        Other
    }
}
