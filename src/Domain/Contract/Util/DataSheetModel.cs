namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Util
{
    public class DataSheetModel
    {
        // ★ 业务唯一键，用于生成 ModelKey
        public string ModelKey { get; set; } = "";

        // 实际数据
        public Dictionary<string, object> Data { get; set; } = new();

        /// <summary>
        /// 报告号
        /// </summary>
        public string ReportNumber { get; set; } = string.Empty;

        /// <summary>
        /// 测试方法
        /// </summary>
        public string TestMethod { get; set; } = string.Empty;

        /// <summary>
        /// 测试条件
        /// </summary>
        public string TestCondition { get; set; } = string.Empty;

        /// <summary>
        /// 当前项目model包需要的份数
        /// </summary>
        public int SheetCount { get; set; }

        /// <summary>
        /// 当前model包的data区域个数
        /// </summary>
        public int DataAreaCount { get; set; }

        /// <summary>
        /// 模板文件
        /// </summary>
        public string TemplateUrl { get; set; }

        /// <summary>
        /// 填写sample信息
        /// </summary>
        public SampleMap SampleMap { get; set; }

        /// <summary>
        /// 填写水洗次数信息
        /// </summary>
        public AfterWashMap AfterWashMap { get; set; }

        public DataSheetModel() { }
    }

    public class SampleMap 
    {
        public Dictionary<string, string> SampleMetaData { get; set; } = new Dictionary<string, string>();

        public bool IsAppend { get; set; }
    }

    public class AfterWashMap 
    {
        public Dictionary<string, string> AfterWashMetaData { get; set; } = new Dictionary<string, string>();

        public bool IsAppend { get; set; }
    }
}
