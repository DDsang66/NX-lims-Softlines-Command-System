using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

public static class SheetSorter
{
    /// <summary>
    /// 排序工作表并修正 Named Range 的 localSheetId，保证名称与重新排序后的表对应。
    /// param filePath: Excel 文件路径
    /// </summary>
    public static void SortSheetsAndFixNames(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        using var doc = SpreadsheetDocument.Open(stream, true);

        var workbook = doc.WorkbookPart!.Workbook;
        var sheets = workbook.Sheets;
        var definedNames = workbook.DefinedNames;

        /* 1. 取出原顺序的 sheet 信息 */
        var sheetList = sheets!.Elements<Sheet>().ToList();
        var nameMap = sheetList.Select((s, idx) => new { Sheet = s, OldIndex = idx }).ToList();

        /* 2. 按名称排序 */
        var ordered = nameMap.OrderBy(x => x.Sheet.Name!.Value, StringComparer.OrdinalIgnoreCase).ToList();

        /* 3. 建立 旧索引→新索引 映射 */
        var indexMap = new Dictionary<int, int>();
        for (int i = 0; i < ordered.Count; i++)
            indexMap[ordered[i].OldIndex] = i;   // key=原顺序号，value=新顺序号

        /* 4. 重排 <sheet> 节点 */
        foreach (var elem in sheetList) elem.Remove();
        foreach (var elem in ordered) sheets.Append(elem.Sheet);

        /* 5. 修正 Named Range 的 localSheetId */
        if (definedNames != null)
        {
            foreach (var dn in definedNames.Elements<DefinedName>())
            {
                if (dn.LocalSheetId?.HasValue == true)
                {
                    int oldId = (int)dn.LocalSheetId.Value;
                    if (indexMap.TryGetValue(oldId, out int newId))
                        dn.LocalSheetId.Value = (uint)newId;
                    // 如果 map 找不到，说明原表已被删，保留原值让 Excel 自己去报错
                }
            }
        }

        workbook.Save();
    }
}