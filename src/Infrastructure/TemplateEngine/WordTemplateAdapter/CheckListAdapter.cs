using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.CheckListContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using System.Drawing;
using System.Drawing.Imaging;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.TemplateEngine.WordTemplateAdapter
{
    public class CheckListAdapter:IScopedDependency
    {

        /// <summary>
        /// 异步包装 (保持接口一致性)
        /// </summary>
        public async Task FillCheckListAsync(string filePath, CheckListGenerateDto model, Bitmap? barcode = null)
        {
            await Task.Run(() => FillCheckList(filePath, model, barcode));
        }

        public void FillCheckList(string filePath, CheckListGenerateDto model, Bitmap barcode) 
        {
            using var doc = WordprocessingDocument.Open(filePath, true);

            FillReportNumber(doc, model.ReportNo ?? "");

            // ==================== 2. 填充条形码 ====================
            if (barcode != null)
            {
                // 处理条形码：调整为 Word 兼容尺寸并添加背景
                var processedBarcode = BarcodePictureHelper.AddWhiteBackground(barcode, 10);
                var resizedBarcode = BarcodePictureHelper.Resize(processedBarcode, 200, 80);

                FillBarcode(doc, resizedBarcode);

                // 释放资源
                processedBarcode.Dispose();
                resizedBarcode.Dispose();
            }

            var mainTable = LocateMainTable(doc)
                ?? throw new InvalidOperationException("Checklist 模板缺少主数据表");

            // 按 TestGroup 分组
            var groups = model.Items
                .GroupBy(x => x.TestGroup ?? "Default")
                .ToList();

            if (!groups.Any())
                throw new InvalidOperationException("Checklist 没有可填充的测试项");

            // 获取模板数据行 (含表头结构)
            var templateRow = GetTemplateDataRow(mainTable)
                ?? throw new InvalidOperationException("Checklist 模板缺少数据行结构");

            // 清除模板行中的示例数据 (保留结构)
            WordEditEngine.ClearRowContent(templateRow);

            // 处理第一个组: 直接填充主表
            var firstGroup = groups.First();
            FillGroupTable(mainTable, templateRow, firstGroup.Key, firstGroup.ToList(), 1);

            // 处理其余组: 克隆表格
            for (int i = 1; i < groups.Count; i++)
            {
                var group = groups[i];

                WordEditEngine.InsertEmptyParagraphAfterTable(mainTable);

                var clonedTable = WordEditEngine.CloneTableAfter(mainTable);

                // 获取克隆表中的数据行 (与模板行结构一致)
                var clonedRow = GetClonedDataRow(clonedTable, templateRow)
                    ?? throw new InvalidOperationException($"克隆表缺少数据行结构 (组: {group.Key})");

                FillGroupTable(clonedTable, clonedRow, group.Key, group.ToList(), i + 1);
            }

            doc.MainDocumentPart?.Document?.Save();
        }


        /// <summary>
        /// 填充单个组别的表格
        /// </summary>
        private void FillGroupTable(Table table, TableRow dataRow, string groupName, List<CheckListResponseItemDto> items, int groupIndex)
        {
            // 如果组名不是 "Default"，更新组标题 (假设表格第一行或某处有组名占位)
            if (groupName != "Default")
            {
                // 查找并更新组名单元格 (可根据实际模板调整)
                UpdateGroupName(table, groupName, groupIndex);
            }

            // 获取当前数据行之后的所有行 (用于扩容)
            var existingRows = table.Elements<TableRow>().ToList();
            int dataRowIndex = existingRows.IndexOf(dataRow);

            // 清除现有数据行 (保留第一行作为模板)
            WordEditEngine.ClearRowContent(dataRow);

            // 如果只有一项, 直接填充模板行
            if (items.Count == 1)
            {
                FillDataRow(dataRow, items[0], 1);
                return;
            }

            // 多项: 先填充第一行, 再克隆追加
            FillDataRow(dataRow, items[0], 1);

            for (int i = 1; i < items.Count; i++)
            {
                var newRow = WordEditEngine.AppendClonedRow(table);
                if (newRow == null)
                    throw new InvalidOperationException($"无法克隆行 (组: {groupName}, 项: {i + 1})");

                FillDataRow(newRow, items[i], i + 1);
            }
        }

        /// <summary>
        /// 填充单行数据
        /// </summary>
        private void FillDataRow(TableRow row, CheckListResponseItemDto item, int index)
        {
            var cells = row.Elements<TableCell>().ToList();

            // 根据模板列顺序填充 (0-based)
            // 假设模板列顺序: Index | TestItem | TestMethod | TestParam | Sample | CuttingMethod | Requirement
            SetCellText(cells, 0, index.ToString());                                    // Index
            SetCellText(cells, 1, item.TestItem);                                      // TestItem
            SetCellText(cells, 2, string.Join(", ", item.Standards ?? Enumerable.Empty<string>())); // TestMethod
            SetCellText(cells, 3, item.Parameter);                                     // TestParam
            SetCellText(cells, 4, string.Join(", ", item.Samples ?? new List<string>())); // Sample
            SetCellText(cells, 5, item.CuttingMethod);                                 // CuttingMethod
            SetCellText(cells, 6, item.Requirement);                                   // Requirement
        }

        /// <summary>
        /// 设置单元格文本
        /// </summary>
        private void SetCellText(List<TableCell> cells, int index, string text)
        {
            if (index >= cells.Count) return;

            var cell = cells[index];
            var paragraphs = cell.Elements<Paragraph>().ToList();
            var para = paragraphs.FirstOrDefault() ?? new Paragraph();

            // 清空原有内容
            foreach (var run in para.Elements<Run>().ToList())
                run.Remove();

            if (!string.IsNullOrEmpty(text))
            {
                // 创建 RunProperties 设置 Arial 9pt
                var runProperties = new RunProperties
                {
                    RunFonts = new RunFonts
                    {
                        Ascii = "Arial",
                        HighAnsi = "Arial",
                        EastAsia = "Arial"
                    },
                    FontSize = new FontSize { Val = "18" }  // 9pt = 18 half-points
                };

                var run = new Run(runProperties);
                run.Append(new Text(text));
                para.Append(run);
            }

            if (!cell.HasChildren)
                cell.Append(para);
        }

        /// <summary>
        /// 定位主数据表
        /// </summary>
        private Table? LocateMainTable(WordprocessingDocument doc)
        {
            // 优先通过书签定位
            var table = GetTableByBookmark(doc, "CheckListTable");
            if (table != null) return table;

            // 其次通过内容定位 (包含 "Index" 和 "Test Item" 等关键字)
            table = doc.MainDocumentPart?.Document.Body.Elements<Table>()
                .FirstOrDefault(t => t.InnerText.Contains("Index") && t.InnerText.Contains("Test Item"));
            if (table != null) return table;

            // 最后取第一个表格
            return doc.MainDocumentPart?.Document.Body.Elements<Table>().FirstOrDefault();
        }

        /// <summary>
        /// 通过书签定位表格
        /// </summary>
        private Table? GetTableByBookmark(WordprocessingDocument doc, string bookmarkName)
        {
            var bookmark = doc.MainDocumentPart?.Document.Body
                .Descendants<BookmarkStart>()
                .FirstOrDefault(b => b.Name == bookmarkName);

            return bookmark?.Ancestors<Table>().FirstOrDefault();
        }

        /// <summary>
        /// 获取模板数据行 (包含表头结构的数据行)
        /// </summary>
        private TableRow? GetTemplateDataRow(Table table)
        {
            var rows = table.Elements<TableRow>().ToList();

            // 假设数据行是表头后的第一行 (索引1)
            if (rows.Count > 1)
                return rows[1];

            // 如果只有一行, 返回该行作为模板 (可能是单行模板)
            return rows.FirstOrDefault();
        }

        /// <summary>
        /// 获取克隆表中的对应数据行
        /// </summary>
        private TableRow? GetClonedDataRow(Table clonedTable, TableRow templateRow)
        {
            // 克隆表与原表结构一致, 取相同位置的行
            var rows = clonedTable.Elements<TableRow>().ToList();
            var originalRows = templateRow.Parent?.Elements<TableRow>().ToList();

            if (originalRows == null)
                return rows.ElementAtOrDefault(1); // 默认取第二行

            int index = originalRows.IndexOf(templateRow);
            if (index >= 0 && index < rows.Count)
                return rows[index];

            return rows.ElementAtOrDefault(1);
        }

        /// <summary>
        /// 更新组名 (可根据实际模板结构调整)
        /// </summary>
        private void UpdateGroupName(Table table, string groupName, int groupIndex)
        {
            // 尝试在表格中查找组名占位符并替换
            // 假设模板中有 "GroupName" 或 "Test Group" 字样
            foreach (var cell in table.Descendants<TableCell>())
            {
                var text = cell.InnerText;
                if (text.Contains("GroupName") || text.Contains("Test Group"))
                {
                    // 替换为实际组名
                    var paragraphs = cell.Elements<Paragraph>().ToList();
                    foreach (var para in paragraphs)
                    {
                        foreach (var run in para.Elements<Run>().ToList())
                            run.Remove();

                        para.Append(new Run(new Text($"Test Group: {groupName}")));
                    }
                    return;
                }
            }
        }

        /// <summary>
        /// 填充报告号
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="reportNumber"></param>
        private void FillReportNumber(WordprocessingDocument doc, string reportNumber) 
        {
            // 优先通过书签定位
            var table = GetTableByBookmark(doc, "Test Report Number");

            // 其次通过内容定位 (包含 "Index" 和 "Test Item" 等关键字)
            table = doc.MainDocumentPart?.Document.Body.Elements<Table>()
                .FirstOrDefault(t => t.InnerText.Contains("Test Report Number"));

            var row = table.Elements<TableRow>().ElementAtOrDefault(ChecklistLayout.ReportNoRow);

            var cell = row.Elements<TableCell>().ElementAtOrDefault(ChecklistLayout.ReportNoCol);

            var cells = row.Elements<TableCell>().ToList();

            SetCellText(cells, ChecklistLayout.ReportNoCol, reportNumber);
        }

        /// <summary>
        /// 填充条形码到 Word 文档（仅通过行列坐标定位）
        /// </summary>
        private void FillBarcode(WordprocessingDocument doc, Bitmap barcode)
        {
            if (barcode == null) return;

            try
            {
                // 定位条形码表格
                var table = LocateTableByText(doc, ChecklistLayout.BarcodeTableMarker);
                if (table == null) return;

                // 获取指定行
                var row = table.Elements<TableRow>().ElementAtOrDefault(ChecklistLayout.BarcodeRow);
                if (row == null) return;

                // 获取指定列
                var cell = row.Elements<TableCell>().ElementAtOrDefault(ChecklistLayout.BarcodeCol);
                if (cell == null) return;

                // 插入条形码
                InsertBarcodeIntoCell(cell, doc, barcode);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"填充条形码失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 通过书签定位表格
        /// </summary>
        private Table? LocateTableByText(WordprocessingDocument doc, string bookmarkName)
        {
            // 优先通过书签定位
            var table = GetTableByBookmark(doc, "Barcode");
            if (table != null) return table;

            // 其次通过内容定位 (包含 "Index" 和 "Test Item" 等关键字)
            table = doc.MainDocumentPart?.Document.Body.Elements<Table>()
                .FirstOrDefault(t => t.InnerText.Contains("BarCode"));
            if (table != null) return table;

            // 最后取第一个表格
            return doc.MainDocumentPart?.Document.Body.Elements<Table>().FirstOrDefault();
        }

        /// <summary>
        /// 在单元格中插入条形码
        /// </summary>
        private void InsertBarcodeIntoCell(TableCell cell, WordprocessingDocument doc, Bitmap barcode)
        {
            // 获取或创建段落
            var para = cell.Elements<Paragraph>().FirstOrDefault() ?? new Paragraph();

            // 清空段落内容
            para.RemoveAllChildren<Run>();

            // 创建条形码 Drawing
            var drawing = CreateBarcodeDrawing(doc, barcode);
            if (drawing != null)
            {
                var run = new Run();
                run.Append(drawing);
                para.Append(run);
            }

            // 如果单元格没有段落，添加段落
            if (!cell.HasChildren)
            {
                cell.Append(para);
            }
        }

        /// <summary>
        /// Checklist 模板坐标常量
        /// </summary>
        private static class ChecklistLayout
        {
            // ==================== 表定位标记 ====================
            public const string MainTableMarker = "CheckListTable";      // 主数据表书签
            public const string BarcodeTableMarker = "Barcode";     // 条形码所在表书签

            // ==================== 条形码坐标 ====================
            public const int BarcodeRow = 4;         // 条形码所在行 (0-based)
            public const int BarcodeCol = 4;         // 条形码所在列 (0-based)

            public const int ReportNoRow = 0;         // 报告号所在行 (0-based)
            public const int ReportNoCol = 1;         // 报告号所在列 (0-based)
        }



        /// <summary>
        /// 创建条形码 Drawing 对象（兼容旧版本 OpenXml）
        /// </summary>
        private Drawing? CreateBarcodeDrawing(WordprocessingDocument doc, Bitmap barcode)
        {
            try
            {
                using var memoryStream = new MemoryStream();
                barcode.Save(memoryStream, ImageFormat.Png);
                var imageBytes = memoryStream.ToArray();

                var imagePart = doc.MainDocumentPart?.AddImagePart(ImagePartType.Png);
                if (imagePart == null) return null;

                using var imageStream = new MemoryStream(imageBytes);
                imagePart.FeedData(imageStream);

                var imageId = GetNextImageId(doc);
                var emuWidth = (long)(barcode.Width * 9525);
                var emuHeight = (long)(barcode.Height * 9525);
                var relationshipId = doc.MainDocumentPart?.GetIdOfPart(imagePart) ?? "";

                // 使用 DocumentFormat.OpenXml.Wordprocessing.Inline（旧版本）
                var element = new Drawing(
                    new DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline(
                        new DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent { Cx = emuWidth, Cy = emuHeight },
                       new DocumentFormat.OpenXml.Drawing.Wordprocessing.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                        new DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties { Id = (uint)imageId, Name = "Barcode" },
                        new DocumentFormat.OpenXml.Drawing.NonVisualGraphicFrameDrawingProperties(
                            new DocumentFormat.OpenXml.Drawing.GraphicFrameLocks { NoChangeAspect = true }
                        ),
                        new DocumentFormat.OpenXml.Drawing.Graphic(
                            new DocumentFormat.OpenXml.Drawing.GraphicData(
                                new DocumentFormat.OpenXml.Drawing.Pictures.Picture(
                                    new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureProperties(
                                        new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualDrawingProperties { Id = (uint)imageId, Name = "Barcode" },
                                        new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureDrawingProperties()
                                    ),
                                    new DocumentFormat.OpenXml.Drawing.Pictures.BlipFill(
                                        new DocumentFormat.OpenXml.Drawing.Blip(
                                            new DocumentFormat.OpenXml.Drawing.BlipExtensionList(
                                                new DocumentFormat.OpenXml.Drawing.BlipExtension { Uri = "{28A0092B-C50C-407E-A947-70E740481C1C}" }
                                            )
                                        )
                                        {
                                            Embed = relationshipId,
                                            CompressionState = DocumentFormat.OpenXml.Drawing.BlipCompressionValues.Print
                                        },
                                        new DocumentFormat.OpenXml.Drawing.Stretch(new DocumentFormat.OpenXml.Drawing.FillRectangle())
                                    ),
                                    new DocumentFormat.OpenXml.Drawing.Pictures.ShapeProperties(
                                        new DocumentFormat.OpenXml.Drawing.Transform2D(
                                            new DocumentFormat.OpenXml.Drawing.Offset { X = 0L, Y = 0L },
                                            new DocumentFormat.OpenXml.Drawing.Extents { Cx = emuWidth, Cy = emuHeight }
                                        ),
                                        new DocumentFormat.OpenXml.Drawing.PresetGeometry(new DocumentFormat.OpenXml.Drawing.AdjustValueList())
                                        {
                                            Preset = DocumentFormat.OpenXml.Drawing.ShapeTypeValues.Rectangle
                                        }
                                    )
                                )
                            )
                            { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
                        )
                    )
                    {
                        DistanceFromTop = 0U,
                        DistanceFromBottom = 0U,
                        DistanceFromLeft = 0U,
                        DistanceFromRight = 0U
                    }
                );

                return element;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建条形码失败: {ex.Message}");
                return null;
            }
        }


        /// <summary>
        /// 获取下一个可用的图片 ID
        /// </summary>
        private int GetNextImageId(WordprocessingDocument doc)
        {
            var maxId = 0;
            var drawings = doc.MainDocumentPart?.Document.Body
                .Descendants<DocumentFormat.OpenXml.Wordprocessing.Drawing>().ToList();

            if (drawings != null)
            {
                foreach (var drawing in drawings)
                {
                    var docProps = drawing.Descendants<DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties>().FirstOrDefault();
                    if (docProps?.Id?.Value != null)
                    {
                        int id = (int)docProps.Id.Value;
                        if (id > maxId)
                            maxId = id;
                    }
                }
            }

            return maxId + 1;
        }

    }
}
