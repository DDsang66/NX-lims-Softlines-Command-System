using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis.ValueObj;

namespace NX_lims_Softlines_Command_System.src.Application.Interface.FiberTeamContext
{
    public interface IWordTemplateAdapter
    {
        /// <summary>
        /// 把分析结果摊平成书签字典。
        /// <c>RemoveWhenEmpty</c> 与 <c>RemoveBlockWhenEmpty</c> 是两种删法：
        /// 前者删书签**前的文字**、所在表保留；后者把书签所在的**整张表**删掉。
        /// </summary>
        (Dictionary<string, string> Values, HashSet<string> RedBookmarks, HashSet<string> RemoveWhenEmpty, HashSet<string> RemoveBlockWhenEmpty) Adapt(AnalysisResult analysisResult);
    }
}
