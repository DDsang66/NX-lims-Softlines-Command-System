using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.Application.Services.AuthenticationService;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Drawing.Printing;
using DocumentFormat.OpenXml.Vml.Office;
using System.Collections.Concurrent;
using DocumentFormat.OpenXml.Drawing;
using Azure.Core;


namespace NX_lims_Softlines_Command_System.Infrastructure.Providers
{
    public class OrderRepo
    {
        private readonly LabDbContextSec _db;
        private readonly OrderQueryProvider _orderQueryProvider;
        private readonly ConcurrentDictionary<long, object> _orderLocks = new ConcurrentDictionary<long, object>();
        public OrderRepo(LabDbContextSec db, OrderQueryProvider orderQueryProvider)
        {
            _db = db;
            _orderQueryProvider = orderQueryProvider;
        }

        /// <summary>
        /// 表单数据添加
        /// </summary>
        public bool AddOrder(OrderDto order)
        {
            if (order == null) return false;
            var rows = order.Rows;
            // 检查所有rows中的记录是否已存在
            foreach (var row in rows)
            {
                var existingRecord = _db.LabTestInfos.FirstOrDefault(i =>
                    i.ReportNumber == row.ReportNum &&
                    i.TestGroup == row.Group &&
                    i.IsDelete == "N");

                if (existingRecord != null)
                {
                    // 记录具体的重复信息
                    var duplicateInfo = $"重复记录: ReportNum={row.ReportNum}, Group={row.Group}";
                    // 可以在这里添加日志记录
                    return false;
                }

                if (row.DueDate == null || row.LabIn == null)
                {
                    // 记录具体的重复信息

                    // 可以在这里添加日志记录
                    return false;
                }
            }

            var snowflake = new SnowflakeIdGenerator();
            foreach (var row in rows)
            {
                long snowId = snowflake.NextId();
                var csName = _db.CustomerServices.FirstOrDefault(i => i.Id == row.Cs)!.CustomerService1;
                var currentTime = DateTimeOffset.Now;
                var orderEntity = new LabTestInfo
                {
                    Id = snowId,
                    ReportNumber = row.ReportNum,
                    OrderEntryPerson = row.OrderEntry,
                    Status = 1,
                    Express = row.Express,
                    CustomerService = csName,
                    TestGroup = row.Group,
                    Remark = order.Remark,
                    ScheduleIndex = snowId,
                    LastUpdateTime = currentTime,
                    IsDelete = "N"
                };

                var orderschedule = new LabTestSchedule
                {
                    IdSchedule = snowId,
                    ReportDueDate = row.DueDate ?? DateTimeOffset.Now,
                    OrderInTime = row.LabIn ?? DateTimeOffset.Now,
                };
                _db.LabTestInfos.Add(orderEntity);
                _db.LabTestSchedules.Add(orderschedule);
            }
            _db.SaveChanges();
            return true;
        }


