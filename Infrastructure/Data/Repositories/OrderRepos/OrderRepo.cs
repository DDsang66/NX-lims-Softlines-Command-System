using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.OrderContext;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Infrastructure.Providers.Order;


namespace NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories.OrderRepos
{
    public class OrderRepo
    {
        private readonly LabDbContextSec _db;
        private readonly OrderQueryProvider _orderQueryProvider;
        private readonly OrderReportingQueryProvider _orderReportingQueryProvider;
        public OrderRepo(LabDbContextSec db,
            OrderQueryProvider orderQueryProvider,
            OrderReportingQueryProvider orderReportingQueryProvider)
        {
            _db = db;
            _orderQueryProvider = orderQueryProvider;
            _orderReportingQueryProvider = orderReportingQueryProvider;
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
                where o.OrderEntryPerson == user.NickName && o.IsDelete == "N" && o.OrderInTime!.Value.Date == DateTimeOffset.Now.Date
                select new
                {
                    o.Id,
                    o.ReportNumber,
                    o.OrderEntryPerson,
                    o.CustomerService,
                    o.OrderInTime,
                    o.RfidCode,
                    o.Express,
                    o.TestGroup,
                    o.Remark,
                    o.ReportDueDate,
                    o.DelayType,
                    o.DelayReason,
                    o.LastUpdateTime,
                    Status = o.Status == 1 ? "Entry Complete"
                           : o.Status == 2 ? "Review Finished"
                           : o.Status == 3 ? "In Lab"
                           : o.Status == 4 ? "Test Done"
                           : o.Status == 5 ? "Report Out"
                           : "Unknown"
                })
                .ToListAsync();

