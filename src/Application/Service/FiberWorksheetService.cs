using Mapster;
using NX_lims_Softlines_Command_System.Application.Services.AuthenticationService;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.FiberContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using System.Text.Json;
using NX_lims_Softlines_Command_System.src.Application.Interface.OrderContext;
using NX_lims_Softlines_Command_System.src.Application.Interface.FiberTeamContext;
using NX_lims_Softlines_Command_System.src.Application.Interface;

namespace NX_lims_Softlines_Command_System.src.Application.Service
{
    /// <summary>
    /// 纤维工作表服务
    /// </summary>
    public class FiberWorksheetService : IScopedDependency
    {
        /// <summary>
        /// Method 下拉的候选标准。原先是 FiberWorkSheet.vue 里的 10 条硬编码。
        ///
        /// **刻意留在 Application 层、不进 <see cref="FiberOptions"/>**：那几张表是"纤维属于哪个分组"的
        /// 领域知识，这一串是"界面选哪些标准"的录入项 —— 混进 Domain 等于把展示口径写进领域规则。
        ///
        /// 每条 **value 与 label 相同**（前端就按这个约定渲染），故这里是纯字符串列表。
        /// 两条 AATCC **复合方法名**照原样保留（TM20与 TM20A是一套），
        /// 空格数也照抄 —— 后端多值拆链只按逗号切、不按空格切，正是为了不切坏它们。
        /// </summary>
        private static readonly IReadOnlyList<string> MethodOptions = new[]
        {
            "Regulation (Eu) No. 1007/2011",
            "AATCC TM20-2021",
            "AATCC TM20-2021  AATCC TM20A-2025",
            "ISO1833",
            "DIN EN ISO 1833",
            "FZ/T 01057.1-4–2007",
            "AATCC TM20-2021 AATCC TM20A-2025 (Korea)",
            "CAN/CGSB-4.2 No.14-2005",
            "CNS 2339-1:2013 CNS 2339-2:2013",
            "JIS L1030-1:2012 JIS L1030-2:2012",
        };

        private readonly IFiberWorksheetRepository _worksheetRepo;
        private readonly IFiberDatabaseRepository _fiberDatabaseRepo;
        private readonly IWordTemplateAdapter _wordTemplateAdapter;
        private readonly IWordTemplateEngine _wordTemplateEngine;
        private readonly IFileStorageService _fileStorage;
        private readonly ILabelOptionRepository _labelOptionRepo;
        private readonly IDocxMerger _docxMerger;

        public FiberWorksheetService(
            IFiberWorksheetRepository worksheetRepo,
            IFiberDatabaseRepository fiberDatabaseRepo,
            IWordTemplateAdapter wordTemplateAdapter,
            IWordTemplateEngine wordTemplateEngine,
            IFileStorageService fileStorage,
            ILabelOptionRepository labelOptionRepo,
            IDocxMerger docxMerger)
        {
            _worksheetRepo = worksheetRepo;
            _fiberDatabaseRepo = fiberDatabaseRepo;
            _wordTemplateAdapter = wordTemplateAdapter;
            _wordTemplateEngine = wordTemplateEngine;
            _fileStorage = fileStorage;
            _labelOptionRepo = labelOptionRepo;
            _docxMerger = docxMerger;
        }

        public async Task<object> GetLabelOptionsAsync(CancellationToken ct)
        {
            var options = await _labelOptionRepo.GetLabelOptionsAsync(ct);
            var resultRemarkList = options.Where(o => o.Category == "ResultRemark").Select(o => o.Text).ToList();
            return new
            {
                success = true,
                data = new
                {
                    judgmentLabelOptions = options.Where(o => o.Category == "Judgment").Select(o => o.Text).ToList(),
                    languageLabelOptions = options.Where(o => o.Category == "Language").Select(o => o.Text).ToList(),
                    resultRemarkOptions = resultRemarkList,
                    labelRemarkOptions = resultRemarkList
                }
            };
        }

        public async Task<object> GetAllFibersAsync()
            => new { success = true, data = await _fiberDatabaseRepo.GetAllAsync() };

        /// <summary>
        /// 纤维模块录入界面的候选清单。
        ///
        /// **不含全量成分名单**：那份仍由既有的 /render/compositionsearch 提供
        /// （它还有 FiberContentBoundSingle.vue 另一个消费者）。同一份名单开两个出处迟早会漂，
        /// 故这里只补前端原先**硬编码**的那几组。
        ///
        /// 分组清单来自 <see cref="FiberOptions"/>（Domain，与 <see cref="FiberTokens"/> 同层）；
        /// 标准清单是本类的私有常量 —— 理由见 <see cref="MethodOptions"/>。
        ///
        /// 无需 await：全是常量与静态表，没有任何 I/O。返回类型保持与
        /// <see cref="GetFiberNamesAsync"/> 一致，控制器侧写法才不用分叉。
        /// </summary>
        public Task<object> GetFiberOptionsAsync()
        {
            object payload = new
            {
                success = true,
                data = new
                {
                    cellulosicSub = FiberOptions.CellulosicSub,
                    regeneratedSub = FiberOptions.RegeneratedSub,
                    bicomponentSub = FiberOptions.BicomponentSub,
                    methodOptions = MethodOptions,
                }
            };
            return Task.FromResult(payload);
        }

