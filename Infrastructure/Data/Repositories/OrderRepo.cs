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
using NX_lims_Softlines_Command_System.Infrastructure.Providers;


namespace NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories
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
                    i.TestGroup == row.Group);

                if (existingRecord != null)
                {
                    // 记录具体的重复信息
                    var duplicateInfo = $"重复记录: ReportNum={row.ReportNum}, Group={row.Group}";
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
                };

                var orderschedule = new LabTestSchedule
                {
                    IdSchedule = snowId,
                    ReportDueDate = row.DueDate,
                    OrderInTime = row.LabIn,
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

            var orderLock = _orderLocks.GetOrAdd(order.Id, _ => new object());

            lock (orderLock)
            {
                try
                {
                    // 获取现有订单信息
                    var existingOrderInfo = _db.LabTestInfos.FirstOrDefault(o => o.Id == order.Id);
                    var existingOrderSchedule = _db.LabTestSchedules.FirstOrDefault(o => o.IdSchedule == order.IdSchedule);

                    if (existingOrderInfo == null || existingOrderSchedule == null)
                    {
                        return false;
                    }

                    // 更新LabTestInfo - 只排除Id和IdSchedule
                    UpdateEntity(existingOrderInfo, order,
                        nameof(OrderUpdateDto.Id),
                        nameof(OrderUpdateDto.IdSchedule));

                    // 更新LabTestSchedule - 只排除Id和IdSchedule以及属于LabTestInfo的字段
                    UpdateEntity(existingOrderSchedule, order,
                        nameof(OrderUpdateDto.Id),
                        nameof(OrderUpdateDto.IdSchedule),
                        nameof(OrderUpdateDto.ReportNumber),
                        nameof(OrderUpdateDto.Reviewer),
                        nameof(OrderUpdateDto.TestEngineer),
                        nameof(OrderUpdateDto.OrderEntryPerson),
                        nameof(OrderUpdateDto.CustomerService),
                        nameof(OrderUpdateDto.Status),
                        nameof(OrderUpdateDto.TestGroup),
                        nameof(OrderUpdateDto.TestSampleNum),
                        nameof(OrderUpdateDto.TestItemNum),
                        nameof(OrderUpdateDto.Remark),
                        nameof(OrderUpdateDto.Express),
                        nameof(OrderUpdateDto.Describe),
                        nameof(OrderUpdateDto.ScheduleIndex));

                    // 更新最后修改时间
                    if (order.LastUpdateTime.HasValue)
                    {
                        existingOrderInfo.LastUpdateTime = order.LastUpdateTime.Value;
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
                where o.OrderEntryPerson == user.NickName
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
                                         : "Completed"
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
                        RecodeId = x.Id,
                        Express = x.Express,
                        Group = x.TestGroup,
                        Remark = x.Remark,
                        LabIn = x.OrderInTime.ToUniversalTime(),
                        DueDate = x.ReportDueDate,
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
                .Max(f => f.LastUpdateTime))
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
            var infoQuery = _orderQueryProvider.QueryLabTestInfo(queryParams,_db);
            var scheduleQuery = _orderQueryProvider.QueryLabTestSchedule(queryParams, _db);

            // 获取共同的ID列表
            var commonIds = infoQuery.Select(o => o.Id).ToList();

            // 根据共同的ID筛选两个表的数据
            var filteredInfo = infoQuery.Where(o => commonIds.Contains(o.Id)).ToList();
            var filteredSchedule = scheduleQuery.Where(o => commonIds.Contains(o.IdSchedule)).ToList();

            // 合并结果
            var result = _orderQueryProvider.MergeResults(filteredInfo.AsQueryable(), filteredSchedule.AsQueryable());


            // 应用分页
            var pagedResult = result
                .Skip((dto.PageNum - 1) * dto.PageSize)
                .Take(dto.PageSize)
                .ToList();
            var TotalCount = commonIds.Count();

            var styleType = queryParams.ContainsKey("group") ? queryParams["group"].ToString()!.ToLower() : null;
            if (styleType == "all")
            {
                // 按ReportNumber分组，然后处理每个分组
                var groupedItems = pagedResult
                    .GroupBy(item =>
                    {
                        var info = ((LabTestInfo)item.GetType().GetProperty("Info")!.GetValue(item)!);
                        return info?.ReportNumber ?? string.Empty;
                    })
                    .Select(group => new
                    {
                        ReportNumber = group.Key,
                        Items = group.ToList()
                    });

                var orderOutputResult = new PageResult<OrderOutput>
                {
                    Items = groupedItems.Select(g =>
                    {
                        var firstItem = g.Items[0];
                        var firstInfo = (LabTestInfo)firstItem.GetType()!.GetProperty("Info")!.GetValue(firstItem)!;

                        // 收集所有唯一的TestGroup
                        var distinctGroups = g.Items.Select(item =>
                        {
                            var info = (LabTestInfo)item.GetType()!.GetProperty("Info")!.GetValue(item)!;
                            var schedule = (LabTestSchedule)item.GetType()!.GetProperty("Schedule")!.GetValue(item)!;

                            return new GroupOutput
                            {
                                RecodeId = info?.Id,
                                Express = info?.Express ?? string.Empty,
                                Group = info?.TestGroup ?? string.Empty,
                                TestSampleNum = info?.TestSampleNum ?? 0,
                                TestItemNum = info?.TestItemNum ?? 0,
                                Remark = info?.Remark ?? string.Empty,
                                Reviewer = info?.Reviewer ?? string.Empty,
                                ReviewFinish = schedule?.ReviewFinishTime,
                                LabIn = schedule?.LabOutTime ?? DateTimeOffset.Now,
                                DueDate = schedule?.ReportDueDate ?? DateOnly.FromDateTime(DateTime.Today),
                                LabOut = schedule?.LabOutTime,
                                Status = info?.Status == 1 ? "In Lab"
                                    : info?.Status == 2 ? "Review Finished"
                                    : "Completed"
                            };
                        }).Distinct().ToList();

                        return new OrderOutput
                        {
                            ReportNum = firstInfo?.ReportNumber ?? string.Empty,
                            OrderEntry = firstInfo?.OrderEntryPerson ?? string.Empty,
                            Cs = firstInfo?.CustomerService ?? string.Empty,
                            TestGroups = string.Join(",", distinctGroups.Select(x => x.Group).Distinct()),
                            Groups = distinctGroups.OrderBy(x =>
                                x.Group switch
                                {
                                    "Physics" => 0,
                                    "Wet" => 1,
                                    "Fiber" => 2,
                                    "Flam" => 3,
                                    _ => 4  // 其他group排在最后
                                }).ToList()
                        };
                    }).ToList(),
                    TotalCount = TotalCount,
                    Page = dto.PageNum,
                    PageSize = dto.PageSize
                };

                return orderOutputResult;
            }
            else
            {
                //这里组合成PageResult<OrderSummary>类型
                var orderSummaryResult = new PageResult<OrderSummary>
                {
                    Items = pagedResult.Select(item =>
                    {
                        var dynamicItem = item as dynamic;
                        var info = dynamicItem.Info as LabTestInfo;
                        var schedule = dynamicItem.Schedule as LabTestSchedule;

                        return new OrderSummary
                        {
                            ReportNum = info?.ReportNumber ?? string.Empty,
                            OrderEntry = info?.OrderEntryPerson ?? string.Empty,
                            Express = info?.Express ?? string.Empty,
                            Cs = info?.CustomerService ?? string.Empty,
                            TestGroup = info?.TestGroup ?? string.Empty,
                            ReviewFinish = schedule?.ReviewFinishTime,
                            Reviewer = info?.Reviewer ?? string.Empty,
                            DueDate = schedule?.ReportDueDate ?? DateOnly.FromDateTime(DateTime.Today),
                            LabIn = schedule?.OrderInTime ?? DateTimeOffset.Now,
                            LabOut = schedule?.LabOutTime,
                            TestSampleNum = info?.TestSampleNum ?? 0,
                            TestItemNum = info?.TestItemNum ?? 0,
                            Remark = info?.Remark ?? string.Empty,
                            Status = info?.Status == 1 ? "In Lab"
                                         : info?.Status == 2 ? "Review Finished"
                                         : "Completed"
                        };
                    }).ToList(),
                    TotalCount = TotalCount,
                    Page = dto.PageNum,
                    PageSize = dto.PageSize
                };
                return orderSummaryResult;
            }

        }


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


        private void UpdateEntity<T>(T entity, OrderUpdateDto dto, params string[] excludeProperties)
        {
            var entityType = typeof(T);
            var properties = entityType.GetProperties();

            foreach (var property in properties)
            {
                // 跳过排除列表中的字段
                if (excludeProperties.Contains(property.Name))
                    continue;

                // 获取DTO中对应的属性值
                var dtoProperty = typeof(OrderUpdateDto).GetProperty(property.Name);
                if (dtoProperty == null)
                    continue;

                var value = dtoProperty.GetValue(dto);
                if (value == null)
                    continue;

                // 设置值到实体
                if (property.CanWrite)
                {
                    // 特殊处理类型转换
                    if (property.PropertyType == typeof(DateOnly) && value is DateTime dateTime)
                    {
                        property.SetValue(entity, DateOnly.FromDateTime(dateTime));
                    }
                    else if (property.PropertyType == typeof(DateTimeOffset) && value is DateTime dateTimeValue)
                    {
                        property.SetValue(entity, new DateTimeOffset(dateTimeValue));
                    }
                    else if (property.PropertyType == typeof(DateTimeOffset?) && value is DateTime dateTimeNullableValue)
                    {
                        property.SetValue(entity, (DateTimeOffset?)new DateTimeOffset(dateTimeNullableValue));
                    }
                    else if (property.PropertyType == typeof(DateTime?) && value is DateTime dateTimeValueNullable)
                    {
                        property.SetValue(entity, (DateTime?)dateTimeValueNullable);
                    }
                    else
                    {
                        property.SetValue(entity, value);
                    }
                }
            }
        }

    }
}
