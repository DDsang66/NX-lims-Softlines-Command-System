using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;
using NX_lims_Softlines_Command_System.src.Infrastructure.TemplateEngine.WordTemplateAdapter;
using System.Text.Json;

namespace NX_lims_Softlines_Command_System.src.Application.Service.DataSheetContext
{
    public class DataSheetGenerateWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DataSheetGenerateWorker> _logger;

        public DataSheetGenerateWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<DataSheetGenerateWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        /// <summary>
        /// 启动生成进程
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            _logger.LogInformation("DataSheetGenerateWorker started");

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var sp = scope.ServiceProvider;

                    var dsRepo = sp.GetRequiredService<IDataSheetRepository>();
                    var modelGenerator = sp.GetRequiredService<IDataSheetModelGenerator>();
                    var fillingEngine = sp.GetRequiredService<DataSheetFillingEngine>();
                    var poolRepo = sp.GetRequiredService<IConditionPoolRepository>();
                    var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
                    var checklistRepo = sp.GetRequiredService<ICheckListRepository>();

                    // 1. 拿一批 PENDING
                    var pending = await dsRepo.GetPendingAsync(batchSize: 20, ct);
                    if (pending.Count == 0)
                    {
                        await Task.Delay(1000, ct);
                        continue;
                    }

                    // 2. 按 checkListId 分组预取 pools
                    var poolsByCheckList = new Dictionary<Guid, List<ConditionPool>>();
                    foreach (var g in pending.GroupBy(d => d.CheckListId.Value))
                    {
                        var pools = await poolRepo.GetByCheckListIdAsync(
                            new CheckListId(g.Key), ct);
                        poolsByCheckList[g.Key] = pools.ToList();
                    }

                    // 3. 并行处理：每个任务独立 scope，避免 DbContext 并发访问
                    var pendingIds = pending.Select(d => d.Id.Value).ToList();

