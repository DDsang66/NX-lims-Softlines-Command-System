namespace NX_lims_Softlines_Command_System.src.Application.Interface.FiberTeamContext
{
    public interface IWordTemplateEngine
    {
        void ReplaceText(string filePath, Dictionary<string, string> bookmarkValues, HashSet<string>? redBookmarks = null, HashSet<string>? removeWhenEmpty = null);
        void InsertMicroscopeImages(string filePath, IEnumerable<string> fiberNames, string imageFolder);
    }
}