        public async Task<object> GetFiberNamesAsync()
            => new { success = true, data = await _fiberDatabaseRepo.GetAllNamesAsync() };

        public async Task<object> AddFiberAsync(FiberDatabaseCreateDto dto)
        {
            var entity = new CompositionNew
            {
                CompositionNameEn = dto.FiberNameEn,
                CompositionNameChn = dto.FiberNameCn,
                PrimaryCategoryEn = dto.Category
            };
            var result = await _fiberDatabaseRepo.AddAsync(entity);
            return new { success = true, data = result };
        }

        public async Task<object> UpdateFiberAsync(Guid id, FiberDatabaseCreateDto dto)
        {
            var fiber = await _fiberDatabaseRepo.GetByIdAsync(id);
            if (fiber == null) return new { success = false, message = "纤维数据不存在" };
            fiber.CompositionNameEn = dto.FiberNameEn;
            fiber.CompositionNameChn = dto.FiberNameCn;
            fiber.PrimaryCategoryEn = dto.Category;
            var result = await _fiberDatabaseRepo.UpdateAsync(fiber);
            return new { success = true, data = result };
        }

        public async Task<object> DeleteFiberAsync(Guid id)
        {
            var result = await _fiberDatabaseRepo.DeleteAsync(id);
            return new { success = result };
        }

        public async Task<object> GetWorkSheetAsync(string reportNumber)
        {
            var result = await _worksheetRepo.GetByReportNumberAsync(reportNumber);
            if (result == null) return new { success = false, message = "工作表不存在" };
            return new { success = true, data = result };
        }

        public async Task<object> DeleteWorksheetAsync(Guid id)
        {
            var result = await _worksheetRepo.DeleteAsync(id);
            return new { success = result };
        }

        /// <summary>
        /// 构建成分分析报告服务
        /// </summary>
        /// <returns></returns>0
        public async Task<Result<string>> BuildAnalysisAsync(BuildAnalysisDto dto,CancellationToken ct)
        {

            //需要重新进行用例编排
            //实例化AnalysisWorksheet生成word文件
            //实例化IngredientAnalysis进行计算
            //结果回传AnalysisWorksheet进行文档填写

            var entity = dto.Adapt<FiberAnalysis>();

            var snowflake = new SnowflakeIdGenerator();

            entity.Id = snowflake.NextId();

            await _worksheetRepo.AddAsync(entity, ct);

            var po = await _worksheetRepo.GetByIdAsync(entity.Id, ct);

            if (po == null) return Result<string>.Fail("data is not found");

            // 目标文件路径提到 try 之外：失败时要能删掉半成品（见 catch）
            string? targetPath = null;

            //执行计算
            try
            {
                // Adapt（构造聚合根）原先写在 try 之外，于是"空 methods / 空 components"
                // 这类记录会直接抛出去、走不到下面的补偿分支。现在连同回潮率取值一起放进 try。
                var ingredientsAnalysis = await BuildAndCalculateAsync(po);
                //计算失败触发补偿机制

                //执行生成word
                string targetFileName = $"{ingredientsAnalysis.ReportNo}_{DateTime.Now:yyMMddHHmmss}_FiberAnalysis.docx";
                targetPath = _fileStorage.CopyTemplate(
                    Path.Combine("DocxModel", "FIBER_ANALYSIS_DATA_SHEET.docx"),
                    Path.Combine("DocxModel", "SaveDocx"),
                    targetFileName);

                // 显微镜图片的纤维名单与标准无关（Components 在构造时就定了，
                // 逐标准重算只产出 AnalysisResult），故只收集一次、每份复用。
                var microscopeFibers = CollectMicroscopeFiberNames(ingredientsAnalysis.Components);

                // 第 1 份渲进基底文件 —— **单标准记录到此为止**，走的就是单标准那条老路径。
                RenderOne(targetPath, ingredientsAnalysis.StandardResults[0], microscopeFibers);

                // 多标准 → 第 2..N 份各渲一份中间产物，再并进上面那一份。
                // 零回归保证：StandardResults.Count == 1 时**不经过合并器**。
                if (ingredientsAnalysis.StandardResults.Count > 1)
                    MergeStandardReports(targetPath, ingredientsAnalysis.StandardResults.Skip(1).ToList(), microscopeFibers);

                //ingredientsAnalysis.WorkSheetGenerator(filePath);
                //执行保存

                //IFiberReposity 查询Word地址与生成状态；返回url

                return Result<string>.Ok(targetFileName);
            }
            catch (Exception ex)
            {
                // 半成品不能留在可下载目录里。合并是**原地改写**，中途抛异常会留下半份文件，
                // 而它在 wwwroot/DocxModel/SaveDocx/ 下、按文件名是取得到的。
                // （顺带也覆盖了改动前就有的情形：ReplaceText 抛异常时同样会剩一份没填完的模板副本。）
                TryDelete(targetPath);

                //记录日志
                //触发补偿机制
                return Result<string>.Fail(ex.Message);
            }
        }

