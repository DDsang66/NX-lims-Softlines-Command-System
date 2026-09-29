using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.DataSheetConetxt;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;
using NX_lims_Softlines_Command_System.src.Infrastructure.TemplateEngine.WordTemplateAdapter;
using DataSheet = NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetContext.DataSheet;
using DataSheetBatch = NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetBatchContext.DataSheetBatch;

namespace NX_lims_Softlines_Command_System.src.Application.Service.DataSheetContext
{
    public class DataSheetService : IScopedDependency
    {
        private readonly ICheckListRepository _checkListRepository;
        private readonly IConditionPoolRepository _conditionPoolRepository;
        private readonly IDataSheetModelGenerator _dataSheetModelGenerator;
        private readonly IFileStorageService _fileStorageService;
        private readonly DataSheetFillingEngine _dataSheetFillingEngine;
        private readonly ILogger<DataSheetService> _logger;
        private readonly IDataSheetBatchRepository _dataSheetBatchRepository;
        private readonly IDataSheetRepository _dataSheetRepository;
        private readonly IWebHostEnvironment _env;
        private readonly IUnitOfWork _unitOfWork;

        public DataSheetService(
            IUnitOfWork unitOfWork,
            ICheckListRepository checkListRepository,
            IConditionPoolRepository conditionPoolRepository,
            DataSheetFillingEngine dataSheetFillingEngine,
            ILogger<DataSheetService> logger,
            IDataSheetRepository dataSheetRepository,
            IWebHostEnvironment env,
            IFileStorageService fileStorageService,
            IDataSheetModelGenerator dataSheetModelGenerator,
            IDataSheetBatchRepository dataSheetBatchRepository)
        {
            _unitOfWork = unitOfWork;
            _env = env;
            _logger = logger;
            _fileStorageService = fileStorageService;
            _checkListRepository = checkListRepository;
            _conditionPoolRepository = conditionPoolRepository;
            _dataSheetFillingEngine = dataSheetFillingEngine;
            _dataSheetRepository = dataSheetRepository;
            _dataSheetModelGenerator = dataSheetModelGenerator;
            _dataSheetBatchRepository = dataSheetBatchRepository;
        }

        #region abandoned
        ///// <summary>
        ///// 彻底跑通新代码前先保留
        ///// </summary>
        ///// <param name="dto"></param>
        ///// <param name="ct"></param>
        ///// <returns></returns>
        ///// <exception cref="ArgumentException"></exception>
        //public async Task<Result> GenerateTaskStart1(DataSheetGenerateDto dto,CancellationToken ct) 
        //{
        //    //拿到checklistid

        //    var checklist = await  _checkListRepository.GetByIdAsync(new CheckListId(dto.CheckListId),ct);

        //    if (checklist.Status != CheckListStatus.InProgress)
        //        throw new ArgumentException("CheckList is not completed yet");

        //    var pools =  await  _conditionPoolRepository.GetByCheckListIdAsync(new CheckListId(dto.CheckListId),ct);

        //    //幂等验证，如果已经有checklist的批次正在生成或者说该批次的ds已经生成过了，就不再重复生成;

        //    foreach (var item in checklist.Items) 
        //    {
        //        //按照每个项目创建测试ds任务

        //        //模板查询器 (领域服务) 查询到对应模板
        //        var contactTemplateUrl = "DocxModel/Common_WET/WET_Dimensional_Change_Wasing.docx";

        //        //var ds = DataSheet.Create(
        //        //    checklist.Id,
        //        //    null,
        //        //    contactTemplateUrl,
        //        //    DataSheetStatus.Pending,
        //        //    checklist.OderId!,
        //        //    item.TestItemId!);

        //        // await _dataSheetRepository.AddAsync(ds,ct);
        //    }

        //    //暂时：手动触发processHub监听，实时将生成进度推送到前端

        //    await _unitOfWork.SaveChangesAsync(ct);

        //    //以下代码交由eventHandler处理，现在为了验证流程可行性先显示写在这里

        //    foreach (var item in checklist.Items) 
        //    {
        //        //开始调用DataSheetGenerator生成model，在应用层进行用例编排
        //        var models = await _dataSheetModelGenerator.GenerateAsync(pools.ToList(), item,ct);

        //        //调用模板引擎将model数据填充进入到模板中，生成最终的ds文件
        //       _dataSheetFillingEngine.FillDataSheet(models.First());
        //    }

        //    return Result.Ok();
        //}
        #endregion

