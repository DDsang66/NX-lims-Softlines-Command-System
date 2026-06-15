using NX_lims_Softlines_Command_System.Domain.Shared.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.AnalysisWorksheet
{
    public sealed class AnalysisWorksheet:IAggregateRoot
    {
        /// <summary>
        /// 分析工作单聚合根，用户进入界面随即向后端创建一个工作单用于记录分析过程，
        /// 后续所有的操作经过该聚合根
        /// </summary>


        public Guid Id { get; private set; }
        public string WorksheetNo { get; private set; } = string.Empty;  // 工作单号

        // ==================== 状态 ====================
        //public WorksheetStatus Status { get; private set; }

        // ==================== 基础信息（用户输入） ====================
        public string ReportNo { get; private set; } = string.Empty;     // 报告号
        public string Buyer { get; private set; } = string.Empty;        // 买方/客户
        public List<string> Methods { get; private set; } = new();       // 检测方法
        public AnalysisType Type { get; private set; }                   // 单/多组分
        public Dictionary<string, object> Data { get; init; } = new();
        public AnalysisResult CalculationResult { get; private set; } = new();

        public static AnalysisWorksheet Create() 
        {
            return new AnalysisWorksheet();
        }

        /// <summary>
        /// 同步结果
        /// </summary>
        /// <param name="result"></param>
        public void AttachCalculationResult(AnalysisResult result)
        {
            CalculationResult = result;
            //Status = WorksheetStatus.Calculated;
        }

        // 生成Word是基础设施调用，不是领域逻辑
        public void MarkWordGenerated(string filePath)
        {
            //WordFilePath = filePath;
            //Status = WorksheetStatus.Completed;
            //CompletedAt = DateTime.UtcNow;
        }

    }
}
