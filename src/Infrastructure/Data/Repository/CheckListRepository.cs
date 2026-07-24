using DocumentFormat.OpenXml.Office2010.Excel;
using Mapster;
using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;
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
            var existingPo = await _dbContext.CheckLists.FindAsync(aggregateRoot.Id.Value);

            aggregateRoot.Adapt(existingPo);

            if (aggregateRoot.Items != null)
            {
                foreach (var item in aggregateRoot.Items)
                {
                    var itemPo = await _dbContext.CheckListItems.FindAsync(item.Id);
                    itemPo.TestItemId = item.TestItemId == null ? string.Empty : item.TestItemId.Value;
                    itemPo.StandardId = string.Join(",", item.StandardIds.Select(id => id.Value));
                    itemPo.BuyerModifiedTestItem = item.BuyerModifiedTestItemId;
                    itemPo.BuyerModifiedTestStandard = item.BuyerModifiedTextMethodId;
                    itemPo.TestGroup = (byte)item.TestGroup;
                    itemPo.TestPointParams = System.Text.Json.JsonSerializer.Serialize(
                        item.TestPointParams,
                        new System.Text.Json.JsonSerializerOptions
                        {
                            WriteIndented = false,
                            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                        });
                    itemPo.Samples = string.Join(",", item.Samples);
                    itemPo.Status = (byte)item.Status;
                }
            }
            _dbContext.CheckLists.Update(existingPo);
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