        /// <summary>
        /// 生成任务启动
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<Result<GenerateDataSheetTaskResult>> GenerateTaskStart(DataSheetGenerateDto dto, CancellationToken ct)
        {
            var checkListId = new CheckListId(dto.CheckListId);

            // 1. 校验 checklist
            var checklist = await _checkListRepository.GetByIdAsync(checkListId, ct)
                ?? throw new ArgumentException("CheckList not found");

            if (checklist.Status != CheckListStatus.InProgress)
                throw new ArgumentException("CheckList is not completed yet");

            // 2. 幂等：是否已有未失败的批次
            if (!dto.ForceRegenerate)
            {
                var existingBatch = await _dataSheetBatchRepository
                    .GetActiveByCheckListIdAsync(checkListId, ct);
                if (existingBatch != null)
                {
                    return Result<GenerateDataSheetTaskResult>.Ok(new GenerateDataSheetTaskResult(
                        existingBatch.Id.Value,
                        checklist.Id.Value,
                        existingBatch.Total));
                }
            }

            // 3. 模板查询（领域服务）—— 先固定，后续替换成真正的查询
            var contactTemplateUrl = "DocxModel/Common_WET/WET_Dimensional_Change_Wasing.docx";

            // 4. 创建批次
            var batch = DataSheetBatch.Create(
                checkListId,
                checklist.OderId!,
                checklist.Items.Count,
                contactTemplateUrl);

            await _dataSheetBatchRepository.AddAsync(batch, ct);

            // 5. 按项目拆 N 个初始占位 PENDING datasheet
            foreach (var item in checklist.Items)
            {
                var ds = DataSheet.Create(
                    checklist.Id,
                    null,
                    contactTemplateUrl,
                    DataSheetStatus.Pending,
                    checklist.OderId!,
                    item.TestItemId!,
                    batch.Id,
                    0);

                await _dataSheetRepository.AddAsync(ds, ct);
            }

            await _unitOfWork.SaveChangesAsync(ct);

            // ★ 不再在这里生成、不再推送
            return Result<GenerateDataSheetTaskResult>.Ok(new GenerateDataSheetTaskResult(
                batch.Id.Value,
                checklist.Id.Value,
                checklist.Items.Count));
        }

        /// <summary>
        /// 单条重试：把失败的 datasheet 重置为 Pending
        /// </summary>
        public async Task<Result> RetryAsync(Guid dataSheetId, CancellationToken ct)
        {
            var ds = await _dataSheetRepository.GetByIdAsync(new DataSheetId(dataSheetId), ct);
            if (ds == null)
                return Result.Fail("Datasheet 不存在");

            if (ds.Status != DataSheetStatus.Failed)
                return Result.Fail($"只有失败的任务才能重试，当前状态: {ds.Status}");

            try
            {
                ds.MarkPending();
            }
            catch (InvalidOperationException ex)
            {
                return Result.Fail(ex.Message);
            }

            await _dataSheetRepository.UpdateAsync(ds, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return Result.Ok();
        }

        /// <summary>
        /// 批量重试：把失败的 datasheet 重置为 Pending
        /// </summary>
        /// <param name="checkListId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result> RetryBatchAsync(CheckListId checkListId, CancellationToken ct)
        {
            await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var affected = await _dataSheetRepository.BulkMarkPendingAsync(checkListId, ct);

                if (affected == 0)
                {
                    await transaction.RollbackAsync(ct);
                    return Result.Fail("该 checklist 下没有失败的任务");
                }

                await transaction.CommitAsync(ct);
                return Result.Ok();
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        /// <summary>
        /// 接收回调
        /// </summary>
        /// <returns></returns>
        public async Task SaveFromOnlyOffice(string datasheetId, string url, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(datasheetId))
                throw new ArgumentException("datasheetId 不能为空", nameof(datasheetId));

            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("ONLYOFFICE url 不能为空", nameof(url));

            // 1. 查 datasheet 记录
            var datasheet = await _dataSheetRepository.GetByIdAsync(new DataSheetId(Guid.Parse(datasheetId)), ct)
                ?? throw new InvalidOperationException($"Datasheet {datasheetId} 不存在");

            if (string.IsNullOrWhiteSpace(datasheet.Url))
                throw new InvalidOperationException($"Datasheet {datasheetId} 没有 Url 字段");

            //报错代码：后续用其他方式幂等
            //// ★ 2. 幂等：同一个 callback url 重复推送直接跳过
            //if (!string.IsNullOrEmpty(datasheet.LastCallbackUrl)
            //    && datasheet.LastCallbackUrl == url)
            //{
            //    _logger.LogInformation(
            //        "重复的 ONLYOFFICE 回调，跳过。datasheetId={DatasheetId}, url={Url}",
            //        datasheetId, url);
            //    return;
            //}

            // 3. 从 ONLYOFFICE 下载最新 docx
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(2);

            byte[] fileBytes;
            try
            {
                fileBytes = await httpClient.GetByteArrayAsync(url, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从 ONLYOFFICE 下载文件失败: {Url}", url);
                throw;
            }

            if (fileBytes == null || fileBytes.Length == 0)
                throw new InvalidOperationException("下载的文件为空");

            // 4. 拼出原文件物理路径
            var relativePath = datasheet.Url
                .Replace('\\', '/')
                .TrimStart('/');

            if (relativePath.Contains(".."))
                throw new InvalidOperationException("非法的文件路径");

            var physicalPath = Path.Combine(
                _env.WebRootPath,
                relativePath.Replace('/', Path.DirectorySeparatorChar)
            );

            // 5. 覆盖写入原文件
            var directory = Path.GetDirectoryName(physicalPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllBytesAsync(physicalPath, fileBytes, ct);

            // ★ 6. 写盘成功后才更新元数据（顺序不能颠倒）
            datasheet.MarkSaved(url);   // 保留你原有的领域方法

            await _dataSheetRepository.UpdateAsync(datasheet, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation(
                "ONLYOFFICE 保存成功: datasheetId={DatasheetId}, 路径={Path}, 大小={Size} bytes, 版本={Version}, 时间={Time}",
                datasheetId, physicalPath, fileBytes.Length, datasheet.EditorVersion, datasheet.UpdateTime);
        }
    }
}
