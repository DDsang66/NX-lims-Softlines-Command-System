using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.LabScheduleContext;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Domain.Share.Enums;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;
using NX_lims_Softlines_Command_System.src.Infrastructure.Interface;
using System;

namespace NX_lims_Softlines_Command_System.src.Application.UseCase
{
    public class LabScheduleUseCaseService : IScopedDependency
    {
        private readonly ICheckListAppService _checkListAppService;
        private readonly IWebHostEnvironment _env;
        private readonly IDocxMergeService _docxMergeService;
        private readonly IFileStorageService _fileStorageService;
        private readonly dbContext _db;

        public LabScheduleUseCaseService(
            ICheckListAppService checkListAppService,
            IWebHostEnvironment env,
            IDocxMergeService docxMergerService,
            IFileStorageService fileStorageService,
            dbContext db)
        {
            _db = db;
            _env = env;
            _docxMergeService = docxMergerService;
            _fileStorageService = fileStorageService;
            _checkListAppService = checkListAppService;
        }

        /// <summary>
        /// 基础CURD，返回实验室日程表
        /// </summary>
        /// <param name="param"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<PagedResult<LabScheduleDto>> GetSummaryAsync(
            CheckListQueryParamDto param,CancellationToken ct)
        {
            if (param.PageNum < 1) param.PageNum = 1;
            if (param.PageSize < 1) param.PageSize = 10;

            // ============ 第 1 段：分页查 CheckList 主表 ============
            var query = _db.CheckLists.AsNoTracking();

            // ChecklistId 是 VO，比较时用 Value
            if (!string.IsNullOrWhiteSpace(param.ChecklistId)
                && Guid.TryParse(param.ChecklistId, out var cid))
            {
                query = query.Where(c => c.CheckListId == cid);
            }

            // ★ CheckList 上没有 ReportNumber，改成按 OderId 过滤
            //   （如果业务上 ReportNumber 实际存的是 OrderId，就保持这个写法）
            if (!string.IsNullOrWhiteSpace(param.ReportNumber))
            {
                query = query.Where(c => c.OrderId != null
                                      && c.OrderId.ToString() == param.ReportNumber);
            }

            if (!string.IsNullOrWhiteSpace(param.Status)
                && param.Status != "All"
                && Enum.TryParse<CheckListStatus>(param.Status, out var status))
            {
                query = query.Where(c => c.Status == (byte)status);
            }

            if (param.StartTime.HasValue)
                query = query.Where(c => c.CreatedTime >= param.StartTime.Value);

            if (param.EndTime.HasValue)
                query = query.Where(c => c.CreatedTime <= param.EndTime.Value);

            var total = await query.CountAsync();

            // ★ 只投影主表字段，不带 Items（避免 EF Core 分页警告）
            var page = await query
                .OrderByDescending(c => c.CreatedTime)
                .Skip((param.PageNum - 1) * param.PageSize)
                .Take(param.PageSize)
                .Select(c => new
                {
                    CheckListId = c.CheckListId,            // ★ 主键是 Id
                    OrderId = c.OrderId != null ? c.OrderId : (string?)null,
                    c.Status,
                    c.CreatedTime,
                })
                .ToListAsync();

            if (!page.Any())
                return PagedResult<LabScheduleDto>.Empty(total);

            var checklistIds = page.Select(c => c.CheckListId).ToList();

            // ============ 第 2 段：批量查 CheckListItem ============
            // 注意：CheckListId 是 VO，Contains 时比较 Value
            var items = await _db.CheckListItems
                .AsNoTracking()
                .Where(i => checklistIds.Contains(i.CheckListId))
                .Select(i => new
                {
                    ItemId = i.CheckListItemId,                                 // ★ Entity 主键是 Id
                    CheckListId = i.CheckListId,
                    TestGroup = i.TestGroup,                       // VO，下一步再 ToString
                    TestItemId = i.TestItemId != null ? i.TestItemId.ToString() : null,
                    i.Status,
                    i.Requirement,
                })
                .ToListAsync();

            if (!items.Any())
            {
                // 没有 items 也要返回父级（Groups 为空）
                var emptyResult = page.Select(c => new LabScheduleDto
                {
                    ChecklistId = c.CheckListId,
                    ReportNumber = c.OrderId?.ToString() ?? string.Empty,
                    Status = c.Status.ToString(),
                    TestGroups = string.Empty,
                    Items = new List<CheckListItemReaderDto>(),
                }).ToList();

                return new PagedResult<LabScheduleDto>
                {
                    Items = emptyResult,
                    TotalCount = total,
                };
            }

            // ============ 第 3 段：批量查 DataSheet（另一聚合根）============
            // ★ DataSheet 通过 (CheckListId, TestItemId) 关联，而不是 CheckListItem.Id
            var testItemIds = items
                .Where(i => !string.IsNullOrEmpty(i.TestItemId))
                .Select(i => i.TestItemId!)
                .Distinct()
                .ToList();

            var datasheets = await _db.DataSheets
                .AsNoTracking()
                .Where(d => checklistIds.Contains(d.CheckListId))
                .Select(d => new
                {
                    DataSheetId = d.Id,
                    CheckListId = d.CheckListId,
                    TestItemId = d.TestItemId.ToString(),
                    d.ModelKey,
                    d.Url,
                    d.Status,
                    d.ModelIndex,
                    d.Version,
                    // Approver 不存在于 DataSheet，需要从别处来（见下面说明）
                })
                .ToListAsync();

            // ★ 用 (CheckListId, TestItemId) 组合键做 lookup
            //   因为同一 checklist 下同一 testItem 可能有多个 datasheet（多 model）
            var dsLookup = datasheets
                .GroupBy(d => (d.CheckListId, d.TestItemId))
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(x => x.ModelIndex).ToList()
                );

