using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis.ValueObj;

namespace NX_lims_Softlines_Command_System.src.Application.Contract
{
    public interface IWordTemplateAdapter
    {
        (Dictionary<string, string> Values, HashSet<string> RedBookmarks) Adapt(AnalysisResult analysisResult);
    }
}