        /// <summary>
        /// 表单数据更新
        /// </summary>
        public bool UpdateOrder(OrderUpdateDto order)
        {
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order));
            }

            if (order.Rows == null || order.Rows.Count == 0)
            {
                return false; // 或者抛出异常，取决于你的需求
            }
            try
            {
                foreach (var item in order.Rows)
                {
                    if (item.RecordId == null)
                    {
                        return false; // 或者抛出异常，取决于你的需求
                    }
                    var orderLock = _orderLocks.GetOrAdd(long.Parse(item.RecordId), _ => new object());

                    lock (orderLock)
                    {

                        // 获取现有订单信息
                        var existingOrderInfo = _db.LabTestInfos.FirstOrDefault(o => o.Id == long.Parse(item.RecordId) && o.IsDelete == "N");
                        var existingOrderSchedule = _db.LabTestSchedules.FirstOrDefault(o => o.IdSchedule == long.Parse(item.RecordId));

                        if (existingOrderInfo == null || existingOrderSchedule == null || existingOrderInfo.TestGroup != item.TestGroup)
                        {
                            return false;
                        }

                        //labtestinfo表
                        existingOrderInfo.Reviewer = item.Reviewer;
                        existingOrderInfo.Express = item.Express;
                        existingOrderInfo.Remark = item.Remark;
                        existingOrderInfo.LastUpdateTime = DateTimeOffset.Now;
                        existingOrderInfo.TestItemNum = item.TestItemNum;
                        existingOrderInfo.TestSampleNum = item.TestSampleNum;
                        //labtestschedule表
                        if (item.ReviewFinishTime != null)
                        {
                            existingOrderInfo.Status = 2;
                            existingOrderSchedule.ReviewFinishTime = item.ReviewFinishTime;
                        }
                        if (item.LabOutTime != null)
                        {
                            existingOrderInfo.Status = 3;
                            existingOrderSchedule.LabOutTime = item.LabOutTime;
                        }
                        _db.LabTestInfos.Update(existingOrderInfo);
                        _db.LabTestSchedules.Update(existingOrderSchedule);
                    }
                }
                // 保存更改到数据库
                _db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                // 记录异常日志
                return false;
            }
        }


        /// <summary>
        /// 表单数据软删除
        /// </summary>
        public bool DeleteOrder(OrderDeleteRequest order)
        {
            var user = _db.Users.FirstOrDefault(u => u.UserId == order.UserId);
            if (user == null) return false;
            try 
            {
                foreach (var item in order.Items)
                {
                    var orderLock = _orderLocks.GetOrAdd(long.Parse(item.RecordId), _ => new object());
                    lock (orderLock)
                    {
                        long? recordId = long.Parse(item.RecordId);
                        string reason = item.Reason;
                        // 处理删除逻辑

                        if (string.IsNullOrEmpty(reason) || recordId == null) continue;
                        var orderEntity = _db.LabTestInfos.FirstOrDefault(o => o.Id == recordId);
                        var scheduleEntity = _db.LabTestSchedules.FirstOrDefault(o => o.IdSchedule == recordId);
                        if (orderEntity == null || scheduleEntity == null) continue;
                        else
                        {
                            orderEntity!.IsDelete = "Y";
                        }
                        //生成当前订单级删除历史
                        var groupdeleteHistory = new AuditHistory
                        {
                            ChangeHistoryId = new SnowflakeIdGenerator().NextId(),
                            ContactTable = "LabTestInfo & LabTestSchedule",
                            ContactId = recordId,
                            ReportNumber = orderEntity.ReportNumber,
                            LastChangeTime = DateTimeOffset.Now,
                        };
                        //对当前动作进行日志记录
                        var auditlog = new AuditChange
                        {
                            ChangeRecordId = new SnowflakeIdGenerator().NextId(),
                            ChangeHistoryIndex = groupdeleteHistory.ChangeHistoryId,
                            TableName = "LabTestInfo & LabTestSchedule",
                            ChangePerson = user!.NickName,
                            ChangeTime = DateTimeOffset.Now,
                            Remark = reason,
                            BatchIndex = "delete"
                        };
                        _db.AuditChanges.Add(auditlog);
                        _db.AuditHistories.Add(groupdeleteHistory);
                        _db.LabTestInfos.Update(orderEntity);
                        _db.SaveChanges();
                    }
                }
                return true;
            } 
            catch (Exception ex)
            { 
                return false; 
            }
        }


        /// <summary>
        /// 获取当前用户的订单列表
        /// </summary>
        public async Task<OrderOutput[]> GetOrderListAsync(string userId)
        {
            // 1. 先拿昵称（防御空引用）
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return Array.Empty<OrderOutput>();

            // 2. 异步查询并投射
            var flat = await (
                from o in _db.LabTestInfos
                join s in _db.LabTestSchedules on o.ScheduleIndex equals s.IdSchedule
                where o.OrderEntryPerson == user.NickName && o.IsDelete == "N"
                select new
                {
                    o.Id,
                    o.ReportNumber,
                    o.OrderEntryPerson,
                    o.CustomerService,
                    s.OrderInTime,
                    o.Express,
                    o.TestGroup,
                    o.Remark,
                    s.ReportDueDate,
                    o.LastUpdateTime,
                    Status = o.Status == 1 ? "In Lab"
                                         : o.Status == 2 ? "Review Finished"
                                         : "Test Done"
                })
                .ToListAsync();

            // 2. 分组投射,按订单时间排序
            var orders = flat
                .GroupBy(x => new { x.ReportNumber, x.OrderEntryPerson, x.CustomerService })
                .Select(g => new OrderOutput
                {
                    ReportNum = g.Key.ReportNumber,
                    OrderEntry = g.Key.OrderEntryPerson,
                    Cs = g.Key.CustomerService,
                    TestGroups = string.Join(",", g.Select(x => x.TestGroup).Distinct()),
                    Groups = g.Select(x => new GroupOutput
                    {
                        RecordId = x.Id.ToString(),
                        Express = x.Express,
                        Group = x.TestGroup,
                        Remark = x.Remark,
                        LabIn = x.OrderInTime?.ToUniversalTime(),
                        DueDate = DateOnly.FromDateTime(x.ReportDueDate!.Value.DateTime),
                        Status = x.Status
                    }).OrderBy(x =>
                        x.Group switch
                        {
                            "Physics" => 0,
                            "Wet" => 1,
                            "Fiber" => 2,
                            "Flam" => 3,
                            _ => 4  // 其他group排在最后
                        })
                    .ToList()
                })
                .OrderByDescending(o => flat.Where(f => f.ReportNumber == o.ReportNum)
                .Max(f => f.OrderInTime))
                .ToArray();

            return orders;
        }


        /// <summary>
        /// 根据样式和查询参数筛选summary格式的订单
        /// </summary>
        public async Task<object> GetSummaryOrdersAsync(OrderQueryParams dto)
        {
            // 获取查询条件
            var queryParams = _orderQueryProvider.GetQueryParams(dto);

            // 分别查询两个表
            var infoQuery = _orderQueryProvider.QueryLabTestInfo(queryParams, _db);
            var scheduleQuery = _orderQueryProvider.QueryLabTestSchedule(queryParams, _db);

            // 获取共同的ID列表
            var commonIds = infoQuery.Select(o => o.Id).ToList();

            // 根据共同的ID筛选两个表的数据
            var filteredInfo = infoQuery.Where(o => commonIds.Contains(o.Id) && o.IsDelete == "N").ToList();
            var filteredSchedule = scheduleQuery.Where(o => commonIds.Contains(o.IdSchedule)).ToList();

            // 合并结果
            IQueryable<LabTestJoinDto> result = _orderQueryProvider.MergeResults(filteredInfo.AsQueryable(), filteredSchedule.AsQueryable());


            // 0. 取全量数据（一次落内存，后面要分组/映射）
            var fullData = result.ToList();

            // 1. 预计算两种总数
            int totalCountAll = fullData.Count();                                          // 平铺模式
            int totalCountFold = fullData.Select(x => x.Info.ReportNumber).Distinct().Count(); // 分组模式

            // 2. 分页参数
            int skip = (dto.PageNum - 1) * dto.PageSize;
            int take = dto.PageSize;

            // 3. 风格开关
            string styleType = queryParams.ContainsKey("group")
                ? queryParams["group"].ToString()!.ToLower()
                : "else";

            /* ---------------------------------------------------- */
            if (styleType == "all")
            {
                /* 分组模式 → OrderOutput */
                var grouped = fullData
                    .GroupBy(d => d.Info.ReportNumber ?? string.Empty)
                    .Select(g =>
                    {
                        var first = g.First();

                        // 构造 GroupOutput
                        var distinctGroups = g
                            .Select(d => new GroupOutput
                            {
                                RecordId = d.Info.Id.ToString(),
                                Express = d.Info.Express ?? string.Empty,
                                Group = d.Info.TestGroup ?? string.Empty,
                                TestSampleNum = d.Info.TestSampleNum ?? 0,
                                TestItemNum = d.Info.TestItemNum ?? 0,
                                Remark = d.Info.Remark ?? string.Empty,
                                Reviewer = d.Info.Reviewer ?? string.Empty,
                                ReviewFinish = d.Schedule.ReviewFinishTime,
                                LabIn = d.Schedule.OrderInTime ?? DateTimeOffset.Now,
                                DueDate = d.Schedule.ReportDueDate switch
                                {
                                    DateTimeOffset offset => DateOnly.FromDateTime(offset.DateTime.Date),
                                    _ => DateOnly.FromDateTime(DateTime.UtcNow.Date)
                                },
                                LabOut = d.Schedule.LabOutTime,
                                Status = d.Info.Status switch
                                {
                                    1 => "In Lab",
                                    2 => "Review Finished",
                                    _ => "Test Done"
                                }
                            })
                            .Distinct()
                            .OrderBy(x => x.Group switch
                            {
                                "Physics" => 0,
                                "Wet" => 1,
                                "Fiber" => 2,
                                "Flam" => 3,
                                _ => 4
                            })
                            .ToList();

                        return new OrderOutput
                        {
                            ReportNum = g.Key,
                            OrderEntry = first.Info.OrderEntryPerson ?? string.Empty,
                            Cs = first.Info.CustomerService ?? string.Empty,
                            TestGroups = string.Join(",", distinctGroups.Select(dg => dg.Group).Distinct()),
                            Groups = distinctGroups
                        };
                    })
                    .ToList();

                // 4. 对最终 DTO 分页
                var pageList = grouped.Skip(skip).Take(take).ToList();

                return new PageResult<OrderOutput>
                {
                    Items = pageList,
                    TotalCount = totalCountFold,
                    Page = dto.PageNum,
                    PageSize = dto.PageSize
                };
            }
            else
            {
                /* 平铺模式 → OrderSummary */
                var flat = fullData
                    .Select(d => new OrderSummary
                    {
                        RecordId = d.Info.Id.ToString(),
                        ReportNum = d.Info.ReportNumber ?? string.Empty,
                        OrderEntry = d.Info.OrderEntryPerson ?? string.Empty,
                        Express = d.Info.Express ?? string.Empty,
                        Cs = d.Info.CustomerService ?? string.Empty,
                        TestGroup = d.Info.TestGroup ?? string.Empty,
                        ReviewFinish = d.Schedule.ReviewFinishTime,
                        Reviewer = d.Info.Reviewer ?? string.Empty,
                        DueDate = d.Schedule.ReportDueDate switch
                        {
                            DateTimeOffset offset => DateOnly.FromDateTime(offset.DateTime.Date),
                            _ => DateOnly.FromDateTime(DateTime.UtcNow.Date)
                        },
                        LabIn = d.Schedule.OrderInTime ?? DateTimeOffset.Now,
                        LabOut = d.Schedule.LabOutTime,
                        TestSampleNum = d.Info.TestSampleNum ?? 0,
                        TestItemNum = d.Info.TestItemNum ?? 0,
                        Remark = d.Info.Remark ?? string.Empty,
                        Status = d.Info.Status switch
                        {
                            1 => "In Lab",
                            2 => "Review Finished",
                            _ => "Test Done"
                        }
                    })
                    .ToList();

                // 4. 对最终 DTO 分页
                var pageList = flat.Skip(skip).Take(take).ToList();

                return new PageResult<OrderSummary>
                {
                    Items = pageList,
                    TotalCount = totalCountAll,
                    Page = dto.PageNum,
                    PageSize = dto.PageSize
                };
            }

        }

        //加急计算逻辑
        private string? GetExpressName(DateOnly duedate, DateTime labindate)
        {
            string express = "-";
            var days = (duedate.ToDateTime(new TimeOnly()) - labindate).TotalDays + 1;
            if (days <= 2 && days > 0) express = "Same Day";
            else if (days > 2 && days <= 3) express = "Shuttle";
            else if (days > 3 && days <= 4) express = "Express";
            else if (days > 4) express = "Regular";
            return express;
        }

    }
}