            // ============ 内存拼装 ============
            var itemsByChecklist = items
                .GroupBy(i => i.CheckListId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = page.Select(c =>
            {
                var cItems = itemsByChecklist.TryGetValue(c.CheckListId, out var list)
                    ? list
                    : new();

                // ★ TestGroup 是 VO，先 ToString 再 Distinct
                var groups = cItems
                    .Select(i => ((TestGroup)i.TestGroup).ToString() ?? string.Empty)
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Distinct()
                    .OrderBy(s => s)
                    .ToList();

                return new LabScheduleDto
                {
                    ChecklistId = c.CheckListId,
                    ReportNumber = c.OrderId?.ToString() ?? string.Empty,
                    Status = c.Status.ToString(),
                    TestGroups = string.Join(", ", groups),
                    Items = cItems.Select(i =>
                    {
                        // ★ 用 (CheckListId, TestItemId) 找 datasheet
                        dsLookup.TryGetValue((c.CheckListId, i.TestItemId ?? string.Empty), out var dsList);
                        var ds = dsList?.FirstOrDefault();

                        return new CheckListItemReaderDto
                        {
                            ItemId = i.ItemId,
                            Group = ((TestGroup)i.TestGroup).ToString() ?? string.Empty,
                            TestItem = i.TestItemId ??  string.Empty,
                            Status = ((CheckListStatus)i.Status).ToString(),

                            DatasheetId = ds?.DataSheetId.ToString() ?? string.Empty,
                            ModelKey = ds?.ModelKey ?? string.Empty,
                            Url = ds?.Url ?? string.Empty,
                            Approver = string.Empty,   // ★ DataSheet 上没有 Approver，见下方说明

                            // 关联键
                            CheckListItemId = i.ItemId,
                            CheckListId = c.CheckListId,
                        };
                    }).ToList()
                };
            }).ToList();

            return new PagedResult<LabScheduleDto>
            {
                Items = result,
                TotalCount = total,
            };
        }

        /// <summary>
        /// 合并选中的items的DataSheet，并返回下载链接
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<Result<DocxUrlResponseDto>> HandleDataSheetMerge(MergeLabScheduleDto dto,CancellationToken ct) 
        {
            var fileName = $"{dto.ReportNumber}_{dto.TestGroup}_GeneralResult_{DateTime.Now:yyyyMMddHHmmss}.docx";

            var sections = dto.SelectedDataSheetUrls
                .Select(url => new DocxMergeSection{FilePath = Path.Combine(_env.WebRootPath, url) }).ToList();

            await  _docxMergeService.MergeAsync(
                Path.Combine(_env.WebRootPath, "DocxModel", "Development", "DataSheet_Cover.docx"),
                sections, 
                Path.Combine(_env.WebRootPath, "DocxModel", "SaveDocx", "DataSheet", dto.ReportNumber, fileName),
                new DocxMergeOptions(),
                ct);

            var urlResponse = new DocxUrlResponseDto
            {
                fileKey = Guid.NewGuid().ToString(),
                fileName = fileName,
                downloadUrl = $"/LabSchedule/datasheet-{fileName}/{dto.ReportNumber}/download",
                callbackUrl = string.Empty, // 可选：如果需要回调，可以设置回调 URL
            };

            return Result<DocxUrlResponseDto>.Ok(urlResponse);
        }
    } 
}
