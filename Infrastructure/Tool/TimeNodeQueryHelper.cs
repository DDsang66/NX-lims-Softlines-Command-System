using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;

namespace NX_lims_Softlines_Command_System.Infrastructure.Tool
{
    public class TimeNodeQueryHelper
    {
        /// <summary>
        /// 根据时间范围类型和时间选项构建查询条件
        /// </summary>
        /// <param name="query">原始查询</param>
        /// <param name="timeRange">时间范围，可以是单个时间或多个时间节点</param>
        /// <param name="timeOpt">时间选项（如labin、labout等）</param>
        /// <param name="timeType">时间类型（day、month、year）</param>
        /// <returns>应用了时间筛选条件的查询</returns>
        public static IQueryable<LabTestSchedule> ApplyTimeNodeFilter(
            IQueryable<LabTestSchedule> query,
            object timeRange,
            string timeOpt,
            string timeType)
        {
            if (timeRange == null || string.IsNullOrEmpty(timeOpt) || string.IsNullOrEmpty(timeType))
            {
                return query;
            }

            // 获取对应的时间属性表达式
            var dateProperty = GetDateProperty(timeOpt);
            if (dateProperty == null)
            {
                return query;
            }

            // 根据timeType处理不同的单时间命中
            if (timeType.ToLower().Contains("datetime"))
            {
                return ApplyDateTimeFilter(query, dateProperty, timeRange);//单时间多时间
            }
            else if (timeType.ToLower().Contains("date"))
            {
                return ApplyDayFilter(query, dateProperty, timeRange);
            }
            else if (timeType.ToLower().Contains("month"))
            {
                return ApplyMonthFilter(query, dateProperty, timeRange);
            }
            else if (timeType.ToLower().Contains("year"))
            {
                return ApplyYearFilter(query, dateProperty, timeRange);
            }
            else
            {
                return query;
            }
        }

        /// <summary>
        /// 获取对应的时间属性表达式
        /// </summary>
        private static Expression<Func<LabTestSchedule, DateTimeOffset?>> GetDateProperty(string timeOpt)
        {
            switch (timeOpt.ToLower())
            {
                case "labin":
                    return x => x.OrderInTime;
                case "labout":
                    return x => x.LabOutTime;
                case "reviewfinish":
                    return x => x.ReviewFinishTime;
                case "duedate":
                    return x => x.ReportDueDate;
                default:
                    return null;
            }
        }

        /// <summary>
        /// 处理datetime筛选
        /// </summary>
        private static IQueryable<LabTestSchedule> ApplyDateTimeFilter(
            IQueryable<LabTestSchedule> query,
            Expression<Func<LabTestSchedule, DateTimeOffset?>> dateProperty,
            object timeRange)
        {
            var timeRangeStr = timeRange?.ToString();
            if (string.IsNullOrEmpty(timeRangeStr))
                return query;

            if (timeRangeStr.StartsWith("[") && timeRangeStr.EndsWith("]"))
            {
                // 处理多个时间点 [时间1, 时间2, 时间3]
                var dateTimes = timeRangeStr.Trim('[').Trim(']').Split(',')
                    .Select(d =>
                    {
                        if (DateTimeOffset.TryParse(d.Trim('"'), out var date))
                        {
                            // 转换为东八区时间
                            return date.ToOffset(TimeSpan.FromHours(8));
                        }
                        return (DateTimeOffset?)null;
                    })
                    .Where(d => d.HasValue)
                    .Select(d => d.Value)
                    .ToList();

                if (dateTimes.Count > 0)
                {
                    // 获取属性名称
                    var propertyName = ((MemberExpression)dateProperty.Body).Member.Name;

                    // 使用Dynamic LINQ构建查询：属性值等于任意一个传入的时间
                    var conditions = string.Join(" || ", dateTimes.Select((_, index) => $"{propertyName} == @{index}"));
                    return query.Where(conditions, dateTimes.Cast<object>().ToArray());
                }
            }
            else
            {
                // 处理单个时间点
                if (DateTimeOffset.TryParse(timeRangeStr, out var dateTime))
                {
                    // 转换为东八区时间
                    var beijingTime = dateTime.ToOffset(TimeSpan.FromHours(8));

                    // 获取属性名称
                    var propertyName = ((MemberExpression)dateProperty.Body).Member.Name;

                    // 使用Dynamic LINQ构建查询：属性值等于传入的时间
                    return query.Where($"{propertyName} == @0", beijingTime);
                }
            }