        /// <summary>
        /// 把一份 <see cref="AnalysisResult"/> 渲进指定的 docx —— Adapt + ReplaceText
        /// + InsertMicroscopeImages，即之前 <see cref="BuildAnalysisAsync"/> 里那三行的原样。
        /// </summary>
        /// <remarks>
        /// 现在不动这三步中的任何一步：多标准是"同一套渲染跑 N 遍"，不是"改渲染"。
        /// </remarks>
        private void RenderOne(string targetPath, AnalysisResult result, IReadOnlyList<string> microscopeFibers)
        {
            var (values, redBookmarks, removeWhenEmpty) = _wordTemplateAdapter.Adapt(result);

            _wordTemplateEngine.ReplaceText(targetPath, values, redBookmarks, removeWhenEmpty);

            var imageFolder = Path.Combine("wwwroot", "MicroscopeImages");
            _wordTemplateEngine.InsertMicroscopeImages(targetPath, microscopeFibers, imageFolder);
        }

        /// <summary>
        /// 收集要插显微镜图的纤维名 —— 多组分展开所有溶解单元。
        /// </summary>
        private static IReadOnlyList<string> CollectMicroscopeFiberNames(IEnumerable<FiberComponent> components)
            => components
                .SelectMany(c => c is DissolvedFiberComponent d
                    ? d.DissolutionUnits.Select(u => u.FiberName)
                    : new[] { c.FiberName })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        /// <summary>
        /// 把第 2..N 份标准各渲一份中间产物，再合并进 <paramref name="basePath"/>（原地改写）。
        /// </summary>
        /// <remarks>
        /// **中间产物落 %TEMP%，绝不落 wwwroot** —— 后者是可下载目录，
        /// 中间产物进去就等于对外可见（且会与正式产物抢清理）。故用
        /// <see cref="IFileStorageService.CopyTemplateTo"/> 而非 CopyTemplate：
        /// 后者把源与目标都钉死在 WebRootPath 下。
        /// 目录名带 guid：并发请求各用各的，不会互相覆盖 / 互删。
        /// </remarks>
        private void MergeStandardReports(
            string basePath, IReadOnlyList<AnalysisResult> rest, IReadOnlyList<string> microscopeFibers)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "nxlims-docx-merge", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                var sources = new List<string>();
                for (int i = 0; i < rest.Count; i++)
                {
                    string path = Path.Combine(tempDir, $"section{i + 2}.docx");
                    _fileStorage.CopyTemplateTo(
                        Path.Combine("DocxModel", "FIBER_ANALYSIS_DATA_SHEET.docx"), path);
                    RenderOne(path, rest[i], microscopeFibers);
                    sources.Add(path);
                }

