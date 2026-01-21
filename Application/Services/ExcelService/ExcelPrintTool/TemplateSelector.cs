namespace NX_lims_Softlines_Command_System.Application.Services.ExcelService.ExcelPrintTool
{
    public class TemplateSelector
    {
        private readonly Dictionary<string, Dictionary<string[], string>> _templateSheetNames;
        private readonly Dictionary<string, string> _templateSheetNamesNormal;
        private readonly string _defaultSheetName;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="templateSheetNames"></param>
        /// <param name="templateSheetNamesNormal"></param>
        /// <param name="defaultSheetName"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public TemplateSelector(
            Dictionary<string, Dictionary<string[], string>> templateSheetNames,
            Dictionary<string, string> templateSheetNamesNormal,
            string defaultSheetName = "DefaultSheetName")
        {
            _templateSheetNames = templateSheetNames ?? throw new ArgumentNullException(nameof(templateSheetNames));
            _templateSheetNamesNormal = templateSheetNamesNormal ?? throw new ArgumentNullException(nameof(templateSheetNamesNormal));
            _defaultSheetName = defaultSheetName;
        }
        /// <summary>
        /// 用于获取字典中的模板名
        /// </summary>
        /// <param name="itemName"></param>
        /// <param name="sampleDescription"></param>
        /// <returns></returns>
        public string GetTemplateName(string itemName, string sampleDescription)
        {
            // 1) 模板 sheet
            if (_templateSheetNames.TryGetValue(itemName, out var subDictionary))
            {
                // 按条件数量从多到少排序，确保优先匹配多个条件的情况
                var sortedConditions = subDictionary.Keys
                    .OrderByDescending(arr => arr.Length)
                    .ThenBy(arr => arr.Length > 0 ? string.Join(",", arr) : "");

                foreach (var conditions in sortedConditions)
                {
                    // 检查所有条件是否都满足
                    if (conditions.All(condition =>
                        !string.IsNullOrEmpty(condition) &&
                        sampleDescription.Contains(condition, StringComparison.OrdinalIgnoreCase)))
                    {
                        return subDictionary[conditions];
                    }
                }
            }

            // 2) 如果在 TemplateSheetNames 中未找到，尝试从 TemplateSheetNamesNormal 中查找
            if (_templateSheetNamesNormal.TryGetValue(itemName, out var normalTemplateName))
            {
                return normalTemplateName;
            }

            // 3) 如果仍未找到匹配的模板名
            Console.WriteLine("未找到对应的模板名");
            return _defaultSheetName;
        }
    }
}
