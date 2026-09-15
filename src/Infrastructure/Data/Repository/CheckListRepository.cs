using DocumentFormat.OpenXml.Office2010.Excel;
using Mapster;
using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.CheckListContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;
using System.Text.Json;
using CheckList = NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence.CheckList;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository
{
    public class CheckListRepository:IScopedDependency,ICheckListRepository
    {
        private readonly dbContext _dbContext;

        public CheckListRepository(dbContext dbContext) 
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// 添加聚合根
        /// </summary>
        /// <param name="aggregateRoot"></param>
        /// <returns></returns>
        public async Task AddAsync(Domain.Aggregeates.CheckListContext.CheckList aggregateRoot, CancellationToken ct)
        {
            var checkListPo = aggregateRoot.Adapt<CheckList>();

            // 手动映射并添加子实体到 PO 的集合中
            if (aggregateRoot.Items != null)
            {
                foreach (var item in aggregateRoot.Items)
                {
                    var itemPo = item.Adapt<Persistence.CheckListItem>();

                    itemPo.CheckListId = checkListPo.CheckListId;

                    await _dbContext.AddAsync(itemPo, ct);
                }
            }

            await  _dbContext.AddAsync(checkListPo, ct);

            await Task.CompletedTask;
        }

        /// <summary>
        /// 修改聚合根
        /// </summary>
        /// <param name="aggregateRoot"></param>
        /// <returns></returns>
        public async Task UpdateAsync(Domain.Aggregeates.CheckListContext.CheckList aggregateRoot, CancellationToken ct) 
        {
            var existingPo = await _dbContext.CheckLists
                .FirstOrDefaultAsync(cl => cl.CheckListId == aggregateRoot.Id.Value, ct);

            if (existingPo == null)
                throw new KeyNotFoundException($"CheckList with ID {aggregateRoot.Id.Value} not found");

            // 2. 更新主实体属性（Remark, Status 等）
            existingPo.Remark = aggregateRoot.Remark;
            existingPo.Status = (byte)aggregateRoot.Status;
            // ...其他主实体属性

            // 3. 处理所有 Items 的变更（新增/修改/删除）
            // 获取当前数据库中的 Items ID
            var existingItemIds = await _dbContext.CheckListItems
               .Where(i => i.CheckListId == aggregateRoot.Id.Value)
               .Select(i => i.CheckListItemId)
               .ToListAsync(ct);
            // 获取聚合根中的 Items ID
            var newItemIds = aggregateRoot.Items.Select(i => i.Id).ToList();

            // 3.1 删除 Items（如果聚合根中不再有）
            foreach (var itemId in existingItemIds.Except(newItemIds))
            {
                var itemToRemove = await _dbContext.CheckListItems.FindAsync(new object[] { itemId }, ct);
                if (itemToRemove != null)
                {
                    _dbContext.CheckListItems.Remove(itemToRemove);
                }
            }

            // 3.2 更新或新增所有 Items
            foreach (var item in aggregateRoot.Items)
            {
                var itemPo = await _dbContext.CheckListItems
                    .FirstOrDefaultAsync(i => i.CheckListItemId == item.Id && i.CheckListId == aggregateRoot.Id.Value, ct);

                if (itemPo != null)
                {
                    // 更新现有 Item
                    itemPo.TestItemId = item.TestItemId?.Value ?? string.Empty;
                    itemPo.StandardId = string.Join(",", item.StandardIds.Select(id => id.Value));
                    itemPo.BuyerModifiedTestItem = item.BuyerModifiedTestItemId;
                    itemPo.BuyerModifiedTestStandard = item.BuyerModifiedTextMethodId;
                    itemPo.TestGroup = (byte)item.TestGroup;
                    itemPo.TestPointParams = JsonSerializer.Serialize(
                        item.TestPointParams,
                        new JsonSerializerOptions { WriteIndented = false, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                    itemPo.Samples = string.Join(",", item.Samples);
                    itemPo.Status = (byte)item.Status;
                }
                else
                {
                    // 新增 Item
                    itemPo = new Infrastructure.Data.Persistence.CheckListItem
                    {
                        CheckListItemId = item.Id,
                        CheckListId = aggregateRoot.Id.Value, // 关联到正确的 CheckList
                        TestItemId = item.TestItemId?.Value ?? string.Empty,
                        StandardId = string.Join(",", item.StandardIds.Select(id => id.Value)),
                        BuyerModifiedTestItem = item.BuyerModifiedTestItemId,
                        BuyerModifiedTestStandard = item.BuyerModifiedTextMethodId,
                        TestGroup = (byte)item.TestGroup,
                        TestPointParams = JsonSerializer.Serialize(
                            item.TestPointParams,
                            new JsonSerializerOptions { WriteIndented = false, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                        Samples = string.Join(",", item.Samples),
                        Status = (byte)item.Status
                    };
                    _dbContext.CheckListItems.Add(itemPo);
                }
            }

        }

        /// <summary>
        /// 查询聚合根
        /// </summary>
        /// <param name="aggregateRootId"></param>
        /// <param name="ct"></param>
        /// <returns>聚合根</returns>
        public async Task<Domain.Aggregeates.CheckListContext.CheckList> GetByIdAsync(CheckListId aggregateRootId, CancellationToken ct) 
        {
            var checkListPo = await  _dbContext.FindAsync<CheckList>(aggregateRootId.Value,ct);

            // 2. 查询内部实体 PO
            var checkListItemPos = await _dbContext.CheckListItems
                .Where(x => x.CheckListId == aggregateRootId.Value) // 外键应该是 CheckListId
                .ToListAsync(ct);

            // 3. PO -> 领域实体 映射
            // 将子表 PO 转换为领域实体集合
            var checklist = Domain.Aggregeates.CheckListContext.CheckList.Reconstitute(
                new CheckListId(checkListPo.CheckListId),
                new OrderId(checkListPo.OrderId),
                checkListItemPos.Adapt<List<Domain.Aggregeates.CheckListContext.CheckListItem>>(),
                checkListPo.CreatedTime,
                (CheckListStatus)checkListPo.Status,
                checkListPo.Remark);

            return checklist;
        }

    }
}