            // 2. 分组投射,按订单时间排序
            var orders = flat
                .GroupBy(x => new { x.ReportNumber, x.OrderEntryPerson, x.CustomerService })
                .Select(g => new OrderOutput
                {
                    ReportNumber = g.Key.ReportNumber,
                    OrderEntryPerson = g.Key.OrderEntryPerson,
                    CustomerServiceName = g.Key.CustomerService,
                    TestGroups = string.Join(",", g.Select(x => x.TestGroup).Distinct()),
                    Lines = g.Select(x => new OrderLineOutput
                    {
                        LineId = x.Id.ToString(),
                        Express = x.Express,
                        TestGroup = x.TestGroup,
                        Remark = x.Remark,
                        DelayType = x.DelayType,
                        DelayReason = x.DelayReason,
                        LabIn = x.OrderInTime?.ToUniversalTime(),
                        DueDate = x.ReportDueDate?.ToUniversalTime(),
                        RfidCode = x.RfidCode,
                        Status = x.Status
                    }).OrderBy(x =>
                        x.TestGroup switch
                        {
                            "Physics" => 0,
                            "Wet" => 1,
                            "Fiber" => 2,
                            "Flam" => 3,
                            _ => 4  // 其他group排在最后
                        })
                    .ToList()
                })
                .OrderByDescending(o => flat.Where(f => f.ReportNumber == o.ReportNumber)
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

            // 交集由 MergeResults 的 INNER JOIN 保证，此处只做 IsDelete 过滤
            var filteredInfo = infoQuery.Where(o => o.IsDelete == "N").ToList();
            var filteredSchedule = scheduleQuery.Where(o => o.IsDelete == "N").ToList();

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

                        // 构造 OrderLineOutput
                        var distinctGroups = g
                            .Select(d => new OrderLineOutput
                            {
                                LineId = d.Info.Id.ToString(),
                                Express = d.Info.Express ?? string.Empty,
                                TestGroup = d.Info.TestGroup ?? string.Empty,
                                SampleCount = d.Info.TestSampleNum ?? 0,
                                ItemCount = d.Info.TestItemNum ?? 0,
                                DelayType = d.Info.DelayType ?? string.Empty,
                                DelayReason = d.Info.DelayReason ?? string.Empty,
                                Remark = d.Info.Remark ?? string.Empty,
                                Reviewer = d.Info.Reviewer ?? string.Empty,
                                ReviewerId = _db.Users.FirstOrDefault(l => l.NickName == d.Info.Reviewer)?.UserId,
                                ReviewFinish = d.Schedule.ReviewFinishTime,
                                LabIn = d.Schedule.OrderInTime ?? DateTimeOffset.Now,
                                DueDate = d.Schedule.ReportDueDate,
                                LabOut = d.Schedule.LabOutTime,
                                RfidCode = d.Info.RfidCode,
                                Status = d.Info.Status switch
                                {
                                    1 => "Entry Complete",
                                    2 => "Review Finished",
                                    3 => "In Lab",
                                    4 => "Test Done",
                                    5 => "Report Out",
                                    _ => "Unknown"
                                }
                            })
                            .Distinct()
                            .OrderBy(x => x.TestGroup switch
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
                            ReportNumber = g.Key,
                            OrderEntryPerson = first.Info.OrderEntryPerson ?? string.Empty,
                            OrderEntryPersonId = _db.Users.FirstOrDefault(l => l.NickName == first.Info.OrderEntryPerson)?.UserId,
                            CustomerServiceName = first.Info.CustomerService ?? string.Empty,
                            CustomerServiceId = _db.CustomerServices.FirstOrDefault(l => l.CustomerService1 == first.Info.CustomerService)?.Id,
                            TestGroups = string.Join(",", distinctGroups.Select(dg => dg.TestGroup).Distinct()),
                            Lines = distinctGroups
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
                        LineId = d.Info.Id.ToString(),
                        ReportNumber = d.Info.ReportNumber ?? string.Empty,
                        OrderEntryPerson = d.Info.OrderEntryPerson ?? string.Empty,
                        OrderEntryPersonId = _db.Users.FirstOrDefault(l => l.NickName == d.Info.OrderEntryPerson)?.UserId,
                        Express = d.Info.Express ?? string.Empty,
                        CustomerServiceName = d.Info.CustomerService ?? string.Empty,
                        CustomerServiceId = _db.CustomerServices.FirstOrDefault(l => l.CustomerService1 == d.Info.CustomerService)?.Id,
                        TestGroup = d.Info.TestGroup ?? string.Empty,
                        ReviewFinish = d.Schedule.ReviewFinishTime,
                        Reviewer = d.Info.Reviewer ?? string.Empty,
                        ReviewerId = _db.Users.FirstOrDefault(l => l.NickName == d.Info.Reviewer)?.UserId,
                        DueDate = d.Schedule.ReportDueDate,
                        //switch{
                        //    DateTimeOffset offset => DateOnly.FromDateTime(offset.DateTime.Date),
                        //    _ => DateOnly.FromDateTime(DateTime.UtcNow.Date)
                        //},
                        LabIn = d.Schedule.OrderInTime ?? DateTimeOffset.Now,
                        LabOut = d.Schedule.LabOutTime,
                        SampleCount = d.Info.TestSampleNum ?? 0,
                        ItemCount = d.Info.TestItemNum ?? 0,
                        DelayType = d.Info.DelayType ?? string.Empty,
                        DelayReason = d.Info.DelayReason ?? string.Empty,
                        Remark = d.Info.Remark ?? string.Empty,
                        RfidCode = d.Info.RfidCode,
                        Status = d.Info.Status switch
                        {
                            1 => "Entry Complete",
                            2 => "Review Finished",
                            3 => "In Lab",
                            4 => "Test Done",
                            5 => "Report Out",
                            _ => "Unknown"
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

        /// <summary>
        /// 订单报表参数筛选
        /// </summary>
        public async Task<OrderCardOutput> OrderCardAsync(DateTimeOffset time, string group, string timeType)
        {
            if (time == null) time = DateTimeOffset.Now;

            // 获取基础查询数据
            var infoQuery = _orderReportingQueryProvider.QueryGroupInfo(group, _db).ToList();

            // 获取所有查询类型
            var queryTypes = new[] { "needLabOut", "actuallyLabOut", "delayLabOut", "inAdvanceLabOut", "internalReasonDelay" };
            var queries = queryTypes.ToDictionary(
                type => type,
                type => _orderReportingQueryProvider.QuerySelect(time, timeType, type, _db)
                    .ToList()
            );

            // 计算交集和去重计数的通用方法
            Func<string, int> calculateCount = (type) =>
            {
                var query = queries[type];
                var queryIds = query.Select(q => q.Id).ToHashSet(); // 提取 query 中的 Id 并去重，提取字典中每个type的id并且去重

                if (group.ToLower() == "all")
                {
                    // 如果 group 是 "all"，则按 ReportNumber 分组并判断状态
                    var reportNumbers = infoQuery.GroupBy(fi => fi.ReportNumber)
                                                    .Select(g => new
                                                    {
                                                        ReportNumber = g.Key,
                                                        Status = DetermineStatus(g, queryIds, type)
                                                    })
                                                    .ToList();

                    return reportNumbers.Count(rn => rn.Status == type);
                }
                else
                {
                    // 否则直接计算去重后的数量
                    var intersection = infoQuery.Where(fi => queryIds.Contains(fi.Id)).Select(fi => fi.Id).Distinct();
                    return intersection.Count();
                }
            };

            // 判断单号状态的方法
            string DetermineStatus(IGrouping<string, LabTestInfo> group, HashSet<Guid> queryIds, string type)
            {
                var groupIds = group.Select(g => g.Id).ToHashSet();

                // 特殊处理 ActuallyLabOut
                if (type == "actuallyLabOut")
                {
                    // 只有当所有 Id 都是 actuallyLabOut 时，单号才算作 actuallyLabOut
                    if (groupIds.All(id => queryIds.Contains(id)))
                    {
                        return "actuallyLabOut";
                    }
                }

                if (type == "delayLabOut")
                {
                    // 如果有任何一个 Id 是 delay，整体算作 delay
                    if (groupIds.Any(id => queryIds.Contains(id) && queries["delayLabOut"].Any(q => q.Id == id)))
                    {
                        return "delayLabOut";
                    }
                }

                if (type == "inAdvanceLabOut")
                {
                    // 如果所有 Id 都是 inAdvance，整体算作 inAdvance
                    if (groupIds.All(id => queryIds.Contains(id) && queries["inAdvanceLabOut"].Any(q => q.Id == id)))
                    {
                        return "inAdvanceLabOut";
                    }
                }
                if (type == "needLabOut")
                {
                    // 如果有任何一个 Id 是 needLabOut，整体算作 needLabOut
                    if (groupIds.Any(id => queryIds.Contains(id) && queries["needLabOut"].Any(q => q.Id == id)))
                    {
                        return "needLabOut";
                    }
                }

                if (type == "internalReasonDelay")
                {
                    // 如果有任何一个 Id 是 delay，整体算作 delay
                    if (groupIds.Any(id => queryIds.Contains(id) && queries["internalReasonDelay"].Any(q => q.Id == id)))
                    {
                        return "internalReasonDelay";
                    }
                }
                return null;
            }

            var TimeQuery = _orderReportingQueryProvider.QuerySelect(time, timeType, "needLabOut", _db).ToList();
            var info = infoQuery.Where(x => TimeQuery.Any(y => x.Id == y.Id)).ToList();//取出时间与小组的交集

            // 计算 交集对应的 NumOfSample 总和
            var xTotalNumOfSamples = info.Sum(item => item.TestSampleNum ?? 0);


            // 构建输出结果
            var CardOutput = new OrderCardOutput
            {
                NeedLabOut = calculateCount("needLabOut"),
                ActuallyLabOut = calculateCount("actuallyLabOut"),
                DelayLabOut = calculateCount("delayLabOut"),
                InAdvanceLabOut = calculateCount("inAdvanceLabOut"),
                InternalReasonDelay = calculateCount("internalReasonDelay"),
                NumOfSample = xTotalNumOfSamples
            };

            return CardOutput;
        }


        /// <summary>
        /// 扇形图
        /// </summary>
        /// <param name="duedate"></param>
        /// <param name="labindate"></param>
        /// <returns></returns>
        public async Task<OrderFanCardOutput> OrderfanCardAsync(DateTimeOffset time, string group, string timeType)
        {
            var infoQuery = _orderReportingQueryProvider.QueryGroupInfo(group, _db).ToList();

            // 获取所有查询类型
            var queryTypes = new[] { "needLabOut", "delayLabOut", "inAdvanceLabOut", "Unknow", "inDueDate", "internalReasonDelay" };
            var queries = queryTypes.ToDictionary(
                type => type,
                type => _orderReportingQueryProvider.QuerySelect(time, timeType, type, _db)
                    .ToList()
            );
            if (queries["needLabOut"].Count() == 0) return new OrderFanCardOutput { Delay = 0, InAdvance = 0, Normal = 0 };
            // 计算交集和去重计数的通用方法
            Func<string, int> calculateCount = (type) =>
            {
                var query = queries[type];
                var queryIds = query.Select(q => q.Id).ToHashSet(); // 提取 query 中的 Id 并去重

                if (group.ToLower() == "all")
                {
                    // 如果 group 是 "all"，则按 ReportNumber 分组并判断状态
                    var reportNumbers = infoQuery.GroupBy(fi => fi.ReportNumber)
                                                    .Select(g => new
                                                    {
                                                        ReportNumber = g.Key,
                                                        Status = DetermineStatus(g, queryIds, type)
                                                    })
                                                    .ToList();

                    return reportNumbers.Count(rn => rn.Status == type);
                }
                else
                {
                    // 否则直接计算去重后的数量
                    var intersection = infoQuery.Where(fi => queryIds.Contains(fi.Id)).Select(fi => fi.Id).Distinct();
                    return intersection.Count();
                }
            };

            // 判断单号状态的方法
            string DetermineStatus(IGrouping<string, LabTestInfo> group, HashSet<Guid> queryIds, string type)
            {
                var groupIds = group.Select(g => g.Id).ToHashSet();

                // 特殊处理 ActuallyLabOut
                if (type == "actuallyLabOut")
                {
                    // 只有当所有 Id 都是 actuallyLabOut 时，单号才算作 actuallyLabOut
                    if (groupIds.All(id => queryIds.Contains(id)))
                    {
                        return "actuallyLabOut";
                    }
                }

                if (type == "delayLabOut")
                {
                    // 如果有任何一个 Id 是 delay，整体算作 delay
                    if (groupIds.Any(id => queryIds.Contains(id) && queries["delayLabOut"].Any(q => q.Id == id)))
                    {
                        return "delayLabOut";
                    }
                }

                if (type == "inAdvanceLabOut")
                {
                    // 如果所有 Id 都是 inAdvance，整体算作 inAdvance
                    if (groupIds.All(id => queryIds.Contains(id) && queries["inAdvanceLabOut"].Any(q => q.Id == id)))
                    {
                        return "inAdvanceLabOut";
                    }
                }
                if (type == "needLabOut")
                {
                    // 如果有任何一个 Id 是 needLabOut，整体算作 needLabOut
                    if (groupIds.Any(id => queryIds.Contains(id) && queries["needLabOut"].Any(q => q.Id == id)))
                    {
                        return "needLabOut";
                    }
                }
                if (type == "Unknow")
                {
                    // 如果有任何一个 Id 是 Unknow，整体算作 Unknow
                    if (groupIds.Any(id => queryIds.Contains(id) && queries["Unknow"].Any(q => q.Id == id)))
                    {
                        return "Unknow";
                    }
                }
                if (type == "inDueDate")
                {
                    // 如果有任何一个 Id 是 inDueDate，整体算作 inDueDate
                    if (groupIds.Any(id => queryIds.Contains(id) && queries["inDueDate"].Any(q => q.Id == id)))
                    {
                        return "inDueDate";
                    }
                }
                if (type == "internalReasonDelay")
                {
                    // 如果有任何一个 Id 是 internalReasonDelay，整体算作 internalReasonDelay
                    if (groupIds.Any(id => queryIds.Contains(id) && queries["internalReasonDelay"].Any(q => q.Id == id)))
                    {
                        return "internalReasonDelay";
                    }
                }

                return null;
            }
            return new OrderFanCardOutput
            {
                Delay = calculateCount("delayLabOut"),
                InAdvance = calculateCount("inAdvanceLabOut"),
                InDueDate = calculateCount("inDueDate"),
                Unknown = calculateCount("Unknow"),
                InternalReasonDelay = calculateCount("internalReasonDelay"),
                Normal = calculateCount("inDueDate") + calculateCount("inAdvanceLabOut")
            };
        }



        /// <summary>
        /// 折线图、柱状图
        /// </summary>
        /// <param name="duedate"></param>
        /// <param name="labindate"></param>
        /// <returns></returns>
        public async Task<OrderLineCardOutput> OrderLineChartAsync(DateTimeOffset[] time, string group, string timeType, string Type)
        {
            if (time == null)
                time = new[] { DateTimeOffset.Now };

            // 获取基础查询数据
            var infoQuery = _orderReportingQueryProvider.QueryGroupInfo(group, _db);
            var filteredInfo = infoQuery.Where(o => o.IsDelete == "N").ToList();

            if (timeType.ToLower() == "month")
            {
                // 获取当前月份的第一天和最后一天
                var firstDayOfMonth = new DateTimeOffset(time[0].Year, time[0].Month, 1, 0, 0, 0, time[0].Offset);
                var lastDayOfMonth = new DateTimeOffset(time[0].Year, time[0].Month, DateTime.DaysInMonth(time[0].Year, time[0].Month), 23, 59, 59, time[0].Offset);

                // 创建1到31天的数组作为TimePropertyName
                var dayNames = Enumerable.Range(1, 31).ToArray();

                // 初始化TimeProperty，包含当前月份的统计结果
                var timeProperty = new List<TimePropertyValue>();

                // 初始化当前月份的统计结果数组
                var monthlyValues = new int[31];

                switch (Type.ToLower())
                {
                    case "all":
                        // 获取当前月份的所有数据
                        var monthlyData = filteredInfo.Where(o =>
                            o.ReportDueDate.HasValue &&
                            o.ReportDueDate.Value >= firstDayOfMonth &&
                            o.ReportDueDate.Value <= lastDayOfMonth
                        ).ToList();

                        // 如果 group 为 "All", 按 ReportNumber 去重
                        if (group.ToLower() == "all")
                        {
                            monthlyData = monthlyData
                                .GroupBy(o => new { o.ReportNumber, o.ReportDueDate })
                                .Select(g => g.First()) // 或者 g.ToList() 中的任意一条记录
                                .ToList();
                        }

                        // 按日期分组统计，每个 reportnumber 在每个日期中只计数一次
                        var result = monthlyData
                            .GroupBy(o => o.ReportDueDate!.Value.Date)
                            .Select(g => new
                            {
                                Day = g.Key.Day - 1,
                                Count = g.Select(o => o.ReportNumber).Distinct().Count()
                            }).ToList();

                        foreach (var item in result)
                        {
                            monthlyValues[item.Day] = item.Count;
                        }

                        break;
                    case "delay":
                        // 添加delay条件
                        var delayQuery = filteredInfo.Where(o =>
                            (!o.LabOutTime.HasValue ?
                                o.ReportDueDate!.Value.AddDays(1) < DateTime.Now :
                                o.LabOutTime.Value > o.ReportDueDate!.Value.AddDays(1)) ||
                            (o.DelayReason != null || o.DelayType != null)
                            && o.ReportDueDate.HasValue &&
                            o.ReportDueDate.Value >= firstDayOfMonth &&
                            o.ReportDueDate.Value <= lastDayOfMonth
                        ).ToList();
                        // 如果 group 为 "All"，对 delayQuery 按 ReportNumber 去重
                        if (group.ToLower() == "all")
                        {
                            delayQuery = delayQuery
                                .GroupBy(o => new { o.ReportNumber, o.ReportDueDate })
                                .Select(g => g.First()) // 或者 g.ToList() 中的任意一条记录
                                .ToList();
                        }

                        // 按日期分组统计，每个 reportnumber 在每个日期中只计数一次
                        var delay = delayQuery
                            .GroupBy(o => o.ReportDueDate!.Value.Date)
                            .Select(g => new
                            {
                                Day = g.Key.Day - 1,
                                Count = g.Select(o => o.ReportNumber).Distinct().Count()
                            }).ToList();

                        foreach (var item in delay)
                        {
                            monthlyValues[item.Day] = item.Count;
                        }
                        break;
                    case "normal":
                        var normalQuery = filteredInfo.Where(
                            o => o.LabOutTime.HasValue &&
                            o.ReportDueDate.HasValue &&
                            (o.LabOutTime.Value.Date == o.ReportDueDate.Value.Date || o.LabOutTime < o.ReportDueDate.Value.Date) &&
                            o.ReportDueDate.Value >= firstDayOfMonth &&
                            o.ReportDueDate.Value <= lastDayOfMonth).ToList();

                        // 如果 group 为 "All"，确保每个 reportnumber 的所有 group 都满足 normal 条件
                        if (group.ToLower() == "all")
                        {
                            var reportNumbersWithAllGroupsnormal = normalQuery
                                .GroupBy(o => o.ReportNumber)
                                .Where(g => g.Count() == filteredInfo.Where(f => f.ReportNumber == g.Key).Select(f => f.TestGroup).Distinct().Count())
                                .Select(g => g.Key)
                                .ToList();

                            normalQuery = normalQuery
                                .Where(o => reportNumbersWithAllGroupsnormal.Contains(o.ReportNumber))
                                .ToList();
                        }

                        // 按日期分组统计，每个 reportnumber 在每个日期中只计数一次
                        var normal = normalQuery
                            .GroupBy(o => o.ReportDueDate!.Value.Date)
                            .Select(g => new
                            {
                                Day = g.Key.Day - 1,
                                Count = g.Select(o => o.ReportNumber).Distinct().Count()
                            }).ToList();

                        foreach (var item in normal)
                        {
                            monthlyValues[item.Day] = item.Count;
                        }

                        break;
                    case "inadvance":
                        var advanceQuery = filteredInfo.Where(
                            o => o.LabOutTime.HasValue &&
                            o.ReportDueDate.HasValue &&
                            (o.LabOutTime.Value.Date.AddDays(1) == o.ReportDueDate.Value.Date
                            || o.LabOutTime.Value.Date.AddDays(1) < o.ReportDueDate.Value.Date) &&
                            o.ReportDueDate.Value >= firstDayOfMonth &&
                            o.ReportDueDate.Value <= lastDayOfMonth
                        ).ToList();

                        // 如果 group 为 "All"，确保每个 reportnumber 的所有 group 都满足 advance 条件
                        if (group.ToLower() == "all")
                        {
                            var reportNumbersWithAllGroupsAdvance = advanceQuery
                                .GroupBy(o => o.ReportNumber)
                                .Where(g => g.Count() == filteredInfo.Where(f => f.ReportNumber == g.Key).Select(f => f.TestGroup).Distinct().Count())
                                .Select(g => g.Key)
                                .ToList();

                            advanceQuery = advanceQuery
                                .Where(o => reportNumbersWithAllGroupsAdvance.Contains(o.ReportNumber))
                                .ToList();
                        }

                        // 按日期分组统计，每个 reportnumber 在每个日期中只计数一次
                        var advance = advanceQuery
                            .GroupBy(o => o.ReportDueDate!.Value.Date)
                            .Select(g => new
                            {
                                Day = g.Key.Day - 1,
                                Count = g.Select(o => o.ReportNumber).Distinct().Count()
                            }).ToList();

                        foreach (var item in advance)
                        {
                            monthlyValues[item.Day] = item.Count;
                        }
                        break;
                }

                // 创建TimePropertyValue对象
                var timePropertyValue = new TimePropertyValue
                {
                    TimeHead = $"{time[0].Month}月", // 月份名称
                    TimeValue = monthlyValues // 当月的统计结果
                };

                // 添加到timeProperty列表
                timeProperty.Add(timePropertyValue);

                // 创建OrderLineCardOutput对象
                return new OrderLineCardOutput
                {
                    TimePropertyName = dayNames, // 日期名称
                    TimeProperty = timeProperty // 当月的统计结果
                };
            }
            else if (timeType.ToLower() == "year")
            {
                // 获取当前年份的第一天和最后一天
                var firstDayOfYear = new DateTimeOffset(time[0].Year, 1, 1, 0, 0, 0, time[0].Offset);
                var lastDayOfYear = new DateTimeOffset(time[0].Year, 12, 31, 23, 59, 59, time[0].Offset);

                // 创建1到12月的数组作为TimePropertyName
                var monthNames = Enumerable.Range(1, 12).ToArray();

                // 初始化TimeProperty，包含当前年份的统计结果
                var timeProperty = new List<TimePropertyValue>();

                // 初始化当前年份的统计结果数组
                var yearlyValues = new int[12];

                switch (Type.ToLower())
                {
                    case "all":
                        // 获取当前年份的所有数据
                        var yearlyData = filteredInfo.Where(o =>
                            o.ReportDueDate.HasValue &&
                            o.ReportDueDate.Value >= firstDayOfYear &&
                            o.ReportDueDate.Value <= lastDayOfYear
                        ).ToList();

                        // 如果 group 为 "All", 按 ReportNumber 去重
                        if (group.ToLower() == "all")
                        {
                            yearlyData = yearlyData
                                .GroupBy(o => new { o.ReportNumber, o.ReportDueDate })
                                .Select(g => g.First())
                                .ToList();
                        }

                        // 按月份分组统计，每个 reportnumber 在每个月份中只计数一次
                        var result = yearlyData
                            .GroupBy(o => o.ReportDueDate!.Value.Month - 1) // 转换为0-based索引
                            .Select(g => new
                            {
                                Month = g.Key,
                                Count = g.Select(o => o.ReportNumber).Distinct().Count()
                            }).ToList();

                        foreach (var item in result)
                        {
                            yearlyValues[item.Month] = item.Count;
                        }

                        break;
                    case "delay":
                        // 添加delay条件
                        var delayQuery = filteredInfo.Where(o =>
                            (!o.LabOutTime.HasValue ?
                                o.ReportDueDate!.Value.AddDays(1) < DateTime.Now :
                                o.LabOutTime.Value > o.ReportDueDate!.Value.AddDays(1)) ||
                            (o.DelayReason != null || o.DelayType != null)
                            && o.ReportDueDate.HasValue &&
                            o.ReportDueDate.Value >= firstDayOfYear &&
                            o.ReportDueDate.Value <= lastDayOfYear
                        ).ToList();

                        // 如果 group 为 "All"，对 delayQuery 按 ReportNumber 去重
                        if (group.ToLower() == "all")
                        {
                            delayQuery = delayQuery
                                .GroupBy(o => new { o.ReportNumber, o.ReportDueDate })
                                .Select(g => g.First())
                                .ToList();
                        }

                        // 按月份分组统计，每个 reportnumber 在每个月份中只计数一次
                        var delay = delayQuery
                            .GroupBy(o => o.ReportDueDate!.Value.Month - 1)
                            .Select(g => new
                            {
                                Month = g.Key,
                                Count = g.Select(o => o.ReportNumber).Distinct().Count()
                            }).ToList();
                        foreach (var item in delay)
                        {
                            yearlyValues[item.Month] = item.Count;
                        }
                        break;
                    case "normal":
                        var normalQuery = filteredInfo.Where(
                            o => o.LabOutTime.HasValue &&
                            o.ReportDueDate.HasValue &&
                             (o.LabOutTime.Value.Date == o.ReportDueDate.Value.Date || o.LabOutTime < o.ReportDueDate.Value.Date) &&
                            o.ReportDueDate.Value >= firstDayOfYear &&
                            o.ReportDueDate.Value <= lastDayOfYear).ToList();

                        // 如果 group 为 "All"，确保每个 reportnumber 的所有 group 都满足 normal 条件
                        if (group.ToLower() == "all")
                        {
                            var reportNumbersWithAllGroupsNormal = normalQuery
                                .GroupBy(o => o.ReportNumber)
                                .Where(g => g.Count() == filteredInfo.Where(f => f.ReportNumber == g.Key).Select(f => f.TestGroup).Distinct().Count())
                                .Select(g => g.Key)
                                .ToList();

                            normalQuery = normalQuery
                                .Where(o => reportNumbersWithAllGroupsNormal.Contains(o.ReportNumber))
                                .ToList();
                        }

                        // 按月份分组统计，每个 reportnumber 在每个月份中只计数一次
                        var normal = normalQuery
                            .GroupBy(o => o.ReportDueDate!.Value.Month - 1)
                            .Select(g => new
                            {
                                Month = g.Key,
                                Count = g.Select(o => o.ReportNumber).Distinct().Count()
                            }).ToList();

                        foreach (var item in normal)
                        {
                            yearlyValues[item.Month] = item.Count;
                        }

                        break;
                    case "inadvance":
                        var advanceQuery = filteredInfo.Where(
                            o => o.LabOutTime.HasValue &&
                            o.ReportDueDate.HasValue &&
                            (o.LabOutTime.Value.Date.AddDays(1) == o.ReportDueDate.Value.Date
                            || o.LabOutTime.Value.Date.AddDays(1) < o.ReportDueDate.Value.Date) &&
                            o.ReportDueDate.Value >= firstDayOfYear &&
                            o.ReportDueDate.Value <= lastDayOfYear
                        ).ToList();


                        // 如果 group 为 "All"，确保每个 reportnumber 的所有 group 都满足 advance 条件
                        if (group.ToLower() == "all")
                        {
                            var reportNumbersWithAllGroupsAdvance = advanceQuery
                                .GroupBy(o => o.ReportNumber)
                                .Where(g => g.Count() == filteredInfo.Where(f => f.ReportNumber == g.Key).Select(f => f.TestGroup).Distinct().Count())
                                .Select(g => g.Key)
                                .ToList();

                            advanceQuery = advanceQuery
                                .Where(o => reportNumbersWithAllGroupsAdvance.Contains(o.ReportNumber))
                                .ToList();
                        }

                        // 按月份分组统计，每个 reportnumber 在每个月份中只计数一次
                        var advance = advanceQuery
                            .GroupBy(o => o.ReportDueDate!.Value.Month - 1)
                            .Select(g => new
                            {
                                Month = g.Key,
                                Count = g.Select(o => o.ReportNumber).Distinct().Count()
                            }).ToList();


                        foreach (var item in advance)
                        {
                            yearlyValues[item.Month] = item.Count;
                        }
                        break;
                }

                // 创建TimePropertyValue对象
                var timePropertyValue = new TimePropertyValue
                {
                    TimeHead = $"{time[0].Year}年", // 年份名称
                    TimeValue = yearlyValues // 当年的统计结果
                };

                // 添加到timeProperty列表
                timeProperty.Add(timePropertyValue);

                // 创建OrderLineCardOutput对象
                return new OrderLineCardOutput
                {
                    TimePropertyName = monthNames, // 月份名称
                    TimeProperty = timeProperty // 当年的统计结果
                };
            }
            else if (timeType.ToLower() == "allyear")
            {
                // 创建过去5年的数组作为TimePropertyName
                var currentYear = DateTimeOffset.Now.Year;
                var yearNames = Enumerable.Range(currentYear - 4, 5).ToArray();

                // 初始化TimeProperty，包含每一年的统计结果
                var timeProperty = new List<TimePropertyValue>();

                // 初始化每一年的统计结果数组
                var yearlyValues = new int[5];

                switch (Type.ToLower())
                {
                    case "all":
                        // 获取过去5年的所有数据
                        var yearlyData = filteredInfo.Where(o =>
                            o.ReportDueDate.HasValue &&
                            o.ReportDueDate.Value.Year >= currentYear - 4 &&
                            o.ReportDueDate.Value.Year <= currentYear
                        ).ToList();

                        // 按年份分组统计
                        var result = yearlyData
                            .GroupBy(o => o.ReportDueDate!.Value.Year)
                            .Select(g => new
                            {
                                Year = g.Key,
                                Count = g.Count()
                            }).ToList();

                        foreach (var item in result)
                        {
                            var index = item.Year - (currentYear - 4);
                            if (index >= 0 && index < 5)
                            {
                                yearlyValues[index] = item.Count;
                            }
                        }

                        break;
                    case "delay":
                        // 添加delay条件
                        var delayQuery = filteredInfo.Where(o =>
                           (!o.LabOutTime.HasValue ?
                                o.ReportDueDate!.Value.AddDays(1) < DateTime.Now :
                                o.LabOutTime.Value > o.ReportDueDate!.Value.AddDays(1)) ||
                            (o.DelayReason != null || o.DelayType != null)
                            && o.ReportDueDate.HasValue &&
                            o.ReportDueDate.Value.Year >= currentYear - 4 &&
                            o.ReportDueDate.Value.Year <= currentYear
                        ).ToList();

                        // 按年份分组统计
                        var delay = delayQuery
                            .GroupBy(o => o.ReportDueDate!.Value.Year)
                            .Select(g => new
                            {
                                Year = g.Key,
                                Count = g.Count()
                            }).ToList();
                        foreach (var item in delay)
                        {
                            var index = item.Year - (currentYear - 4);
                            if (index >= 0 && index < 5)
                            {
                                yearlyValues[index] = item.Count;
                            }
                        }
                        break;
                    case "normal":
                        var normalQuery = filteredInfo.Where(
                            o => o.LabOutTime.HasValue &&
                            o.ReportDueDate.HasValue &&
                            (o.LabOutTime.Value.Date == o.ReportDueDate.Value.Date || o.LabOutTime < o.ReportDueDate.Value.Date) &&
                            o.ReportDueDate.Value.Year >= currentYear - 4 &&
                            o.ReportDueDate.Value.Year <= currentYear).ToList();

                        var normal = normalQuery
                            .GroupBy(o => o.ReportDueDate!.Value.Year)
                            .Select(g => new
                            {
                                Year = g.Key,
                                Count = g.Count()
                            }).ToList();

                        foreach (var item in normal)
                        {
                            var index = item.Year - (currentYear - 4);
                            if (index >= 0 && index < 5)
                            {
                                yearlyValues[index] = item.Count;
                            }
                        }

                        break;
                    case "inadvance":
                        var inadvanceQuery = filteredInfo.Where(o => o.LabOutTime.HasValue &&
                            o.ReportDueDate.HasValue &&
                            (o.LabOutTime.Value.Date.AddDays(1) == o.ReportDueDate.Value.Date
                            || o.LabOutTime.Value.Date.AddDays(1) < o.ReportDueDate.Value.Date) &&
                            o.ReportDueDate.Value.Year >= currentYear - 4 &&
                            o.ReportDueDate.Value.Year <= currentYear
                        ).ToList();
                        var inadvance = inadvanceQuery.GroupBy(o => o.ReportDueDate!.Value.Year)
                            .Select(g => new
                            {
                                Year = g.Key,
                                Count = g.Count()
                            }).ToList();

                        foreach (var item in inadvance)
                        {
                            var index = item.Year - (currentYear - 4);
                            if (index >= 0 && index < 5)
                            {
                                yearlyValues[index] = item.Count;
                            }
                        }
                        break;
                }

                // 创建TimePropertyValue对象
                var timePropertyValue = new TimePropertyValue
                {
                    TimeHead = "过去5年", // 时间范围名称
                    TimeValue = yearlyValues // 过去5年的统计结果
                };

                // 添加到timeProperty列表
                timeProperty.Add(timePropertyValue);

                // 创建OrderLineCardOutput对象
                return new OrderLineCardOutput
                {
                    TimePropertyName = yearNames, // 年份名称
                    TimeProperty = timeProperty // 过去5年的统计结果
                };
            }
            else if (timeType.ToLower() == "months")
            {
                // 初始化TimeProperty，包含每个月的统计结果
                var timeProperty = new List<TimePropertyValue>();

                foreach (var t in time)
                {
                    // 获取当前月份的第一天和最后一天
                    var firstDayOfMonth = new DateTimeOffset(t.Year, t.Month, 1, 0, 0, 0, t.Offset);
                    var lastDayOfMonth = new DateTimeOffset(t.Year, t.Month, DateTime.DaysInMonth(t.Year, t.Month), 23, 59, 59, t.Offset);

                    var monthlyValues = new int[31];

                    switch (Type.ToLower())
                    {
                        case "all":
                            // 获取当前月份的所有数据
                            var monthlyData = filteredInfo.Where(o =>
                                o.ReportDueDate.HasValue &&
                                o.ReportDueDate.Value >= firstDayOfMonth &&
                                o.ReportDueDate.Value <= lastDayOfMonth
                            ).ToList();

                            // 按日期分组统计
                            var result = monthlyData
                                .GroupBy(o => o.ReportDueDate!.Value.Date)
                                .Select(g => new
                                {
                                    Day = g.Key.Day - 1, // 转换为0-based索引
                                    Count = g.Count()
                                }).ToList();

                            foreach (var item in result)
                            {
                                monthlyValues[item.Day] = item.Count;
                            }
                            break;
                        case "delay":
                            // 添加delay条件
                            var delayQuery = filteredInfo.Where(o =>
                                (!o.LabOutTime.HasValue ?
                                    o.ReportDueDate!.Value.AddDays(1) < DateTime.Now :
                                    o.LabOutTime.Value > o.ReportDueDate!.Value.AddDays(1)) ||
                            (o.DelayReason != null || o.DelayType != null)
                                && o.ReportDueDate.HasValue &&
                                o.ReportDueDate.Value >= firstDayOfMonth &&
                                o.ReportDueDate.Value <= lastDayOfMonth
                            ).ToList();

                            // 按日期分组统计
                            var delay = delayQuery
                                .GroupBy(o => o.ReportDueDate!.Value.Date)
                                .Select(g => new
                                {
                                    Day = g.Key.Day - 1,
                                    Count = g.Count()
                                }).ToList();
                            foreach (var item in delay)
                            {
                                monthlyValues[item.Day] = item.Count;
                            }
                            break;
                        case "normal":
                            var normalQuery = filteredInfo.Where(
                                o => o.LabOutTime.HasValue &&
                                o.ReportDueDate.HasValue &&
                                (o.LabOutTime.Value.Date == o.ReportDueDate.Value.Date || o.LabOutTime < o.ReportDueDate.Value.Date) &&
                                o.ReportDueDate.Value >= firstDayOfMonth &&
                                o.ReportDueDate.Value <= lastDayOfMonth).ToList();

                            var normal = normalQuery
                                .GroupBy(o => o.ReportDueDate!.Value.Date)
                                .Select(g => new
                                {
                                    Day = g.Key.Day - 1,
                                    Count = g.Count()
                                }).ToList();

                            foreach (var item in normal)
                            {
                                monthlyValues[item.Day] = item.Count;
                            }
                            break;
                        case "inadvance":
                            var advanceQuery = filteredInfo.Where(
                                o => o.LabOutTime.HasValue &&
                                o.ReportDueDate.HasValue &&
                               (o.LabOutTime.Value.Date.AddDays(1) == o.ReportDueDate.Value.Date
                               || o.LabOutTime.Value.Date.AddDays(1) < o.ReportDueDate.Value.Date) &&
                                o.ReportDueDate.Value >= firstDayOfMonth &&
                                o.ReportDueDate.Value <= lastDayOfMonth
                            ).ToList();

                            // 按日期分组统计
                            var advance = advanceQuery
                                .GroupBy(o => o.ReportDueDate!.Value.Date)
                                .Select(g => new
                                {
                                    Day = g.Key.Day - 1,
                                    Count = g.Count()
                                }).ToList();

                            foreach (var item in advance)
                            {
                                monthlyValues[item.Day] = item.Count;
                            }
                            break;
                    }

                    // 创建TimePropertyValue对象
                    var timePropertyValue = new TimePropertyValue
                    {
                        TimeHead = $"{t.Month}月", // 月份名称
                        TimeValue = monthlyValues // 当月的统计结果
                    };

                    // 添加到timeProperty列表
                    timeProperty.Add(timePropertyValue);
                }

                // 创建OrderLineCardOutput对象
                var dayNames = Enumerable.Range(1, 31).ToArray();
                return new OrderLineCardOutput
                {
                    TimePropertyName = dayNames, // 日期名称
                    TimeProperty = timeProperty // 每个月的统计结果
                };
            }
            else if (timeType.ToLower() == "years")
            {
                // 初始化TimeProperty，包含每一年的统计结果
                var timeProperty = new List<TimePropertyValue>();

                foreach (var t in time)
                {
                    // 获取当前年份的第一天和最后一天
                    var firstDayOfYear = new DateTimeOffset(t.Year, 1, 1, 0, 0, 0, t.Offset);
                    var lastDayOfYear = new DateTimeOffset(t.Year, 12, 31, 23, 59, 59, t.Offset);

                    var yearlyValues = new int[12];

                    switch (Type.ToLower())
                    {
                        case "all":
                            // 获取当前年份的所有数据
                            var yearlyData = filteredInfo.Where(o =>
                                o.ReportDueDate.HasValue &&
                                o.ReportDueDate.Value >= firstDayOfYear &&
                                o.ReportDueDate.Value <= lastDayOfYear
                            ).ToList();

                            // 按月份分组统计
                            var result = yearlyData
                                .GroupBy(o => o.ReportDueDate!.Value.Month - 1) // 转换为0-based索引
                                .Select(g => new
                                {
                                    Month = g.Key,
                                    Count = g.Count()
                                }).ToList();

                            foreach (var item in result)
                            {
                                yearlyValues[item.Month] = item.Count;
                            }
                            break;
                        case "delay":
                            // 添加delay条件
                            var delayQuery = filteredInfo.Where(o =>
                                (!o.LabOutTime.HasValue ?
                                    o.ReportDueDate!.Value.AddDays(1) < DateTimeOffset.Now :
                                    o.LabOutTime.Value > o.ReportDueDate!.Value.AddDays(1)) ||
                            (o.DelayReason != null || o.DelayType != null)
                                && o.ReportDueDate.HasValue &&
                                o.ReportDueDate.Value >= firstDayOfYear &&
                                o.ReportDueDate.Value <= lastDayOfYear
                            ).ToList();

                            // 按月份分组统计
                            var delay = delayQuery
                                .GroupBy(o => o.ReportDueDate!.Value.Month - 1)
                                .Select(g => new
                                {
                                    Month = g.Key,
                                    Count = g.Count()
                                }).ToList();
                            foreach (var item in delay)
                            {
                                yearlyValues[item.Month] = item.Count;
                            }
                            break;
                        case "normal":
                            var normalQuery = filteredInfo.Where(
                                o => o.LabOutTime.HasValue &&
                                o.ReportDueDate.HasValue &&
                                (o.LabOutTime.Value.Date == o.ReportDueDate.Value.Date || o.LabOutTime < o.ReportDueDate.Value.Date) &&
                                o.ReportDueDate.Value >= firstDayOfYear &&
                                o.ReportDueDate.Value <= lastDayOfYear).ToList();

                            var normal = normalQuery
                                .GroupBy(o => o.ReportDueDate!.Value.Month - 1)
                                .Select(g => new
                                {
                                    Month = g.Key,
                                    Count = g.Count()
                                }).ToList();

                            foreach (var item in normal)
                            {
                                yearlyValues[item.Month] = item.Count;
                            }
                            break;
                        case "inadvance":
                            var advanceQuery = filteredInfo.Where(
                                o => o.LabOutTime.HasValue &&
                                o.ReportDueDate.HasValue &&
                               (o.LabOutTime.Value.Date.AddDays(1) == o.ReportDueDate.Value.Date
                               || o.LabOutTime.Value.Date.AddDays(1) < o.ReportDueDate.Value.Date) &&
                                o.ReportDueDate.Value >= firstDayOfYear &&
                                o.ReportDueDate.Value <= lastDayOfYear
                            ).ToList();

                            // 按月份分组统计
                            var advance = advanceQuery
                                .GroupBy(o => o.ReportDueDate!.Value.Month - 1)
                                .Select(g => new
                                {
                                    Month = g.Key,
                                    Count = g.Count()
                                }).ToList();

                            foreach (var item in advance)
                            {
                                yearlyValues[item.Month] = item.Count;
                            }
                            break;
                    }

                    // 创建TimePropertyValue对象
                    var timePropertyValue = new TimePropertyValue
                    {
                        TimeHead = $"{t.Year}年", // 年份名称
                        TimeValue = yearlyValues // 当年的统计结果
                    };

                    // 添加到timeProperty列表
                    timeProperty.Add(timePropertyValue);
                }

                // 创建OrderLineCardOutput对象
                var monthNames = Enumerable.Range(1, 12).ToArray();
                return new OrderLineCardOutput
                {
                    TimePropertyName = monthNames, // 月份名称
                    TimeProperty = timeProperty // 每一年的统计结果
                };
            }
            return null;
        }


    }
}

