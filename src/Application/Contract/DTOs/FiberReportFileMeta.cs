using System;

namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs
{
    /// <summary>
    /// 纤维报告文件元信息（GET reports 历史报告列表的一项）。
    /// 报告以 DOCX 文件存服务器 wwwroot/DocxModel/SaveDocx/FiberAnalysis{yyyyMM}/，
    /// 文件名 = {报告号}_{yyMMddHHmmss}_FiberAnalysis.docx，字段从文件名 + 文件属性解析。
    /// </summary>
    public class FiberReportFileMeta
    {
        /// <summary>文件名（含 .docx 扩展），也是下载接口的标识</summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>报告号（文件名去掉末尾"时间戳"那一段；报告号自身含下划线时原样保留）</summary>
        public string ReportNumber { get; set; } = string.Empty;

        /// <summary>文件大小（字节）</summary>
        public long SizeBytes { get; set; }

        /// <summary>生成时间（文件名时间戳解析，解析失败回退文件修改时间）</summary>
        public DateTime GeneratedAt { get; set; }
    }
}
