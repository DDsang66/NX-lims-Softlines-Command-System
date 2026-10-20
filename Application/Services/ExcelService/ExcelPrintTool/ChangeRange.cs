using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using NX_lims_Softlines_Command_System.Application.Services.ExcelService.ExcelPrintTool;

public static class SheetSorter
{
    /// <summary>
    /// 排序工作表并修正 Named Range 的 localSheetId，保证名称与重新排序后的表对应。
    /// param filePath: Excel 文件路径
    /// param rankSelector: 业务顺序（sheet 名 → 档位）；不传则沿用旧的 D→P→S→A→字母序。
    /// </summary>
    public static void SortSheetsAndFixNames(string filePath, Func<string, int>? rankSelector = null)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        using var doc = SpreadsheetDocument.Open(stream, true);

        var workbook = doc.WorkbookPart!.Workbook;
        var sheets = workbook.Sheets;
        var definedNames = workbook.DefinedNames;

        /* 1. 取出原顺序的 sheet 信息 */
        var sheetList = sheets!.Elements<Sheet>().ToList();
        var nameMap = sheetList.Select((s, idx) => new { Sheet = s, OldIndex = idx }).ToList();

        /* 2. 自定义排序 */
        var ordered = rankSelector == null
            // 旧规则：首字母 D→P→S→A→其他，再按名字
            ? nameMap
                .OrderBy(x => LegacyGroup(x.Sheet.Name!.Value!))
                .ThenBy(x => x.Sheet.Name!.Value, StringComparer.OrdinalIgnoreCase)
                .ToList()
            // 业务顺序：先按档位，档内沿用旧规则；副本按基名紧跟基表
            : nameMap
                .OrderBy(x => rankSelector(x.Sheet.Name!.Value!))
                .ThenBy(x => LegacyGroup(WetSheetOrder.BaseName(x.Sheet.Name!.Value!)))
                .ThenBy(x => WetSheetOrder.BaseName(x.Sheet.Name!.Value!), StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => WetSheetOrder.CopyIndex(x.Sheet.Name!.Value!))
                .ThenBy(x => x.Sheet.Name!.Value, StringComparer.OrdinalIgnoreCase)
                .ToList();

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

    /// <summary>旧排序规则的字母分档：D=0，P=1，S=2，A=3，其余=4。</summary>
    private static int LegacyGroup(string sheetName)
    {
        var first = char.ToUpperInvariant(sheetName[0]);
        return first switch
        {
            'D' => 0,
            'P' => 1,
            'S' => 2,
            'A' => 3,
            _ => 4
        };
    }
}