            return query;
        }

        /// <summary>
        /// 处理日期节点筛选（精确匹配日期）
        /// </summary>
        private static IQueryable<LabTestSchedule> ApplyDayFilter(
            IQueryable<LabTestSchedule> query,
            Expression<Func<LabTestSchedule, DateTimeOffset?>> dateProperty,
            object timeRange)
        {
            var timeRangeStr = timeRange?.ToString();
            if (string.IsNullOrEmpty(timeRangeStr))
                return query;

            if (timeRangeStr.StartsWith("[") && timeRangeStr.EndsWith("]"))
            {
                // 处理多个日期节点 [日期1, 日期2, 日期3]
                var dates = timeRangeStr.Trim('[').Trim(']').Split(',')
                    .Select(d =>
                    {
                        if (DateTimeOffset.TryParse(d.Trim('"'), out var date))
                        {
                            // 转换为东八区时间的日期部分
                            return date.ToOffset(TimeSpan.FromHours(8)).Date;
                        }
                        return (DateTime?)null;
                    })
                    .Where(d => d.HasValue)
                    .Select(d => d.Value)
                    .Distinct()
                    .ToList();

                if (dates.Count > 0)
                {
                    // 获取属性名称
                    var propertyName = ((MemberExpression)dateProperty.Body).Member.Name;

                    // 构建查询条件：属性的日期部分等于任意一个传入的日期
                    var conditions = new List<string>();
                    var parameters = new List<object>();

                    for (int i = 0; i < dates.Count; i++)
                    {
                        // 每个日期创建一个日期范围条件
                        DateTimeOffset start = new DateTimeOffset(dates[i], TimeSpan.FromHours(8));
                        DateTimeOffset end = start.AddDays(1);

                        conditions.Add($"({propertyName} >= @{parameters.Count} && {propertyName} < @{parameters.Count + 1})");
                        parameters.Add(start);
                        parameters.Add(end);
                    }

                    var whereClause = string.Join(" || ", conditions);
                    return query.Where(whereClause, parameters.ToArray());
                }
            }
            else
            {
                // 处理单个日期节点
                if (DateTimeOffset.TryParse(timeRangeStr, out var dateTime))
                {
                    // 转换为东八区时间的日期部分
                    var beijingDate = dateTime.ToOffset(TimeSpan.FromHours(8)).Date;

                    // 创建日期范围（这一天的开始到第二天开始）
                    DateTimeOffset start = new DateTimeOffset(beijingDate, TimeSpan.FromHours(8));
                    DateTimeOffset end = start.AddDays(1);

                    // 获取属性名称
                    var propertyName = ((MemberExpression)dateProperty.Body).Member.Name;

                    // 使用Dynamic LINQ构建查询：属性日期部分等于传入的日期
                    return query.Where($"{propertyName} >= @0 && {propertyName} < @1", start, end);
                }
            }

            return query;
        }

        /// <summary>
        /// 处理月份节点筛选（精确匹配月份）
        /// </summary>
        private static IQueryable<LabTestSchedule> ApplyMonthFilter(
            IQueryable<LabTestSchedule> query,
            Expression<Func<LabTestSchedule, DateTimeOffset?>> dateProperty,
            object timeRange)
        {
            var timeRangeStr = timeRange?.ToString();
            if (string.IsNullOrEmpty(timeRangeStr))
                return query;

