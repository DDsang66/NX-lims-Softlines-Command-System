using NX_lims_Softlines_Command_System.Domain.Model.Entities;
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
        public static IQueryable<LabTestSchedule> ApplyTimeRangeFilter(
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

            // 根据timeType处理不同的时间范围
            if (timeType.ToLower().Contains("date"))
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
        /// 处理日范围筛选
        /// </summary>
        private static IQueryable<LabTestSchedule> ApplyDayFilter(
            IQueryable<LabTestSchedule> query,
            Expression<Func<LabTestSchedule, DateTimeOffset?>> dateProperty,
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
                    DateTimeOffset? start = new DateTimeOffset(dates[0].DateTime.Date, TimeSpan.FromHours(8)); // 使用+8时区
                    DateTimeOffset? end = new DateTimeOffset(dates[1].DateTime.Date.AddDays(1).AddSeconds(-1), TimeSpan.FromHours(8)); // 使用+8时区


                    // 使用编译后的表达式获取属性
                    var parameter = Expression.Parameter(typeof(LabTestSchedule),"x");
                    var property = (MemberExpression)dateProperty.Body;

                    // 创建比较表达式
                    var startComparison = Expression.GreaterThanOrEqual(
                                 property,
                                 Expression.Constant(start, typeof(DateTimeOffset?)));

                    var endComparison = Expression.LessThanOrEqual(
                        property,
                        Expression.Constant(end, typeof(DateTimeOffset?)));

                    var combined = Expression.AndAlso(startComparison, endComparison);

                    var lambda = Expression.Lambda<Func<LabTestSchedule, bool>>(combined, parameter);//未解决类型问题
                    return query.Where(lambda);
                }
            }
            else
            {
                // 处理单个日期
                if (DateTime.TryParse(timeRangeStr, out var date))
                {
                    var start = date.Date;
                    var end = date.Date.AddDays(1).AddTicks(-1); // 包含当天的最后一刻

                    return query.Where(x => x.OrderInTime >= start && x.OrderInTime <= end);
                }
            }

            return query;
        }

        /// <summary>
        /// 处理月范围筛选
        /// </summary>
        private static IQueryable<LabTestSchedule> ApplyMonthFilter(
            IQueryable<LabTestSchedule> query,
            Expression<Func<LabTestSchedule, DateTimeOffset?>> dateProperty,
            object timeRange)
        {
            var timeRangeStr = timeRange.ToString();
            if (timeRangeStr.StartsWith("[") && timeRangeStr.EndsWith("]"))
            {
                // 处理月份范围
                var months = timeRangeStr.Trim('[').Trim(']').Split(',')
                    .Select(m =>
                    {
                        if (DateTimeOffset.TryParse(m.Trim('"'), out var date))
                        {
                            return new { Year = date.Year, Month = date.Month };
                        }
                        return null;
                    })
                    .Where(m => m != null)
                    .ToList();


            }
            else
            {
                // 处理单个月份

            }

            return query;
        }

        /// <summary>
        /// 处理年范围筛选
        /// </summary>
        private static IQueryable<LabTestSchedule> ApplyYearFilter(
            IQueryable<LabTestSchedule> query,
            Expression<Func<LabTestSchedule, DateTimeOffset?>> dateProperty,
            object timeRange)
        {
            var timeRangeStr = timeRange.ToString();
            if (timeRangeStr.StartsWith("[") && timeRangeStr.EndsWith("]"))
            {
                // 处理年份范围
                var years = timeRangeStr.Trim('[').Trim(']').Split(',')
                    .Select(y =>
                    {
                        if (DateTimeOffset.TryParse(y.Trim('"'), out var date))
                        {
                            return date.Year;
                        }
                        return (int?)null;
                    })
                    .Where(y => y.HasValue)
                    .Select(y => y.Value)
                    .ToList();

            }
            else
            {

            }

            return query;
        }
    }

}