                _docxMerger.MergeInto(basePath, sources);
            }
            finally
            {
                // 合并器已把内容读进内存并写回基底，中间产物使命已完。
                // 清理失败**不该让整份报告失败**：它落在 %TEMP%，且目录名带 guid 不会撞。
                try { Directory.Delete(tempDir, true); } catch { /* 留给系统清理 */ }
            }
        }

        /// <summary>删掉半成品。失败不吞掉调用方正在处理的异常。</summary>
        private static void TryDelete(string? path)
        {
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // 删不掉也只是留个垃圾文件；真正的失败原因在调用方那个 ex 里，不能被这里盖住
            }
        }


        /// <summary>
        /// 取回潮率列链 → 构造聚合根 → 执行计算。
        /// </summary>
        /// <remarks>
        /// 三条路径共用：新建（<see cref="BuildAnalysisAsync"/>）、按 ID 重算（<see cref="CalculateAsync"/>）、
        /// 按报告号重算（<see cref="CalculateByReportAsync"/>）。
        /// 后两条原先直接调 Calculate()（无参），回潮率 map 为空 —— 计算出的回潮率恒为 0。
        /// 不接 CancellationToken：<see cref="IFiberDatabaseRepository.GetMoistureRegainMapAsync"/>
        /// 本身不接受 ct，且 <see cref="CalculateByReportAsync"/> 的调用点也没有 ct 可传；加一个用不上的参数只是噪声。
        /// **不在此处吞异常**：三条路径的失败语义各不相同（一条返回 Fail 结果、两条让异常上抛），
        /// 由调用方各自决定。
        /// </remarks>
        private async Task<IngredientAnalysisCalculation> BuildAndCalculateAsync(FiberAnalysis po)
        {
            //Mapster封装映射，内部使用工厂模式构建IngredientAnalysis对象
            var ingredientsAnalysis = po.Adapt<IngredientAnalysisCalculation>();

            // 回潮率查询：**每个标准各查一次**。
            //
            // 单标准记录此处就是查一次、实参 = Methods[0]，
            // 与改动前逐字等价，故 WorksheetServiceMoistureRegainTests 原样通过。
            // Methods 为空时 StandardsToRender 退化成单个空标准 → 仍查一次空串，
            // 保住"空 method 记录今天也会查一次 Iso 列"的行为。
            var mrByStandard = new Dictionary<string, IReadOnlyDictionary<string, decimal>>();
            foreach (var standard in ingredientsAnalysis.StandardsToRender)
            {
                // 重复标准串只查一次。前端 el-select multiple 本身不允许选重复值，生产走不到这里；
                // 只是不为重复值多打一次库（少发明规则，不做去重、该出两份还是出两份）。
                if (mrByStandard.ContainsKey(standard)) continue;

                mrByStandard[standard] = await _fiberDatabaseRepo.GetMoistureRegainMapAsync(standard);
            }

            ingredientsAnalysis.CalculatePerStandard(mrByStandard);
            return ingredientsAnalysis;
        }

        /// <summary>
        /// 计算用户的输入值（单项分析）并返回分析结果（不保存数据，仅供前端展示）
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<string>> InputValueCalculateAsync(long id, CancellationToken ct)
        {
            return Result<string>.Ok(string.Empty);
        }

        /// <summary>
        /// 创建分析记录（只保存原始数据）
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<long>> CreateAsync(BuildAnalysisDto dto, CancellationToken ct)
        {
            long id = 1;
            return Result<long>.Ok(id);
        }

        /// <summary>
        /// 按 ID 计算成分分析结果
        /// </summary>
        public async Task<Result<AnalysisResult>> CalculateAsync(long id, CancellationToken ct)
        {
            var entity = await _worksheetRepo.GetByIdAsync(id, ct);
            if (entity == null)
                return Result<AnalysisResult>.Fail("分析记录不存在", "NOT_FOUND");

            // 原先直接 Calculate()，回潮率 map 为空 → 回潮率恒为 0
            var ingredientsAnalysis = await BuildAndCalculateAsync(entity);

            return Result<AnalysisResult>.Ok(ingredientsAnalysis.Result);
        }

        /// <summary>
        /// 按报告号计算（先查 ID 再计算）
        /// </summary>
        public async Task<Result<FiberCalculationResultDto>> CalculateByReportAsync(string reportNumber)
        {
            var entity = await _worksheetRepo.GetByReportNumberAsync(reportNumber);
            if (entity == null)
                return Result<FiberCalculationResultDto>.Fail("工作表不存在", "NOT_FOUND");

            // 同 CalculateAsync：原先漏传回潮率 map
            var ingredientsAnalysis = await BuildAndCalculateAsync(entity);
            var calcResult = ingredientsAnalysis.Result;

            return Result<FiberCalculationResultDto>.Ok(new FiberCalculationResultDto
            {
                RecommendedLabel = calcResult.RecommendedLabelString,
                Items = calcResult.Recommendation?
                    .Select(r => new FiberCalculationItemResultDto { Composition = r })
                    .ToList() ?? new()
            });
        }

        /// <summary>
        /// 纯计算（不持久化）
        /// </summary>
        public async Task<Result<FiberCalculationResultDto>> DirectCalculateAsync(FiberCalculationRequestDto request)
        {
            return Result<FiberCalculationResultDto>.Fail("请先保存工作单后执行计算", "NOT_IMPLEMENTED");
        }

        /// <summary>
        /// 生成 Word（依赖计算）
        /// </summary>
        public async Task<Result<string>> GenerateWordAsync(long id, CancellationToken ct)
        {
            var entity = await _worksheetRepo.GetByIdAsync(id, ct);
            if (entity == null)
                return Result<string>.Fail("分析记录不存在", "NOT_FOUND");

            var targetPath = Path.Combine("wwwroot", "DocxModel", "SaveDocx", $"{entity.ReportNumber}_FiberAnalysis.docx");
            return Result<string>.Ok(targetPath);
        }

        /// <summary>
    }
}