            if (timeRangeStr.StartsWith("[") && timeRangeStr.EndsWith("]"))
            {
                // 处理多个月份节点 [月份1, 月份2, 月份3]
                var monthInfos = timeRangeStr.Trim('[').Trim(']').Split(',')
                    .Select(m =>
                    {
                        if (DateTimeOffset.TryParse(m.Trim('"'), out var date))
                        {
                            // 转换为东八区时间并提取年月
                            var beijingTime = date.ToOffset(TimeSpan.FromHours(8));
                            return new { Year = beijingTime.Year, Month = beijingTime.Month };
                        }
                        return null;
                    })
                    .Where(m => m != null)
                    .Distinct()
                    .ToList();

                if (monthInfos.Count > 0)
                {
                    // 获取属性名称
                    var propertyName = ((MemberExpression)dateProperty.Body).Member.Name;

                    // 构建查询条件：属性的年月部分等于任意一个传入的月份
                    var conditions = new List<string>();
                    var parameters = new List<object>();

                    for (int i = 0; i < monthInfos.Count; i++)
                    {
                        var monthInfo = monthInfos[i];
                        // 每个月份创建一个月份范围条件
                        DateTimeOffset start = new DateTimeOffset(monthInfo.Year, monthInfo.Month, 1, 0, 0, 0, TimeSpan.FromHours(8));
                        DateTimeOffset end = start.AddMonths(1);

                        conditions.Add($"({propertyName} >= @{parameters.Count} && {propertyName} < @{parameters.Count + 1})");
                        parameters.Add(start);
                        parameters.Add(end);
                    }

                    var whereClause = string.Join(" || ", conditions);
                    return query.Where(whereClause, parameters.ToArray());
                }
            }
            else
            {
                // 处理单个月份节点
                if (DateTimeOffset.TryParse(timeRangeStr, out var dateTime))
                {
                    // 转换为东八区时间并提取年月
                    var beijingTime = dateTime.ToOffset(TimeSpan.FromHours(8));
                    var year = beijingTime.Year;
                    var month = beijingTime.Month;

                    // 创建月份范围（这个月的开始到下个月开始）
                    DateTimeOffset start = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.FromHours(8));
                    DateTimeOffset end = start.AddMonths(1);

                    // 获取属性名称
                    var propertyName = ((MemberExpression)dateProperty.Body).Member.Name;

                    // 使用Dynamic LINQ构建查询：属性年月部分等于传入的月份
                    return query.Where($"{propertyName} >= @0 && {propertyName} < @1", start, end);
                }
            }

            return query;
        }


        /// <summary>
        /// 处理年份节点筛选（精确匹配年份）
        /// </summary>
        private static IQueryable<LabTestSchedule> ApplyYearFilter(
            IQueryable<LabTestSchedule> query,
            Expression<Func<LabTestSchedule, DateTimeOffset?>> dateProperty,
            object timeRange)
        {
            var timeRangeStr = timeRange?.ToString();
            if (string.IsNullOrEmpty(timeRangeStr))
                return query;

            if (timeRangeStr.StartsWith("[") && timeRangeStr.EndsWith("]"))
            {
                // 处理多个年份节点 [年份1, 年份2, 年份3]
                var years = timeRangeStr.Trim('[').Trim(']').Split(',')
                    .Select(y =>
                    {
                        if (DateTimeOffset.TryParse(y.Trim('"'), out var date))
                        {
                            // 转换为东八区时间并提取年份
                            var beijingTime = date.ToOffset(TimeSpan.FromHours(8));
                            return beijingTime.Year;
                        }
                        return (int?)null;
                    })
                    .Where(y => y.HasValue)
                    .Select(y => y.Value)
                    .Distinct()
                    .ToList();

                if (years.Count > 0)
                {
                    // 获取属性名称
                    var propertyName = ((MemberExpression)dateProperty.Body).Member.Name;

                    // 构建查询条件：属性的年份部分等于任意一个传入的年份
                    var conditions = new List<string>();
                    var parameters = new List<object>();

                    for (int i = 0; i < years.Count; i++)
                    {
                        var year = years[i];
                        // 每个年份创建一个年份范围条件
                        DateTimeOffset start = new DateTimeOffset(year, 1, 1, 0, 0, 0, TimeSpan.FromHours(8));
                        DateTimeOffset end = start.AddYears(1);

                        conditions.Add($"({propertyName} >= @{parameters.Count} && {propertyName} < @{parameters.Count + 1})");
                        parameters.Add(start);
                        parameters.Add(end);
                    }

                    var whereClause = string.Join(" || ", conditions);
                    return query.Where(whereClause, parameters.ToArray());
                }
            }
            else
            {
                // 处理单个年份节点
                if (DateTimeOffset.TryParse(timeRangeStr, out var dateTime))
                {
                    // 转换为东八区时间并提取年份
                    var beijingTime = dateTime.ToOffset(TimeSpan.FromHours(8));
                    var year = beijingTime.Year;

                    // 创建年份范围（这一年的开始到下一年开始）
                    DateTimeOffset start = new DateTimeOffset(year, 1, 1, 0, 0, 0, TimeSpan.FromHours(8));
                    DateTimeOffset end = start.AddYears(1);

                    // 获取属性名称
                    var propertyName = ((MemberExpression)dateProperty.Body).Member.Name;

                    // 使用Dynamic LINQ构建查询：属性年份部分等于传入的年份
                    return query.Where($"{propertyName} >= @0 && {propertyName} < @1", start, end);
                }
            }

            return query;
        }
    }
}
