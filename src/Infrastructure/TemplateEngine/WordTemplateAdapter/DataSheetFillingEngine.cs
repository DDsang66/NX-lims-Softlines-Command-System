using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using System.Runtime.CompilerServices;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.TemplateEngine.WordTemplateAdapter
{
    public class DataSheetFillingEngine : IScopedDependency
    {
        /// <summary>Sample area 书签前缀，同时是 area 容量分组的第一组。</summary>
        private const string SamplePrefix = "Sample_data_";

        /// <summary>水洗次数 area 书签前缀，与 Sample 同表但不同行。</summary>
        private const string AfterWashPrefix = "After_wash_data_";

        private readonly IFileStorageService _fileStorage;

        /// <summary>
        /// 扩容行为配置。内聚阶段先内置默认实例，避免改动 DI 注册；
        /// TODO(封装): 后续需要按模板差异化配置时，改为构造函数注入并从配置源绑定。
        /// </summary>
        private readonly AreaExpansionOptions _expansionOptions = new();

        public DataSheetFillingEngine(IFileStorageService fileStorage)
        {
            _fileStorage = fileStorage;
        }

        /// <summary>
        /// 主方法，流程入口
        /// </summary>
        /// <param name="model">数据模型</param>
        public string FillDataSheet(DataSheetModel model)
        {
            var templatePath = model.TemplateUrl;
            string docxFileName = $"{model.ReportNumber}_{DateTime.Now:yyMMddHHmmss}_{model.TestItem}.docx";

            string fileUrl = Path.Combine("DocxModel", "SaveDocx",docxFileName);

            string targetDocxPath = _fileStorage.CopyTemplate(
                templatePath,
                Path.Combine("DocxModel", "SaveDocx"),
                docxFileName);

            try
            {
                using var doc = WordprocessingDocument.Open(targetDocxPath, true);
                var mainPart = doc.MainDocumentPart!;
                var body = mainPart.Document.Body
                    ?? throw new DataSheetFillException("docx 缺少 Body，无法填充", retryable: false);

                // 1. 构建书签映射字典
                var bookmarkValues = BuildBookmarkValues(model);

                // 2. 先扩容 area，把 Sample_data_{A+1..} 等书签真正造进文档。
                //    原因：ReplaceBookmarks 是按「文档里存在的书签」驱动的（见 ReplaceBookmarks 的 Where 过滤），
                //    字典里有值但文档没有对应书签时该值会被静默丢弃，所以必须先造书签再写值。
                var requiredSampleCount = model.SampleMap?.SampleMetaData?.Count ?? 0;
                var requiredAfterWashCount = model.AfterWashMap?.AfterWashMetaData?.Count ?? 0;
                var expanded = TryExpandAreas(body, requiredSampleCount, requiredAfterWashCount);

                // TODO(封装): 接入 ILogger 记录 expanded 结果；当前先用 Debug 输出，便于本地确认是否发生过扩容
                if (expanded) System.Diagnostics.Debug.WriteLine($"[DataSheetFilling] area expanded for {model.ReportNumber}");

                // 3. 替换所有匹配的书签（此刻编号 1..M 全部能在文档中命中）
                ReplaceBookmarks(mainPart, bookmarkValues);

                mainPart.Document.Save();

                return fileUrl;
            }
            catch (Exception ex) when (ex is not DataSheetFillException and not OperationCanceledException)
            {
                // 失败不留半成品：CopyTemplate 已把目标文件落到磁盘，残留会被误当作有效产出
                TryDeleteQuietly(targetDocxPath);

                // 结构/数据问题不可重试；文件占用与 IO 抖动可重试
                var retryable = ex is IOException or UnauthorizedAccessException;
                throw new DataSheetFillException($"FillDataSheet failed: {ex.Message}", retryable, ex);
            }
        }

        /// <summary>
        /// 按模型数据量把模板中的 area 块扩容到足够容量。
        /// 数据量未超过模板预留容量时直接返回，不产生任何文档改动与探测开销。
        /// </summary>
        /// <param name="body">文档主体。</param>
        /// <param name="requiredSampleCount">Sample_data 需要的总个数 M（含模板预留部分）。</param>
        /// <param name="requiredAfterWashCount">After_wash_data 需要的总个数 M（含模板预留部分）。</param>
        /// <returns>是否实际发生了扩容。</returns>
        /// <exception cref="DataSheetFillException">模板结构不符合约定时抛出（不可重试）。</exception>
        // TODO(封装): 测试通过后，本方法与 ProbeAreaLayout / CloneBlockAndRenumber
        // TODO(封装): 应抽为独立的 AreaLayoutProbe（只读探测）与 AreaExpander（唯一改 XML 的地方）。
        private bool TryExpandAreas(Body body, int requiredSampleCount, int requiredAfterWashCount)
        {
            // 快速跳过：模型没有任何 area 数据时连模板都不用探测
            if (requiredSampleCount <= 0 && requiredAfterWashCount <= 0) return false;

            var layout = ProbeAreaLayout(body);

            // 各分支所需块数 = ⌈required / cols⌉；物理块数取各分支的最大值
            var requiredBlocksPerBranch = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var branch in layout.Branches)
            {
                var required = branch.Prefix == SamplePrefix ? requiredSampleCount : requiredAfterWashCount;
                requiredBlocksPerBranch[branch.Prefix] = required <= 0
                    ? 0
                    : (required + branch.ColumnsPerBlock - 1) / branch.ColumnsPerBlock;
            }

            var templateBlockCount = layout.Branches.Max(b => b.Blocks.Count);
            var targetBlockCount = requiredBlocksPerBranch.Values.DefaultIfEmpty(0).Max();
            var newBlockCount = targetBlockCount - templateBlockCount;
            if (newBlockCount <= 0) return false;

            var nextBookmarkId = GetNextBookmarkId(body);

            // 复制源取第一个分支的最后一块：各分支的块边界虽不同（Sample 从标签行起、AfterWash 从表头行起），
            // 但第一个分支的块已覆盖整个物理块的全部行（含两组书签所在行）
            var templateBlock = layout.Branches[0].Blocks[^1];
            var anchor = templateBlock[^1];

            for (var k = 1; k <= newBlockCount; k++)
            {
                // 同一个物理块只克隆一次：把各前缀的改名映射合并进同一张表，
                // 按分支分别克隆会导致块数翻倍并产出「只含水洗值」的孤块
                var nameMap = new Dictionary<string, string>(StringComparer.Ordinal);

                foreach (var branch in layout.Branches)
                {
                    if (!requiredBlocksPerBranch.TryGetValue(branch.Prefix, out var branchTarget)) continue;
                    if (branchTarget - templateBlockCount < k) continue;   // 该分支不需要第 k 个新块

                    foreach (var pair in BuildBlockNameMap(branch, k))
                        nameMap[pair.Key] = pair.Value;
                }

                if (nameMap.Count == 0) continue;

                foreach (var clonedRow in CloneBlockAndRenumber(templateBlock, nameMap, ref nextBookmarkId))
                {
                    // 锚点每次前推到刚插入的行，保证新块内部行序与编号递增一致
                    anchor.InsertAfterSelf(clonedRow);
                    anchor = clonedRow;
                }
            }

            InsertPageBreakIfOverflow(layout.StructureTable);

            return true;
        }

        /// <summary>
        /// 构造「克隆第 k 个新块」时使用的精确书签改名表（旧名 -> 新名）。
        /// 编号按块容量连续推进：第 k 个新块第 c 列的编号 = A + (k-1)*cols + c。
        /// </summary>
        /// <param name="branch">该前缀分支的块级布局信息。</param>
        /// <param name="k">第几个新块（从 1 开始）。</param>
        /// <returns>旧书签名到新书签名的映射；未出现在表中的书签保持原名。</returns>
        private static Dictionary<string, string> BuildBlockNameMap(AreaBranchLayout branch, int k)
        {
            var nameMap = new Dictionary<string, string>(StringComparer.Ordinal);

            // 复制源是模板最后一块，其编号区间为 [A - cols + 1, A]
            var templateBlockStartIndex = branch.AreaCount - branch.ColumnsPerBlock + 1;

            for (var column = 0; column < branch.ColumnsPerBlock; column++)
            {
                var oldIndex = templateBlockStartIndex + column;

                // 即使该位置没有对应测点，也必须生成书签占位：
                // ① 保持块的列结构完整（格式与模板一致）；
                // ② 下次 M 增长时可直接命中已存在的书签，无需再次扩容。
                var newIndex = branch.AreaCount + (k - 1) * branch.ColumnsPerBlock + column + 1;

                nameMap[$"{branch.Prefix}{oldIndex}"] = $"{branch.Prefix}{newIndex}";
            }

            return nameMap;
        }

        /// <summary>
        /// 只读探测 structure 容器与各 area 分支的块级布局。
        /// 容量单位是「块」：一块内含 ColumnsPerBlock 个 area 位置（横向按列排布），纵向跨 BlockHeight 行。
        /// </summary>
        /// <param name="body">文档主体。</param>
        /// <returns>容器表格与各前缀分支的块级布局信息。</returns>
        /// <exception cref="DataSheetFillException">
        /// 缺少 area 书签、书签跨表、块结构不齐、无法推导块高、标签行落在表头行或存在纵向合并时抛出。
        /// </exception>
        // TODO(封装): 抽为独立的 AreaLayoutProbe 类型。
        private AreaLayoutProbe ProbeAreaLayout(Body body)
        {
            var sampleBookmarks = body.Descendants<BookmarkStart>()
                .Where(b => b.Name?.Value is { Length: > 0 } name && ParseAreaIndex(name, SamplePrefix) > 0)
                .ToList();

            if (sampleBookmarks.Count == 0)
                throw new DataSheetFillException($"模板中不存在 {SamplePrefix}* 书签，无法扩容", retryable: false);

            // 一个 Table 就是一个 structure 容器：取全部 Sample 书签共同的最近表格
            var structureTable = sampleBookmarks
                .Select(b => b.Ancestors<Table>().FirstOrDefault())
                .FirstOrDefault(t => t != null)
                ?? throw new DataSheetFillException($"{SamplePrefix}* 不在表格内，模板不符合结构约定", retryable: false);

            var tableRows = structureTable.Elements<TableRow>().ToList();
            if (tableRows.Count == 0)
                throw new DataSheetFillException("structure 容器没有任何表格行，模板不符合结构约定", retryable: false);

            var branches = new List<AreaBranchLayout>();

            foreach (var prefix in new[] { SamplePrefix, AfterWashPrefix })
            {
                var bookmarks = CollectAreaBookmarks(body, prefix, structureTable);

                // 模板没有该组（例如 AfterWashDataCount = 0）时整组跳过
                if (bookmarks.Count == 0) continue;

                // 现有 area 总数 A 取自模板实际书签的最大编号：
                // 不依赖 TemplateStructure，也不受 ModelSnapshot 里旧值影响
                var areaCount = bookmarks
                    .Select(b => ParseAreaIndex(b.Name!.Value!, prefix))
                    .DefaultIfEmpty(0)
                    .Max();

                if (areaCount <= 0) continue;

                // 块的个数 B = 含该前缀书签的不同行的数量：
                // 一行是「一块的标签行」，块内的多个 area 以列的形式排在同一行上
                var tagRowIndexes = bookmarks
                    .Select(b => b.Ancestors<TableRow>().FirstOrDefault())
                    .Where(r => r != null)
                    .Distinct()
                    .Select(r => tableRows.IndexOf(r!))
                    .Where(index => index >= 0)
                    .OrderBy(index => index)
                    .ToList();

                if (tagRowIndexes.Count == 0)
                    throw new DataSheetFillException(
                        $"{prefix}* 书签均不在表格行内，模板不符合结构约定", retryable: false);

                if (areaCount % tagRowIndexes.Count != 0)
                    throw new DataSheetFillException(
                        $"{prefix}* 的 area 数({areaCount}) 不能被块数({tagRowIndexes.Count}) 整除，块结构不齐，需人工确认模板",
                        retryable: false);

                var columnsPerBlock = areaCount / tagRowIndexes.Count;

                // 块高推导：多块模板用相邻标签行的行号差；
                // 单块模板（所有 area 横向排在同一标签行）没有相邻标签行可比，
                // 退化为「标签行到表尾」，此时整张表就是一块
                int blockHeight;
                if (tagRowIndexes.Count >= 2)
                {
                    blockHeight = tagRowIndexes[1] - tagRowIndexes[0];
                }
                else
                {
                    blockHeight = tableRows.Count - tagRowIndexes[0];
                }

                // 单块模板的块高 = 标签行到表尾；若推导值超出表格实际剩余行数，说明模板结构异常，直接阻断
                if (blockHeight <= 0 || blockHeight > tableRows.Count - tagRowIndexes[0])
                    throw new DataSheetFillException(
                        $"{prefix}* 块高({blockHeight})超出表格范围(剩余 {tableRows.Count - tagRowIndexes[0]} 行)，需人工确认模板",
                        retryable: false);

                // 切出每一块的行序列：从标签行起，到下一标签行的前一行；
                // 最后一块没有下一标签行作边界，按块高向后取（并夹在表格实际行数内）
                var blocks = new List<IReadOnlyList<TableRow>>(tagRowIndexes.Count);
                for (var blockIndex = 0; blockIndex < tagRowIndexes.Count; blockIndex++)
                {
                    var start = tagRowIndexes[blockIndex];
                    var end = blockIndex + 1 < tagRowIndexes.Count
                        ? tagRowIndexes[blockIndex + 1] - 1
                        : Math.Min(start + blockHeight - 1, tableRows.Count - 1);

                    if (end < start)
                        throw new DataSheetFillException(
                            $"{prefix}* 第 {blockIndex + 1} 块的行范围非法，需人工确认模板", retryable: false);

                    blocks.Add(tableRows.GetRange(start, end - start + 1));
                }

                // 校验每一块的标签行：表头行（重复标题行）与纵向合并都待业务确认，先阻断
                for (var blockIndex = 0; blockIndex < blocks.Count; blockIndex++)
                {
                    var tagRow = blocks[blockIndex][0];

                    // TODO: 业务确认后改为「克隆时移除 tblHeader」或「保留表头」。
                    if (tagRow.GetFirstChild<TableRowProperties>()?.GetFirstChild<TableHeader>() != null)
                        throw new DataSheetFillException(
                            $"{prefix}* 第 {blockIndex + 1} 块的标签行落在表头行(tblHeader)，扩容行为待人工确认",
                            retryable: false);

                    EnsureNoVerticalMerge(tagRow, prefix, blockIndex);
                }

                branches.Add(new AreaBranchLayout(prefix, areaCount, columnsPerBlock, blockHeight, blocks));
            }

            if (branches.Count == 0)
                throw new DataSheetFillException("模板存在 area 书签但编号不可解析，无法扩容", retryable: false);

            return new AreaLayoutProbe(structureTable, branches);
        }

        /// <summary>
        /// 收集指定前缀的全部 area 书签，并校验它们与 structure 容器属于同一张表格。
        /// </summary>
        /// <param name="body">文档主体。</param>
        /// <param name="prefix">书签前缀，如 <c>Sample_data_</c>。</param>
        /// <param name="structureTable">structure 容器表格。</param>
        /// <returns>该前缀的书签集合（可能为空，表示模板没有该组）。</returns>
        /// <exception cref="DataSheetFillException">书签不在表格内或与容器不是同一张表格时抛出。</exception>
        // TODO(封装): 抽入 AreaLayoutProbe。
        private static List<BookmarkStart> CollectAreaBookmarks(Body body, string prefix, Table structureTable)
        {
            var bookmarks = body.Descendants<BookmarkStart>()
                // 前缀 + 纯数字校验：避免将来出现 Sample_data_tpl 之类同前缀书签被误当成 area
                .Where(b => b.Name?.Value is { Length: > 0 } name && ParseAreaIndex(name, prefix) > 0)
                .ToList();

            foreach (var bookmark in bookmarks)
            {
                var owner = bookmark.Ancestors<Table>().FirstOrDefault();

                // 约定「一个 Table = 一个 structure」：落到别的表（含表格外）说明模板做错了
                if (owner == null || owner != structureTable)
                    throw new DataSheetFillException(
                        $"{bookmark.Name?.Value} 与 structure 容器不在同一张表格，模板不符合结构约定", retryable: false);
            }

            return bookmarks;
        }

        /// <summary>
        /// 从 <c>前缀 + 编号</c> 形式的书签名中解析编号。
        /// </summary>
        /// <param name="name">书签名，如 <c>Sample_data_3</c>。</param>
        /// <param name="prefix">书签前缀。</param>
        /// <returns>解析出的编号；前缀不匹配或编号非法时返回 0。</returns>
        private static int ParseAreaIndex(string name, string prefix)
        {
            if (!name.StartsWith(prefix, StringComparison.Ordinal)) return 0;

            return int.TryParse(name.AsSpan(prefix.Length), out var index) ? index : 0;
        }

        /// <summary>
        /// 按配置校验块的标签行是否存在纵向合并（vMerge continue）。
        /// 默认策略为阻断：克隆带纵向合并的行极易产出 Word 判定损坏的文档。
        /// </summary>
        /// <param name="row">块的标签行。</param>
        /// <param name="prefix">书签前缀（仅用于错误信息）。</param>
        /// <param name="blockIndex">块的序号（仅用于错误信息）。</param>
        /// <exception cref="DataSheetFillException">策略为 Reject 且存在纵向合并时抛出。</exception>
        private void EnsureNoVerticalMerge(TableRow row, string prefix, int blockIndex)
        {
            var hasContinue = row.Descendants<TableCell>()
                .Any(c => c.GetFirstChild<TableCellProperties>()?.GetFirstChild<VerticalMerge>() is { } vm
                          && vm.Val?.Value == MergedCellValues.Continue);

            if (!hasContinue) return;

            if (_expansionOptions.VerticalMerge == VerticalMergePolicy.Reject)
                throw new DataSheetFillException(
                    $"{prefix}* 第 {blockIndex + 1} 块的标签行存在纵向合并(vMerge continue)，需人工确认模板后再扩容",
                    retryable: false);

            // SplitOnClone 策略：由 CloneBlockAndRenumber 在克隆后清除 vMerge
        }

        /// <summary>
        /// 取全文档下一个可用的书签 Id（现有最大数值 + 1）。
        /// 新书签必须使用全新 Id，否则 BookmarkEnd 会与旧书签错配。
        /// </summary>
        /// <param name="body">文档主体。</param>
        /// <returns>可安全分配的书签 Id 起始值。</returns>
        private static int GetNextBookmarkId(Body body)
        {
            var max = 0;

            foreach (var bookmark in body.Descendants<BookmarkStart>())
            {
                if (int.TryParse(bookmark.Id?.Value, out var id) && id > max) max = id;
            }

            // BookmarkEnd 也要参与取最大值：若存在 Id 更大的孤立 BookmarkEnd，
            // 仅按 Start 的最大值分配新 Id 会撞车，导致 Start/End 配对错乱
            foreach (var bookmarkEnd in body.Descendants<BookmarkEnd>())
            {
                if (int.TryParse(bookmarkEnd.Id?.Value, out var id) && id > max) max = id;
            }

            return max + 1;
        }

        /// <summary>
        /// 克隆一整块（多行）作为新的 area 块，并按精确映射表重写块内书签名。
        /// </summary>
        /// <param name="templateBlock">作为形状模板的块（有序行序列，通常是模板的最后一块）。</param>
        /// <param name="nameMap">「旧书签名 -> 新书签名」精确映射；未命中的书签保持原名。</param>
        /// <param name="nextBookmarkId">下一个可用书签 Id，方法内自增。</param>
        /// <returns>按原顺序排列的克隆行，由调用方负责依次插入文档。</returns>
        // TODO(封装): 抽入 AreaExpander。
        private List<TableRow> CloneBlockAndRenumber(
            IReadOnlyList<TableRow> templateBlock,
            IReadOnlyDictionary<string, string> nameMap,
            ref int nextBookmarkId)
        {
            var clonedRows = new List<TableRow>(templateBlock.Count);

            foreach (var sourceRow in templateBlock)
            {
                var clone = (TableRow)sourceRow.CloneNode(true);

                // 旧 Id -> 新 Id 映射：BookmarkStart 与 BookmarkEnd 必须严格同 Id 配对，
                // 否则 ReplaceBookmarks 会走跨段落兜底分支，导致样式丢失与结构破坏
                var idMap = new Dictionary<string, string>(StringComparer.Ordinal);

                // 记录本次真正改过名的书签，后面只清这些书签覆盖的文本
                var renamedStarts = new List<BookmarkStart>();

                foreach (var start in clone.Descendants<BookmarkStart>().ToList())
                {
                    var name = start.Name?.Value;
                    if (string.IsNullOrEmpty(name)) continue;

                    // 精确按「旧书签名」匹配：同一行若含多个同前缀书签（如 Sample_data_3 与 Sample_data_4），
                    // 必须各自映射到不同新编号；按前缀统一改名会造成重名，填值时出现两个相同测点
                    if (!nameMap.TryGetValue(name, out var newName)) continue;

                    var newId = (nextBookmarkId++).ToString();
                    idMap[start.Id!.Value!] = newId;

                    start.Name = newName;
                    start.Id = newId;
                    renamedStarts.Add(start);
                }

                foreach (var end in clone.Descendants<BookmarkEnd>().ToList())
                {
                    if (end.Id?.Value != null && idMap.TryGetValue(end.Id.Value, out var newId))
                        end.Id = newId;
                }

                // 只清空「本次改名书签」覆盖范围内的 Run 文本；模板固定文字（Sample: / Original /
                // After Wash / (cm) / (%) / Warp1-3 / Weft1-3）必须原样保留，否则新块只剩空壳
                foreach (var start in renamedStarts)
                {
                    var bookmarkEnd = clone.Descendants<BookmarkEnd>().FirstOrDefault(e => e.Id == start.Id);
                    if (bookmarkEnd == null) continue;

                    var paragraph = start.Ancestors<Paragraph>().FirstOrDefault();
                    if (paragraph == null) continue;

                    var runsBetween = paragraph.Elements<Run>()
                        .SkipWhile(r => !r.Elements<BookmarkStart>().Any(b => b.Id == start.Id))
                        .TakeWhile(r => !r.Elements<BookmarkEnd>().Any(e => e.Id == bookmarkEnd.Id))
                        .ToList();

                    foreach (var run in runsBetween)
                        foreach (var text in run.Descendants<Text>())
                            text.Text = string.Empty;
                }

                // SplitOnClone：新增区独立成格，避免继承纵向合并后被 Word 判定文档损坏
                if (_expansionOptions.VerticalMerge == VerticalMergePolicy.SplitOnClone)
                {
                    foreach (var verticalMerge in clone.Descendants<VerticalMerge>().ToList())
                        verticalMerge.Remove();
                }

                clonedRows.Add(clone);
            }

            return clonedRows;
        }

        /// <summary>
        /// 策略 B：按扩容后的行数估算，超过阈值时在 structure 表之前插入分页符，让整表从新页开始。
        /// OpenXML 无法获取真实页面布局（剩余空间与行高），因此阈值 RowsPerPage 属估算值，
        /// 需按实际模板实测后调整。
        /// </summary>
        /// <param name="table">structure 容器表格。</param>
        private void InsertPageBreakIfOverflow(Table table)
        {
            if (!_expansionOptions.InsertPageBreakWhenOverflow) return;

            var totalRows = table.Elements<TableRow>().Count();
            if (totalRows <= _expansionOptions.RowsPerPage) return;

            // 用 PageBreakBefore 而不是插入一个空行 + 分页符，避免多出一个空段落占位
            var paragraph = new Paragraph(
                new ParagraphProperties(new PageBreakBefore()));

            table.InsertBeforeSelf(paragraph);
        }

        /// <summary>
        /// 尽力删除半成品文件。删除失败不掩盖原始异常。
        /// </summary>
        /// <param name="path">待删除的文件路径。</param>
        private static void TryDeleteQuietly(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // 删除失败时保留原异常信息优先抛出，这里不再包装
            }
        }

        /// <summary>
        /// 构建书签映射字典
        /// </summary>
        private Dictionary<string, string> BuildBookmarkValues(DataSheetModel model)
        {
            // 与 WordTemplateValidator 的 OrdinalIgnoreCase 保持一致：
            // 校验阶段忽略大小写, 填充阶段若区分大小写会导致"校验通过但运行时静默不填"
            var bookmarkValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ReportNumber"] = model.ReportNumber,
                ["TestMethod"] = model.TestMethod,
                ["TestCondition"] = model.TestCondition
            };

            // 添加 SampleMap 的映射
            if (model.SampleMap?.SampleMetaData != null)
            {
                foreach (var sample in model.SampleMap.SampleMetaData)
                {
                    bookmarkValues[sample.Key] = sample.Value;
                }
            }

            if (model.AfterWashMap?.AfterWashMetaData != null) 
            {
                foreach (var afterWash in model.AfterWashMap.AfterWashMetaData) 
                {
                    bookmarkValues[afterWash.Key] = afterWash.Value;
                }
            }

            return bookmarkValues;
        }

        /// <summary>
        /// 替换文档中的所有书签
        /// </summary>
        private void ReplaceBookmarks(MainDocumentPart mainPart, Dictionary<string, string> bookmarkValues)
        {
            var bookmarks = mainPart.Document.Body!.Descendants<BookmarkStart>()
                .Where(b => bookmarkValues.ContainsKey(b.Name!)).ToList();

            foreach (var bookmark in bookmarks)
            {
                var bookmarkEnd = mainPart.Document.Body.Descendants<BookmarkEnd>()
                    .FirstOrDefault(e => e.Id == bookmark.Id);

                if (bookmarkEnd == null) continue;

                var paragraph = bookmark.Ancestors<Paragraph>().FirstOrDefault();
                if (paragraph == null) continue;

                var runsBetween = paragraph.Elements<Run>()
                    .SkipWhile(r => !r.Elements<BookmarkStart>().Any(b => b.Id == bookmark.Id))
                    .TakeWhile(r => !r.Elements<BookmarkEnd>().Any(e => e.Id == bookmarkEnd.Id))
                    .ToList();

                var targetValue = bookmarkValues[bookmark.Name!];
                ReplaceBookmarkContent(bookmark, bookmarkEnd, runsBetween, targetValue);
            }
        }

        /// <summary>
        /// 替换单个书签的内容
        /// </summary>
        private void ReplaceBookmarkContent(BookmarkStart bookmark, BookmarkEnd bookmarkEnd,
            List<Run> runsBetween, string targetValue)
        {
            if (runsBetween.Any())
            {
                // 保留第一个Run的格式，替换其文本
                var firstRun = runsBetween.First();
                firstRun.RemoveAllChildren<Text>();
                firstRun.Append(new Text(targetValue) { Space = SpaceProcessingModeValues.Preserve });

                // 移除范围内多余的Run
                foreach (var run in runsBetween.Skip(1))
                {
                    run.Remove();
                }
            }
            else
            {
                // 若书签内无内容，在 BookmarkStart 后插入新 Run
                var newRun = new Run(new Text(targetValue) { Space = SpaceProcessingModeValues.Preserve });
                bookmark.Parent!.InsertAfter(newRun, bookmark);
            }
        }

        /// <summary>
        /// structure 容器的布局探测结果。
        /// </summary>
        /// <param name="StructureTable">承载 sample area 的表格（一个 Table 即一个 structure）。</param>
        /// <param name="Branches">各书签前缀分支的块级布局信息。</param>
        // TODO(封装): 抽为独立类型 AreaLayoutProbe。
        private sealed record AreaLayoutProbe(Table StructureTable, List<AreaBranchLayout> Branches);

        /// <summary>
        /// 单个书签前缀分支（如 Sample_data_）的块级布局信息。
        /// </summary>
        /// <param name="Prefix">书签前缀。</param>
        /// <param name="AreaCount">模板现有 area 总数 A（取自实际书签最大编号）。</param>
        /// <param name="ColumnsPerBlock">每块的 area 列数 cols = A / 块数。</param>
        /// <param name="BlockHeight">一块占用的表格行数（含无书签的数据行，如 Warp/Weft）。</param>
        /// <param name="Blocks">每个块的行序列（有序）；最后一块用作克隆的形状模板。</param>
        // TODO(封装): 抽为独立类型 AreaBranchLayout。
        private sealed record AreaBranchLayout(
            string Prefix,
            int AreaCount,
            int ColumnsPerBlock,
            int BlockHeight,
            IReadOnlyList<IReadOnlyList<TableRow>> Blocks);
    }
}