                    await Parallel.ForEachAsync(pendingIds, ct, async (dsId, token) =>
                    {
                        using var innerScope = _scopeFactory.CreateScope();
                        var innerSp = innerScope.ServiceProvider;

                        var innerDsRepo = innerSp.GetRequiredService<IDataSheetRepository>();
                        var innerModelGenerator = innerSp.GetRequiredService<IDataSheetModelGenerator>();
                        var innerFillingEngine = innerSp.GetRequiredService<DataSheetFillingEngine>();
                        var innerChecklistRepo = innerSp.GetRequiredService<ICheckListRepository>();
                        var innerUnitOfWork = innerSp.GetRequiredService<IUnitOfWork>();

                        // 在内层 scope 重新加载 ds，避免跨 scope 操作实体
                        var ds = await innerDsRepo.GetByIdAsync(new DataSheetId(dsId), token);
                        if (ds == null) return;

                        await GenerateOne(
                            ds,
                            poolsByCheckList[ds.CheckListId.Value],
                            innerModelGenerator,
                            innerFillingEngine,
                            innerDsRepo,
                            innerChecklistRepo,
                            innerUnitOfWork,
                            token);
                    });

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "DataSheetGenerateWorker error");
                }

                await Task.Delay(1000, ct);
            }
        }

        /// <summary>
        /// 生成一个 datasheet
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="pools"></param>
        /// <param name="modelGenerator"></param>
        /// <param name="fillingEngine"></param>
        /// <param name="dsRepo"></param>
        /// <param name="unitOfWork"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task GenerateOne(
            DataSheet ds,
            List<ConditionPool> pools,
            IDataSheetModelGenerator modelGenerator,
            DataSheetFillingEngine fillingEngine,
            IDataSheetRepository dsRepo,
            ICheckListRepository checklistRepo,
            IUnitOfWork unitOfWork,
            CancellationToken ct)
        {
            await using var transaction = await unitOfWork.BeginTransactionAsync(ct);

            try
            {
                ds.MarkGenerating();
                await dsRepo.UpdateAsync(ds, ct);          // ★ 让 EF 跟踪变更
                await unitOfWork.SaveChangesAsync(ct);

                var checklist = await checklistRepo.GetByIdAsync(ds.CheckListId, ct);
                if (checklist == null)
                {
                    ds.MarkFailed("CheckList not found", retryable: false);
                    await dsRepo.UpdateAsync(ds, ct);
                    await unitOfWork.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);
                    return;
                }

                var item = checklist.Items.FirstOrDefault(i => i.TestItemId == ds.TestItemId);
                if (item == null)
                {
                    ds.MarkFailed("ChecklistItem not found", retryable: false);
                    await dsRepo.UpdateAsync(ds, ct);
                    await unitOfWork.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);
                    return;
                }

                // 1. 拿到本任务对应的 model（优先从快照读，避免重复生成）
                DataSheetModel? myModel = null;
                List<DataSheetModel>? allModels = null;

                if (!string.IsNullOrEmpty(ds.ModelSnapshot))
                {
                    // 已经绑定过 model，直接用
                    myModel = JsonSerializer.Deserialize<DataSheetModel>(ds.ModelSnapshot);
                }
                else
                {
                    // 第一次跑（通常是 index = 0 的占位任务），调 modelGenerator
                    allModels = (await modelGenerator.GenerateAsync(pools, item, ct))?.ToList()
                                ?? new List<DataSheetModel>();

                    if (allModels.Count == 0)
                    {
                        ds.MarkFailed("Model generation returned empty", retryable: false);
                        await dsRepo.UpdateAsync(ds, ct);
                        await unitOfWork.SaveChangesAsync(ct);
                        await transaction.CommitAsync(ct);
                        return;
                    }

                    // 把 index=0 的 model 绑定到自己
                    var key0 = ResolveModelKey(allModels[0], 0);
                    ds.BindModel(key0, JsonSerializer.Serialize(allModels[0]));
                    myModel = allModels[0];

                    // ★ 若 N>1，展开 index=1..N-1
                    if (allModels.Count > 1)
                    {
                        await ExpandPendingTasks(ds, allModels, dsRepo, unitOfWork, ct);
                    }
                }

                if (myModel == null)
                {
                    ds.MarkFailed("Model resolved to null", retryable: false);
                    await dsRepo.UpdateAsync(ds, ct);
                    await unitOfWork.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);
                    return;
                }

                // 2. 生成文件
                var fileUrl = fillingEngine.FillDataSheet(myModel);

                if (string.IsNullOrEmpty(fileUrl))
                {
                    ds.MarkFailed("FillDataSheet returned empty url", retryable: false);
                }
                else
                {
                    ds.MarkSuccess(fileUrl);
                }

                // ★ 一次 SaveChanges：展开 + 状态变更一起提交
                await dsRepo.UpdateAsync(ds, ct);
                await unitOfWork.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                await transaction.RollbackAsync(ct);
                _logger.LogWarning(ex, "Unique violation on expand for {Id}", ds.Id.Value);

                using var recoveryScope = _scopeFactory.CreateScope();
                var recoveryRepo = recoveryScope.ServiceProvider.GetRequiredService<IDataSheetRepository>();
                var recoveryUow = recoveryScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var latest = await recoveryRepo.GetByIdAsync(ds.Id, ct);
                if (latest != null && latest.Status == DataSheetStatus.Generating)
                {
                    latest.MarkFailed("Concurrent expand detected, will retry", retryable: true);
                    await recoveryRepo.UpdateAsync(latest, ct);   // ★ 用 latest，用 recoveryRepo
                    await recoveryUow.SaveChangesAsync(ct);       // ★ 独立 scope，不需要再 Commit
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "GenerateOne failed for {Id}", ds.Id.Value);

                var retryable = IsTransient(ex);

                using var recoveryScope = _scopeFactory.CreateScope();
                var recoveryRepo = recoveryScope.ServiceProvider.GetRequiredService<IDataSheetRepository>();
                var recoveryUow = recoveryScope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var latest = await recoveryRepo.GetByIdAsync(ds.Id, ct);
                if (latest != null)
                {
                    latest.MarkFailed(ex.Message, retryable);
                    await recoveryRepo.UpdateAsync(latest, ct);
                    await recoveryUow.SaveChangesAsync(ct);
                }
            }
        }

        private async Task ExpandPendingTasks(
            DataSheet origin,
            IReadOnlyList<DataSheetModel> models,
            IDataSheetRepository dsRepo,
            IUnitOfWork unitOfWork,
            CancellationToken ct)
        {
            // origin.ModelIndex == 0，展开 index = 1 .. N-1
            if (origin.ModelIndex != 0)
            {
                _logger.LogWarning("ExpandPendingTasks called on non-zero index {Index}", origin.ModelIndex);
                return;
            }

            for (int i = 1; i < models.Count; i++)
            {
                var model = models[i];
                var modelKey = ResolveModelKey(model, i);   // 见第三节
                var snapshotJson = JsonSerializer.Serialize(model);

                // 幂等：先查
                var exists = await dsRepo.ExistsAsync(
                    origin.CheckListId, origin.TestItemId, i, ct);
                if (exists) continue;

                var newDs = DataSheet.Create(
                    origin.CheckListId,
                    null,
                    origin.ContactTemplateUrl,
                    DataSheetStatus.Pending,
                    origin.ReportNumber,
                    origin.TestItemId,
                    origin.BatchId,
                    modelIndex: i);

                newDs.BindModel(modelKey, snapshotJson);

                await dsRepo.AddAsync(newDs, ct);
            }
        }

        private static bool IsTransient(Exception ex)
        {
            // 简单判断，实际可按异常类型细分
            return ex is TimeoutException
                || ex is IOException
                || ex.Message.Contains("503")
                || ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsUniqueViolation(DbUpdateException ex)
        {
            // SQL Server: 2601 / 2627；PostgreSQL: 23505；MySQL: 1062
            var inner = ex.InnerException;
            return inner is SqlException sql
                && (sql.Number == 2601 || sql.Number == 2627);
        }

        private static string ResolveModelKey(DataSheetModel model, int index)
        {
            // 优先用 model 自带的业务键；没有则用 index 兜底
            return string.IsNullOrWhiteSpace(model.ModelKey)
                ? $"__index_{index}"
                : model.ModelKey;
        }
    }
}
