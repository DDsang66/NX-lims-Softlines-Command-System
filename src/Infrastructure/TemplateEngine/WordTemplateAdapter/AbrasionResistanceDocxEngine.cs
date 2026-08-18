using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.AbrasionResistance_FW_;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.AbrasionResistance_FW_.NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.AbrasionResistance_FW_;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.PhysicalWeightContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Interface;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.TemplateEngine.WordTemplateAdapter
{
    public class AbrasionResistanceDocxEngine : IAbrasionResistanceDocxEngine, IScopedDependency
    {
        /// <summary>
        /// 填充耐磨报告 — 流程地图:
        ///   1. 打开文件, 定位表并做结构校验(结构不符 → 抛异常);
        ///   2. 表1 报告头: 填 Method/ReportNo/SampleDescription/Condition/TestAtmosphere;
        ///   3. 表2 结果表: 填 Sample/密度/体积损失/磨耗指数/Requirement/Conclusion;
        ///   4. 保存。
        /// </summary>
        public void FillReport(string filePath, AbrasionResistanceReportFillModel model)
        {
            using var doc = WordprocessingDocument.Open(filePath, true);
            var (headerTable, resultTable, calculationTable) = ValidateTemplate(doc);

            // ==================== 表1: 报告头 ====================
            // R0: Method — 10pt 斜体
            SetCellTextItalic10(Row(headerTable, AbrasionResistanceDocxLayout.HeaderRowMethod)!,
                AbrasionResistanceDocxLayout.HeaderMethodCol, model.Method ?? "");

            // R2: Report No — 10pt 正常
            SetCellText10(Row(headerTable, AbrasionResistanceDocxLayout.HeaderRowReportNo)!,
                AbrasionResistanceDocxLayout.HeaderReportNoCol, model.ReportNo ?? "");

            // R2: Date In (列4) — 留空不填
            // R4: Sample Ref (列2) — 留空不填
            // R4: Date Out (列4) — 留空不填

            // R6: Sample Description — 10pt 正常
            SetCellText10(Row(headerTable, AbrasionResistanceDocxLayout.HeaderRowSampleDesc)!,
                AbrasionResistanceDocxLayout.HeaderSampleDescCol, model.SampleResult ?? "");

            // R9: Condition — 10pt 正常
            SetCellText10(Row(headerTable, AbrasionResistanceDocxLayout.HeaderRowCondition)!,
                AbrasionResistanceDocxLayout.HeaderConditionCol, model.Condition ?? "");

            // R11: Test Atmosphere — 10pt 正常
            SetCellText10(Row(headerTable, AbrasionResistanceDocxLayout.HeaderRowAtmosphere)!,
                AbrasionResistanceDocxLayout.HeaderAtmosphereCol, model.TestAtmosphere ?? "");

            // R13: CleanMethod — 10pt 正常
            SetCellText10(Row(headerTable, AbrasionResistanceDocxLayout.HeaderRowCleanMethod)!,
                AbrasionResistanceDocxLayout.HeaderCleanMethodCol, model.CleanMethod ?? "");

            // ==================== 表2: 测试结果 ====================
            // R16: Sample / Density — 10pt 正常
            SetCellText10(Row(resultTable, AbrasionResistanceDocxLayout.ResultRowDensity)!,
                AbrasionResistanceDocxLayout.ResultSampleCol, model.SampleResult ?? "");
            SetCellText10(Row(resultTable, AbrasionResistanceDocxLayout.ResultRowDensity)!,
                AbrasionResistanceDocxLayout.ResultValueCol, model.ResultDensity?.ToString("F4") ?? "");
            // R16: Requirement 和 Conclusion 置空（不填）

            // R17: Sample / Volume Loss — 10pt 正常
            SetCellText10(Row(resultTable, AbrasionResistanceDocxLayout.ResultRowVolLoss)!,
                AbrasionResistanceDocxLayout.ResultSampleCol, model.SampleResult ?? "");
            SetCellText10(Row(resultTable, AbrasionResistanceDocxLayout.ResultRowVolLoss)!,
                AbrasionResistanceDocxLayout.ResultValueCol, model.ResultVolLoss?.ToString("F4") ?? "");
            SetCellText10(Row(resultTable, AbrasionResistanceDocxLayout.ResultRowVolLoss)!,
                AbrasionResistanceDocxLayout.ResultRequirementCol, "≤ " + model.Requirement ?? "");
            SetCellText10(Row(resultTable, AbrasionResistanceDocxLayout.ResultRowVolLoss)!,
                AbrasionResistanceDocxLayout.ResultConclusionCol, model.Conclusion ?? "");

            // R18: Sample / AR Index — 10pt 正常
            SetCellText10(Row(resultTable, AbrasionResistanceDocxLayout.ResultRowARIndex)!,
                AbrasionResistanceDocxLayout.ResultSampleCol, model.SampleResult ?? "");
            SetCellText10(Row(resultTable, AbrasionResistanceDocxLayout.ResultRowARIndex)!,
                AbrasionResistanceDocxLayout.ResultValueCol, model.ResultARIndex?.ToString("F2") ?? "");
            // R18: Requirement 和 Conclusion 置空（不填）

            // ==================== 表3: 密度计算区 — 7.5pt 正常 ====================
            // R27: Specimen A
            SetCellText(Row(calculationTable, AbrasionResistanceDocxLayout.CalcRowDensityA)!,
                AbrasionResistanceDocxLayout.CalcDensityFormulaCol, model.TestDensityA_Formula ?? "");
            SetCellText(Row(calculationTable, AbrasionResistanceDocxLayout.CalcRowDensityA)!,
                AbrasionResistanceDocxLayout.CalcDensityResultCol, model.TestDensityA?.ToString("F4") ?? "");

            // R28: Specimen B
            SetCellText(Row(calculationTable, AbrasionResistanceDocxLayout.CalcRowDensityB)!,
                AbrasionResistanceDocxLayout.CalcDensityFormulaCol, model.TestDensityB_Formula ?? "");
            SetCellText(Row(calculationTable, AbrasionResistanceDocxLayout.CalcRowDensityB)!,
                AbrasionResistanceDocxLayout.CalcDensityResultCol, model.TestDensityB?.ToString("F4") ?? "");

            // R29: Ave
            SetCellText(Row(calculationTable, AbrasionResistanceDocxLayout.CalcRowDensityAvg)!,
                AbrasionResistanceDocxLayout.CalcDensityResultCol, model.ResultDensity?.ToString("F4") ?? "");

            // ==================== 表3: 体积损失区 — 7.5pt 正常 ====================
            // R31: Abrasion distance
            SetCellText(Row(calculationTable, AbrasionResistanceDocxLayout.CalcRowAbrasionDistance)!,
                AbrasionResistanceDocxLayout.CalcVolLossDistanceCol, model.AbrasionDistance ?? "");

            // R32: Specimen 1
            SetCellText(Row(calculationTable, AbrasionResistanceDocxLayout.CalcRowVolLoss1)!,
                AbrasionResistanceDocxLayout.CalcVolLossFormulaCol, model.Specimen1_VolLoss_Formula ?? "");
            SetCellText(Row(calculationTable, AbrasionResistanceDocxLayout.CalcRowVolLoss1)!,
                AbrasionResistanceDocxLayout.CalcVolLossResultCol, model.Specimen1_VolLoss?.ToString("F4") ?? "");

            // R33: Specimen 2
            SetCellText(Row(calculationTable, AbrasionResistanceDocxLayout.CalcRowVolLoss2)!,
                AbrasionResistanceDocxLayout.CalcVolLossFormulaCol, model.Specimen2_VolLoss_Formula ?? "");
            SetCellText(Row(calculationTable, AbrasionResistanceDocxLayout.CalcRowVolLoss2)!,
                AbrasionResistanceDocxLayout.CalcVolLossResultCol, model.Specimen2_VolLoss?.ToString("F4") ?? "");

            // R34: Specimen 3
            SetCellText(Row(calculationTable, AbrasionResistanceDocxLayout.CalcRowVolLoss3)!,
                AbrasionResistanceDocxLayout.CalcVolLossFormulaCol, model.Specimen3_VolLoss_Formula ?? "");
            SetCellText(Row(calculationTable, AbrasionResistanceDocxLayout.CalcRowVolLoss3)!,
                AbrasionResistanceDocxLayout.CalcVolLossResultCol, model.Specimen3_VolLoss?.ToString("F4") ?? "");

            // R35: Ave
            SetCellText(Row(calculationTable, AbrasionResistanceDocxLayout.CalcRowVolLossAvg)!,
                AbrasionResistanceDocxLayout.CalcVolLossResultCol, model.ResultVolLoss?.ToString("F4") ?? "");

            // ==================== 表3: 参照化合物密度区 — 7.5pt 正常 ====================
            //// R38: Specimen A
            //SetCellText(Row(calculationTable, AbrasionResistanceDocxLayout.CalcRowRefDensityA)!,
            //    AbrasionResistanceDocxLayout.CalcRefDensityM1Col, model.RefM1_A?.ToString("F4") ?? "");
            //SetCellText(Row(calculationTable, AbrasionResistanceDocxLayout.CalcRowRefDensityA)!,
            //    AbrasionResistanceDocxLayout.CalcRefDensityM2Col, model.RefM2_A?.ToString("F4") ?? "");
            //SetCellText(Row(calculationTable, AbrasionResistanceDocxLayout.CalcRowRefDensityA)!,
            //    AbrasionResistanceDocxLayout.CalcRefDensityResultCol, model.RefDensityA?.ToString("F4") ?? "");

            //// R39: Specimen B
            //SetCellText(Row(calculationTable, AbrasionResistanceDocxLayout.CalcRowRefDensityB)!,
            //    AbrasionResistanceDocxLayout.CalcRefDensityM1Col, model.RefM1_B?.ToString("F4") ?? "");
            //SetCellText(Row(calculationTable, AbrasionResistanceDocxLayout.CalcRowRefDensityB)!,
            //    AbrasionResistanceDocxLayout.CalcRefDensityM2Col, model.RefM2_B?.ToString("F4") ?? "");
            //SetCellText(Row(calculationTable, AbrasionResistanceDocxLayout.CalcRowRefDensityB)!,
            //    AbrasionResistanceDocxLayout.CalcRefDensityResultCol, model.RefDensityB?.ToString("F4") ?? "");

            //// R40: Ave
            //SetCellText(Row(calculationTable, AbrasionResistanceDocxLayout.CalcRowRefDensityAvg)!,
            //    AbrasionResistanceDocxLayout.CalcRefDensityResultCol, model.RefDensityAvg?.ToString("F4") ?? "");

            // ==================== 表3: 磨耗指数区 — 7.5pt 正常 ====================
            // R43: Specimen 1
            SetCellText(Row(calculationTable, AbrasionResistanceDocxLayout.CalcRowARIndex1)!,
                AbrasionResistanceDocxLayout.CalcARIndexFormulaCol, model.Specimen1_ARIndex_Formula ?? "");
            SetCellText(Row(calculationTable, AbrasionResistanceDocxLayout.CalcRowARIndex1)!,
                AbrasionResistanceDocxLayout.CalcARIndexResultCol, model.Specimen1ARIndex?.ToString("F2") ?? "");

            // R44: Specimen 2
            SetCellText(Row(calculationTable, AbrasionResistanceDocxLayout.CalcRowARIndex2)!,
                AbrasionResistanceDocxLayout.CalcARIndexFormulaCol, model.Specimen2_ARIndex_Formula ?? "");
            SetCellText(Row(calculationTable, AbrasionResistanceDocxLayout.CalcRowARIndex2)!,
                AbrasionResistanceDocxLayout.CalcARIndexResultCol, model.Specimen2ARIndex?.ToString("F2") ?? "");

            // R45: Specimen 3
            SetCellText(Row(calculationTable, AbrasionResistanceDocxLayout.CalcRowARIndex3)!,
                AbrasionResistanceDocxLayout.CalcARIndexFormulaCol, model.Specimen3_ARIndex_Formula ?? "");
            SetCellText(Row(calculationTable, AbrasionResistanceDocxLayout.CalcRowARIndex3)!,
                AbrasionResistanceDocxLayout.CalcARIndexResultCol, model.Specimen3ARIndex?.ToString("F2") ?? "");

            // R46: Ave
            SetCellText(Row(calculationTable, AbrasionResistanceDocxLayout.CalcRowARIndexAvg)!,
                AbrasionResistanceDocxLayout.CalcARIndexResultCol, model.ResultARIndex?.ToString("F2") ?? "");

            doc.MainDocumentPart?.Document?.Save();
        }
        /// <summary>
        /// 校验模板结构并返回已定位的两张表。
        /// 任何一项不符立即抛异常, 宁可失败也不产出错位文档。
        /// </summary>
        /// <summary>
        /// 校验模板结构并返回已定位的三张表。
        /// 任何一项不符立即抛异常, 宁可失败也不产出错位文档。
        /// </summary>
        private (Table Header, Table Result, Table Calculation) ValidateTemplate(WordprocessingDocument doc)
        {
            var headerTable = LocateTable(doc, AbrasionResistanceDocxLayout.HeaderTableMarker)
                ?? throw new InvalidOperationException("耐磨模板缺少报告头表(Report No)");

            // 校验报告头表关键行
            if (Row(headerTable, AbrasionResistanceDocxLayout.HeaderRowMethod) == null)
                throw new InvalidOperationException("耐磨模板报告头表缺少 Method 行(R0)");
            if (Row(headerTable, AbrasionResistanceDocxLayout.HeaderRowReportNo) == null)
                throw new InvalidOperationException("耐磨模板报告头表缺少 Report No 行(R2)");
            if (Row(headerTable, AbrasionResistanceDocxLayout.HeaderRowSampleDesc) == null)
                throw new InvalidOperationException("耐磨模板报告头表缺少 Sample Description 行(R6)");
            if (Row(headerTable, AbrasionResistanceDocxLayout.HeaderRowCondition) == null)
                throw new InvalidOperationException("耐磨模板报告头表缺少 Condition 行(R9)");
            if (Row(headerTable, AbrasionResistanceDocxLayout.HeaderRowAtmosphere) == null)
                throw new InvalidOperationException("耐磨模板报告头表缺少 Test atmosphere 行(R11)");

            // 校验报告头表关键列数 — R2 结构: [Report No:] | [值] | [Date In:] | [值] = 4列
            var r2Cells = Row(headerTable, AbrasionResistanceDocxLayout.HeaderRowReportNo)!.Elements<TableCell>().Count();
            if (r2Cells < 4)
                throw new InvalidOperationException($"耐磨模板报告头表 R2 格数不足(实际{r2Cells}列, 应含 Report No 和 Date In 共4列)");

            // 校验 R6 Sample Description 列数
            var r6Cells = Row(headerTable, AbrasionResistanceDocxLayout.HeaderRowSampleDesc)!.Elements<TableCell>().Count();
            if (r6Cells < 2)
                throw new InvalidOperationException($"耐磨模板报告头表 R6 格数不足(实际{r6Cells}列, 应至少2列)");

            var resultTable = LocateTable(doc, AbrasionResistanceDocxLayout.ResultTableMarker)
                ?? throw new InvalidOperationException("耐磨模板缺少结果表(Sample/Results)");

            // 校验结果表关键行
            if (Row(resultTable, AbrasionResistanceDocxLayout.ResultRowDensity) == null)
                throw new InvalidOperationException("耐磨模板结果表缺少 Density 行(R16)");
            if (Row(resultTable, AbrasionResistanceDocxLayout.ResultRowVolLoss) == null)
                throw new InvalidOperationException("耐磨模板结果表缺少 Volume Loss 行(R17)");
            if (Row(resultTable, AbrasionResistanceDocxLayout.ResultRowARIndex) == null)
                throw new InvalidOperationException("耐磨模板结果表缺少 AR Index 行(R18)");

            // 校验结果表关键列数 — R17 结构: [Sample] | [Results] | [空] | [Requirement] | [Conclusion] = 5列
            var r17Cells = Row(resultTable, AbrasionResistanceDocxLayout.ResultRowVolLoss)!.Elements<TableCell>().Count();
            if (r17Cells < 5)
                throw new InvalidOperationException($"耐磨模板结果表 R17 格数不足(实际{r17Cells}列, 应含 Sample/Results/Requirement/Conclusion 共5列)");

            // ==================== 定位并校验表3: 计算过程 ====================
            var calculationTable = LocateTable(doc, AbrasionResistanceDocxLayout.CalculationTableMarker)
                ?? throw new InvalidOperationException("耐磨模板缺少计算表(4.Calculation)");

            // 校验计算表关键行
            if (Row(calculationTable, AbrasionResistanceDocxLayout.CalcRowDensityTitle) == null)
                throw new InvalidOperationException("耐磨模板计算表缺少 Density of test Sample 行(R26)");
            if (Row(calculationTable, AbrasionResistanceDocxLayout.CalcRowDensityA) == null)
                throw new InvalidOperationException("耐磨模板计算表缺少 Specimen A 密度行(R27)");
            if (Row(calculationTable, AbrasionResistanceDocxLayout.CalcRowDensityB) == null)
                throw new InvalidOperationException("耐磨模板计算表缺少 Specimen B 密度行(R28)");
            if (Row(calculationTable, AbrasionResistanceDocxLayout.CalcRowDensityAvg) == null)
                throw new InvalidOperationException("耐磨模板计算表缺少 Density Ave 行(R29)");

            // 校验密度区列数
            var r27Cells = Row(calculationTable, AbrasionResistanceDocxLayout.CalcRowDensityA)!.Elements<TableCell>().Count();
            if (r27Cells < 3)
                throw new InvalidOperationException($"耐磨模板计算表 R27 格数不足(实际{r27Cells}列, 应至少3列: [Specimen A:] | [公式] | [结果])");

            return (headerTable, resultTable, calculationTable);
        }

        /// <summary>
        /// 获取表格指定行
        /// </summary>
        private static TableRow? Row(Table? t, int i) => t?.Elements<TableRow>().ElementAtOrDefault(i);

        /// <summary>
        /// 按坐标写单元格文本(0-based), 保留原样式。空文本清空该格。
        /// </summary>
        private void SetCellText(TableRow row, int cellIndex, string text)
            => SetCellText(row.Elements<TableCell>().ElementAtOrDefault(cellIndex), text);

        /// <summary>
        /// 按坐标写单元格文本 - Arial 10pt 正常
        /// </summary>
        private void SetCellText10(TableRow row, int cellIndex, string text)
            => SetCellText10(row.Elements<TableCell>().ElementAtOrDefault(cellIndex), text);

        /// <summary>
        /// 写单元格文本 - Arial 10pt 正常
        /// </summary>
        private void SetCellText10(TableCell? cell, string text)
        {
            if (cell == null) return;

            var rp = new RunProperties
            {
                RunFonts = new RunFonts
                {
                    Ascii = "Arial",
                    HighAnsi = "Arial",
                    EastAsia = "Arial"
                },
                FontSize = new FontSize { Val = "20" }  // 10pt = 20 half-points
            };

            var paragraphs = cell.Elements<Paragraph>().ToList();
            for (int i = 1; i < paragraphs.Count; i++) paragraphs[i].Remove();
            var para = paragraphs.FirstOrDefault();
            if (para == null) { para = new Paragraph(); cell.Append(para); }

            foreach (var run in para.Elements<Run>().ToList()) run.Remove();
            if (string.IsNullOrEmpty(text)) return;

            var newRun = new Run(rp);
            para.Append(newRun);
            InsertTextWithLineBreaks(text, newRun);
        }

        /// <summary>
        /// 按坐标写单元格文本(0-based)，指定是否斜体。
        /// </summary>
        private void SetCellText(TableRow row, int cellIndex, string text, bool isItalic)
            => SetCellText(row.Elements<TableCell>().ElementAtOrDefault(cellIndex), text, isItalic);

        /// <summary>
        /// 按坐标写单元格文本 - Arial 10pt 斜体
        /// </summary>
        private void SetCellTextItalic10(TableRow row, int cellIndex, string text)
            => SetCellTextItalic10(row.Elements<TableCell>().ElementAtOrDefault(cellIndex), text);

        /// <summary>
        /// 写单元格文本，硬编码指定字体样式（Arial 7.5pt）。
        /// </summary>
        private void SetCellText(TableCell? cell, string text)
            => SetCellText(cell, text, false);

        /// <summary>
        /// 写单元格文本 - Arial 10pt 斜体
        /// </summary>
        private void SetCellTextItalic10(TableCell? cell, string text)
        {
            if (cell == null) return;

            var rp = new RunProperties
            {
                RunFonts = new RunFonts
                {
                    Ascii = "Arial",
                    HighAnsi = "Arial",
                    EastAsia = "Arial"
                },
                FontSize = new FontSize { Val = "20" },  // 10pt = 20 half-points
                Italic = new Italic { Val = true }
            };

            var paragraphs = cell.Elements<Paragraph>().ToList();
            for (int i = 1; i < paragraphs.Count; i++) paragraphs[i].Remove();
            var para = paragraphs.FirstOrDefault();
            if (para == null) { para = new Paragraph(); cell.Append(para); }

            foreach (var run in para.Elements<Run>().ToList()) run.Remove();
            if (string.IsNullOrEmpty(text)) return;

            var newRun = new Run(rp);
            para.Append(newRun);
            InsertTextWithLineBreaks(text, newRun);
        }

        /// <summary>
        /// 写单元格文本，硬编码指定字体样式（Arial 7.5pt），可指定斜体。
        /// </summary>
        private void SetCellText(TableCell? cell, string text, bool isItalic)
        {
            if (cell == null) return;

            // 硬编码字体样式：Arial 7.5pt
            var rp = new RunProperties
            {
                RunFonts = new RunFonts
                {
                    Ascii = "Arial",
                    HighAnsi = "Arial",
                    EastAsia = "Arial"
                },
                FontSize = new FontSize { Val = "15" },  // 7.5pt = 15 half-points
                Italic = new Italic { Val = isItalic }
            };

            // 保留第一个段落, 删除多余段落
            var paragraphs = cell.Elements<Paragraph>().ToList();
            for (int i = 1; i < paragraphs.Count; i++) paragraphs[i].Remove();
            var para = paragraphs.FirstOrDefault();
            if (para == null) { para = new Paragraph(); cell.Append(para); }

            foreach (var run in para.Elements<Run>().ToList()) run.Remove();
            if (string.IsNullOrEmpty(text)) return;

            var newRun = new Run(rp);
            para.Append(newRun);
            InsertTextWithLineBreaks(text, newRun);
        }

        /// <summary>
        /// 支持换行符的文本插入
        /// </summary>
        private static void InsertTextWithLineBreaks(string text, Run run)
        {
            if (string.IsNullOrEmpty(text)) return;

            var lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0)
                {
                    run.Append(new Break());
                }
                run.Append(new Text(lines[i]));
            }
        }

        /// <summary>
        /// 定位表格（支持书签、内容匹配、索引等多种策略）
        /// </summary>
        private Table? LocateTable(WordprocessingDocument doc, string identifier)
        {
            var table = GetTableByBookmark(doc, identifier);
            if (table != null) return table;

            table = GetTableByContent(doc, identifier);
            if (table != null) return table;

            if (int.TryParse(identifier, out int index))
            {
                table = GetTableByIndex(doc, index);
                if (table != null) return table;
            }

            return null;
        }

        private Table? GetTableByIndex(WordprocessingDocument doc, int index)
        {
            var tables = doc.MainDocumentPart?.Document.Body.Elements<Table>().ToList();
            if (tables == null || index < 0 || index >= tables.Count) return null;
            return tables[index];
        }

        private Table? GetTableByBookmark(WordprocessingDocument doc, string bookmarkName)
        {
            var bookmark = doc.MainDocumentPart?.Document.Body
                .Descendants<BookmarkStart>()
                .FirstOrDefault(b => b.Name == bookmarkName);

            return bookmark?.Ancestors<Table>().FirstOrDefault();
        }

        private Table? GetTableByContent(WordprocessingDocument doc, string searchText)
        {
            return doc.MainDocumentPart?.Document.Body.Elements<Table>()
                .FirstOrDefault(t => t.InnerText.Contains(searchText));
        }

        /// <summary>
        /// 耐磨模板坐标常量 — 模板布局一变, 只改这里
        /// </summary>
        private static class AbrasionResistanceDocxLayout
        {
            // ==================== 表定位标记 ====================
            public const string HeaderTableMarker = "Report No:";      // 表1: 报告头
            public const string ResultTableMarker = "Sample";          // 表2: 结果表
            public const string CalculationTableMarker = "4.Calculation:"; //  表3: 计算区

            // ==================== 表1: 报告头坐标 ====================
            public const int HeaderRowMethod = 0;          // R0: Method (Standard + MethodCategory)
            public const int HeaderMethodCol = 1;          // 列2: 方法值

            public const int HeaderRowReportNo = 2;        // R2: Report No 行
            public const int HeaderReportNoCol = 1;        // 列2: 报告号值

            // R2 列4: Date In — 留空不填
            public const int HeaderRowCleanMethod = 13;    // R13: Clean Method 行
            public const int HeaderCleanMethodCol = 1;     // 列2: 清洁方法值

            public const int HeaderRowSampleRef = 4;       // R4: Sample Ref 行
            // 列2: Sample Ref — 留空不填
            // 列4: Date Out — 留空不填

            public const int HeaderRowSampleDesc = 6;      // R6: Sample Description 行
            public const int HeaderSampleDescCol = 1;      // 列2: 样品描述值

            public const int HeaderRowCondition = 9;       // R9: Condition 行
            public const int HeaderConditionCol = 1;       // 列2: 条件值

            public const int HeaderRowAtmosphere = 11;     // R11: Test atmosphere 行
            public const int HeaderAtmosphereCol = 1;      // 列2: 环境值

            // R13: Test Result — 留空不填

            // ==================== 表2: 结果表坐标 ====================
            public const int ResultRowDensity = 16;        // R16: Density 行
            public const int ResultRowVolLoss = 17;        // R17: Volume Loss 行
            public const int ResultRowARIndex = 18;        // R18: AR Index 行

            public const int ResultSampleCol = 0;          // 列0: Sample 名称
            public const int ResultValueCol = 2;           // 列1: 结果值
            public const int ResultRequirementCol = 3;     // 列3: Requirement
            public const int ResultConclusionCol = 4;      // 列4: Conclusion
                                                           // ==================== 表头列数常量（用于校验） ====================
            public const int HeaderRowReportNoMinCols = 4; // R2 最少4列: [Report No:] | [值] | [Date In:] | [值]
            public const int HeaderRowSampleDescMinCols = 2; // R6 最少2列: [Sample Description:] | [值]
            public const int ResultRowMinCols = 5;          // 结果行最少5列: [Sample] | [Results] | [空] | [Requirement] | [Conclusion]


            // ==================== 表3: 计算过程坐标（内部相对行索引） ====================
            // 密度计算区
            public const int CalcRowDensityTitle = 2;           // "Density of test Sample:"
            public const int CalcRowDensityA = 3;               // Specimen A
            public const int CalcRowDensityB = 4;               // Specimen B
            public const int CalcRowDensityAvg = 5;             // Ave

            public const int CalcDensityFormulaCol = 1;         // 列1: 公式文本
            public const int CalcDensityResultCol = 2;          // 列2: 密度结果

            // 体积损失区
            public const int CalcRowAbrasionDistance = 7;       // "Abrasion distance:"
            public const int CalcRowVolLoss1 = 8;               // Specimen 1
            public const int CalcRowVolLoss2 = 9;               // Specimen 2
            public const int CalcRowVolLoss3 = 10;              // Specimen 3
            public const int CalcRowVolLossAvg = 11;            // Ave

            public const int CalcVolLossDistanceCol = 1;        // 列1: 磨损里程值
            public const int CalcVolLossFormulaCol = 1;         // 列1: 公式文本
            public const int CalcVolLossResultCol = 2;          // 列2: 体积损失结果

            // 参照化合物密度区
            public const int CalcRowRefDensityTitle = 13;       // "Density of reference compound:"
            public const int CalcRowRefDensityA = 14;           // Specimen A
            public const int CalcRowRefDensityB = 15;           // Specimen B
            public const int CalcRowRefDensityAvg = 16;         // Ave

            public const int CalcRefDensityM1Col = 1;           // 列1: m1值
            public const int CalcRefDensityM2Col = 2;           // 列2: m2值
            public const int CalcRefDensityResultCol = 3;       // 列3: 密度结果

            // 磨耗指数区
            public const int CalcRowARIndexTitle = 18;          // "Abrasion resistance Index:"
            public const int CalcRowARIndex1 = 19;              // Specimen 1
            public const int CalcRowARIndex2 = 20;              // Specimen 2
            public const int CalcRowARIndex3 = 21;              // Specimen 3
            public const int CalcRowARIndexAvg = 22;            // Ave

            public const int CalcARIndexFormulaCol = 1;         // 列1: 公式文本
            public const int CalcARIndexResultCol = 2;          // 列3: 磨耗指数结果
        }
    }

}

