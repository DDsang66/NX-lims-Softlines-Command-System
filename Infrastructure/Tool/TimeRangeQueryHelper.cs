using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;

namespace NX_lims_Softlines_Command_System.Infrastructure.Tool
{
    public class TimeRangeQueryHelper
    {
        /// <summary>
        /// 根据时间范围类型和时间选项构建查询条件
        /// </summary>
        /// <param name="query">原始查询</param>
        /// <param name="timeRange">时间范围，可以是单个时间或时间范围数组</param>
        /// <param name="timeOpt">时间选项（如labin、labout等）</param>
        /// <param name="timeType">时间类型（day、month、year）</param>
        /// <returns>应用了时间筛选条件的查询</returns>
        public static IQueryable<LabTestInfo> ApplyTimeRangeFilter(
            IQueryable<LabTestInfo> query,
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

            // 根据timeType处理不同的时间范围
            if (timeType.ToLower().Contains("datetime"))
            {
                return ApplyDateTimeFilter(query, dateProperty, timeRange);
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
        private static Expression<Func<LabTestInfo, DateTimeOffset?>> GetDateProperty(string timeOpt)
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
        /// 处理datetime范围筛选
        /// </summary>
        private static IQueryable<LabTestInfo> ApplyDateTimeFilter(
            IQueryable<LabTestInfo> query,
            Expression<Func<LabTestInfo, DateTimeOffset?>> dateProperty,
            object timeRange)
        {
            var timeRangeStr = timeRange?.ToString();
            if (string.IsNullOrEmpty(timeRangeStr))
                return query;

            if (timeRangeStr.StartsWith("[") && timeRangeStr.EndsWith("]"))
            {
                // 处理日期时间范围 [开始时间, 结束时间]
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

                if (dateTimes.Count == 2)
                {
                    DateTimeOffset start = dateTimes[0];
                    DateTimeOffset end = dateTimes[1];

                    // 获取属性名称
                    var propertyName = ((MemberExpression)dateProperty.Body).Member.Name;

                    // 使用Dynamic LINQ构建查询
                    return query.Where($"{propertyName} >= @0 && {propertyName} <= @1", start, end);
                }
            }
            else
            {
                // 处理单个日期时间点（精确到分钟）
                if (DateTimeOffset.TryParse(timeRangeStr, out var dateTime))
                {
                    // 转换为东八区时间
                    var beijingTime = dateTime.ToOffset(TimeSpan.FromHours(8));

                    // 可以按需调整精度（分钟、小时等）
                    DateTimeOffset start = beijingTime;
                    DateTimeOffset end = beijingTime.AddMinutes(1); // 查询这一分钟内的数据

                    // 获取属性名称
                    var propertyName = ((MemberExpression)dateProperty.Body).Member.Name;

                    // 使用Dynamic LINQ构建查询
                    return query.Where($"{propertyName} >= @0 && {propertyName} < @1", start, end);
                }
            }

            return query;
        }

        /// <summary>
        /// 处理日范围筛选
        /// </summary>
        private static IQueryable<LabTestInfo> ApplyDayFilter(
            IQueryable<LabTestInfo> query,
            Expression<Func<LabTestInfo, DateTimeOffset?>> dateProperty,
            object timeRange)
        {
            var timeRangeStr = timeRange.ToString();
            if (timeRangeStr.StartsWith("[") && timeRangeStr.EndsWith("]"))
            {
                // 处理日期范围
                var dates = timeRangeStr.Trim('[').Trim(']').Split(',')
                    .Select(d => DateTimeOffset.Parse(d.Trim('"')))
                    .ToList();

                if (dates.Count == 2)
                {
                    DateTimeOffset start = dates[0].ToOffset(TimeSpan.FromHours(8)).Date; // 转换为东八区的日期
                    DateTimeOffset end = dates[1].ToOffset(TimeSpan.FromHours(8)).Date.AddDays(1); // 东八区下

                    // 获取属性名称
                    var propertyName = ((MemberExpression)dateProperty.Body).Member.Name;

                    // 使用Dynamic LINQ构建查询
                    return query.Where($"{propertyName} >= @0 && {propertyName} < @1", start, end);
                }
            }
            else
            {
                // 处理单个日期
                if (DateTime.TryParse(timeRangeStr, out var date))
                {
                    // 将DateTime转换为DateTimeOffset（东八区）
                    DateTimeOffset start = new DateTimeOffset(date.Date, TimeSpan.FromHours(8));
                    DateTimeOffset end = new DateTimeOffset(date.Date.AddDays(1), TimeSpan.FromHours(8));

                    // 获取属性名称
                    var propertyName = ((MemberExpression)dateProperty.Body).Member.Name;

                    // 使用Dynamic LINQ构建查询（使用 < end 而不是 <= end.AddTicks(-1)）
                    return query.Where($"{propertyName} >= @0 && {propertyName} < @1", start, end);
                }
            }

            return query;
        }

        /// <summary>
        /// 处理月范围筛选
        /// </summary>
        private static IQueryable<LabTestInfo> ApplyMonthFilter(
            IQueryable<LabTestInfo> query,
            Expression<Func<LabTestInfo, DateTimeOffset?>> dateProperty,
            object timeRange)
        {
            var timeRangeStr = timeRange?.ToString();
            if (string.IsNullOrEmpty(timeRangeStr))
                return query;

            if (timeRangeStr.StartsWith("[") && timeRangeStr.EndsWith("]"))
            {
                // 处理月份范围
                var months = timeRangeStr.Trim('[').Trim(']').Split(',')
                    .Select(m =>
                    {
                        if (DateTimeOffset.TryParse(m.Trim('"'), out var date))
                        {
                            // 转换为东八区时间
                            var beijingTime = date.ToOffset(TimeSpan.FromHours(8));
                            return new { Year = beijingTime.Year, Month = beijingTime.Month };
                        }
                        return null;
                    })
                    .Where(m => m != null)
                    .ToList();

                if (months.Count == 2)
                {
                    // 获取月份范围的开始和结束时间（东八区）
                    var startMonth = months[0];
                    var endMonth = months[1];

                    // 开始时间：第一个月的1号 00:00:00 东八区
                    DateTimeOffset start = new DateTimeOffset(startMonth.Year, startMonth.Month, 1, 0, 0, 0, TimeSpan.FromHours(8));

                    // 结束时间：结束月份的下个月1号 00:00:00 东八区
                    DateTimeOffset end = new DateTimeOffset(endMonth.Year, endMonth.Month, 1, 0, 0, 0, TimeSpan.FromHours(8))
                        .AddMonths(1);

                    // 获取属性名称
                    var propertyName = ((MemberExpression)dateProperty.Body).Member.Name;

                    // 使用Dynamic LINQ构建查询
                    return query.Where($"{propertyName} >= @0 && {propertyName} < @1", start, end);
                }
            }
            else
            {
                // 处理单个月份
                if (DateTimeOffset.TryParse(timeRangeStr, out var date))
                {
                    // 转换为东八区时间
                    var beijingTime = date.ToOffset(TimeSpan.FromHours(8));
                    var year = beijingTime.Year;
                    var month = beijingTime.Month;

                    // 月份的开始和结束时间（东八区）
                    DateTimeOffset start = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.FromHours(8));
                    DateTimeOffset end = start.AddMonths(1); // 下个月1号 00:00:00

                    // 获取属性名称
                    var propertyName = ((MemberExpression)dateProperty.Body).Member.Name;

                    // 使用Dynamic LINQ构建查询
                    return query.Where($"{propertyName} >= @0 && {propertyName} < @1", start, end);
                }
            }

            return query;
        }

        /// <summary>
        /// 处理年范围筛选
        /// </summary>
        private static IQueryable<LabTestInfo> ApplyYearFilter(
            IQueryable<LabTestInfo> query,
            Expression<Func<LabTestInfo, DateTimeOffset?>> dateProperty,
            object timeRange)
        {
            var timeRangeStr = timeRange?.ToString();
            if (string.IsNullOrEmpty(timeRangeStr))
                return query;

            if (timeRangeStr.StartsWith("[") && timeRangeStr.EndsWith("]"))
            {
                // 处理年份范围
                var years = timeRangeStr.Trim('[').Trim(']').Split(',')
                    .Select(y =>
                    {
                        if (DateTimeOffset.TryParse(y.Trim('"'), out var date))
                        {
                            // 转换为东八区时间获取年份
                            var beijingTime = date.ToOffset(TimeSpan.FromHours(8));
                            return beijingTime.Year;
                        }
                        return (int?)null;
                    })
                    .Where(y => y.HasValue)
                    .Select(y => y.Value)
                    .ToList();

                if (years.Count == 2)
                {
                    var startYear = years[0];
                    var endYear = years[1];

                    // 开始时间：开始年份的1月1日 00:00:00 东八区
                    DateTimeOffset start = new DateTimeOffset(startYear, 1, 1, 0, 0, 0, TimeSpan.FromHours(8));

                    // 结束时间：结束年份的下一年1月1日 00:00:00 东八区
                    DateTimeOffset end = new DateTimeOffset(endYear, 1, 1, 0, 0, 0, TimeSpan.FromHours(8))
                        .AddYears(1);

                    // 获取属性名称
                    var propertyName = ((MemberExpression)dateProperty.Body).Member.Name;

                    // 使用Dynamic LINQ构建查询
                    return query.Where($"{propertyName} >= @0 && {propertyName} < @1", start, end);
                }
            }
            else
            {
                // 处理单个年份
                if (DateTimeOffset.TryParse(timeRangeStr, out var date))
                {
                    // 转换为东八区时间获取年份
                    var beijingTime = date.ToOffset(TimeSpan.FromHours(8));
                    var year = beijingTime.Year;

                    // 年份的开始和结束时间（东八区）
                    DateTimeOffset start = new DateTimeOffset(year, 1, 1, 0, 0, 0, TimeSpan.FromHours(8));
                    DateTimeOffset end = start.AddYears(1); // 下一年1月1日 00:00:00

                    // 获取属性名称
                    var propertyName = ((MemberExpression)dateProperty.Body).Member.Name;

                    // 使用Dynamic LINQ构建查询
                    return query.Where($"{propertyName} >= @0 && {propertyName} < @1", start, end);
                }
            }

            return query;
        }
    }

}
