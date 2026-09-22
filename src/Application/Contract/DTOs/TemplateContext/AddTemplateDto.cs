namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.TemplateContext
{
    public record AddTemplateDto
    {
        /// <summary>
        /// 模板名称
        /// </summary>
        public string TemplateName { get; set; } = string.Empty;

        /// <summary>
        /// 节点
        /// </summary>
        public string Site { get; set; } = string.Empty;

        /// <summary>
        /// 模板文件类型
        /// </summary>
        public string FileType { get; set; } = string.Empty;

        /// <summary>
        /// 模板类型
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// 测试类型
        /// </summary>
        public string? TestType { get; set; } = string.Empty;

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; } = string.Empty;

        /// <summary>
        /// 模板文件
        /// </summary>
        public IFormFile? TemplateFile { get; set; }

        /// <summary>
        /// 模板索引
        /// </summary>
        public string TemplateIndexJson { get; set; } = string.Empty;

        /// <summary>
        /// 测试条件文本模板
        /// </summary>
        public string TestConditionTextTemplateDtosJson { get; set; } = string.Empty;

        /// <summary>
        /// 模板结构
        /// </summary>
        public string TemplateSturctureDtoJson { get; set; } = string.Empty;
    }

    public record TemplateSturctureDto 
    {
        /// <summary>
        /// 测试Condition个数
        /// </summary>
        public int TestConditionCount { get; set; }

        /// <summary>
        /// method位置个数
        /// </summary>
        public int TestMethodCount { get; set; }

        /// <summary>
        /// 试样编号位置个数
        /// </summary>
        public int SampleDataAreaCount { get; set; }

        /// <summary>
        /// 试验结果位置个数
        /// </summary>
        public int SampleResultAreaCount { get; set; }

        /// <summary>
        /// 洗涤位置个数
        /// </summary>
        public int AfterWashDataCount { get; set; }
    }

    public record TestConditionTextTemplateDto 
    {
        /// <summary>
        /// 模板索引
        /// </summary>
        public Dictionary<string,object> TemplateIndex { get; set; } = new();

        /// <summary>
        /// 文本模板
        /// 例如： "Procedure No: {WashProcedure}; Using horizontal axis, front-loading type machie: Machine wash at {WashingTemperature} degree C with {WashLoad} kg total dry mass( {BallastType} + specimen) and {ReferenceDetergentsComposition}, {DryProcedure}, / Iron.";
        /// 目的是为了提供样本插入参数中的值，生成完整的测试条件
        /// </summary>
        public string Text { get; set; } = string.Empty;
    }
}
