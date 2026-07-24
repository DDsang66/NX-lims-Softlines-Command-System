using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.OrderContext;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;
using System.Linq.Expressions;

namespace NX_lims_Softlines_Command_System.Infrastructure.Providers.Order
{
    public class OrderQueryProvider
    {
        /// <summary>
        /// 获取查询条件字典中的键值对
        /// </summary>
        /// <param name="dto">查询参数对象</param>
        /// <returns>查询条件字典</returns>
        public Dictionary<string, object> GetQueryParams(OrderQueryParams dto)
        {
            if (dto?.QueryParam == null)
                return new Dictionary<string, object>();

            return dto.QueryParam;
        }



        /// <summary>
        /// 查询 LabTestInfo 表
        /// </summary>
        /// <param name="queryParams">查询条件字典</param>
        /// <returns>符合条件的 LabTestInfo 列表</returns>
        public IQueryable<LabTestInfo> QueryLabTestInfo(
            Dictionary<string, object> queryParams,
            LabDbContextSec _db)
        {
            var query = _db.LabTestInfos.AsQueryable();

            if (queryParams == null || !queryParams.Any())
                return query;

            foreach (var param in queryParams)
            {
                switch (param.Key.ToLower())
                {
                    case "reportnum":
                        string reportNumber = param.Value?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(reportNumber))
                        {
                            query = query.Where(o => o.ReportNumber!.Contains(reportNumber));
                        }
                        break;

                    case "express":
                        string express = param.Value?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(express) && express != "All")
                        {
                            query = query.Where(o => o.Express!.Contains(express));
                        }
                        break;

                    case "group":
                        string testGroup = param.Value?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(testGroup) && testGroup != "All")
                        {
                            query = query.Where(o => o.TestGroup!.Contains(testGroup));
                        }
                        break;

                    case "orderEnrty":
                        string entryPerson = param.Value?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(entryPerson))
                        {
                            query = query.Where(o => o.OrderEntryPerson!.Contains(entryPerson));
                        }
                        break;

                    case "status":
                        var statusMap = new Dictionary<string, int>
                        {
                            { "In Lab", 1 },
                            { "Review Finished", 2 },
                            { "Test Done", 3 }
                            };

                        if (param.Value != null)
                        {
                            var statusStr = param.Value.ToString();

                            // 尝试直接解析为数字
                            if (int.TryParse(statusStr, out int status))
                            {
                                query = query.Where(o => o.Status == status);
                            }
                            // 尝试解析为字符串状态
                            else if (statusMap.TryGetValue(statusStr!, out int mappedStatus))
                            {
                                query = query.Where(o => o.Status == mappedStatus);
                            }
                        }
                        break;
                }
            }

            return query;
        }


        /// <summary>
        /// 查询 LabTestSchedule 表
        /// </summary>
        /// <param name="queryParams">查询条件字典</param>
        /// <returns>符合条件的 LabTestSchedule 列表</returns>
        public IQueryable<LabTestInfo> QueryLabTestSchedule(
            Dictionary<string, object> queryParams,
            LabDbContextSec _db)
        {

            //******************** 原始查询  ********************//
            var query = _db.LabTestInfos.AsQueryable();
            // 获取时间相关参数
            if (queryParams == null || !queryParams.Any())
                return query;

            // 取出时间相关参数
            var timeOpt = queryParams.ContainsKey("timeOpt") ? queryParams["timeOpt"]?.ToString() : null;
            var timeType = queryParams.ContainsKey("timeType") ? queryParams["timeType"]?.ToString() : null;
            var timeRange = queryParams.ContainsKey("timeRange") ? queryParams["timeRange"] : null;

            // 如果任一参数为null，直接返回原始查询
            if (string.IsNullOrEmpty(timeOpt) || string.IsNullOrEmpty(timeType) || timeRange == null)
            {
                return query;
            }
            //******************** 原始查询  ********************//

            if (timeType.ToLower().Contains("range"))
            {
                //处理两个时间的区间
                return query = TimeRangeQueryHelper.ApplyTimeRangeFilter(query, timeRange, timeOpt, timeType);
            }
            else
            {
                //处理单个时间节点或多个时间节点的情况
                return query = TimeNodeQueryHelper.ApplyTimeNodeFilter(query, timeRange, timeOpt, timeType);
            }
        }


        /// <summary>
        /// 合并两个表的结果
        /// </summary>
        /// <param name="infoQuery">LabTestInfo 查询结果</param>
        /// <param name="scheduleQuery">LabTestSchedule 查询结果</param>
        /// <returns>对两表查询结果取交集</returns>
        public IQueryable<LabTestJoinDto> MergeResults(
                IQueryable<LabTestInfo> infoQuery,
                IQueryable<LabTestInfo> scheduleQuery)
        {
            // 直接用 IQueryable 让 EF 翻译 SQL，不再 ToList()
            return from info in infoQuery
                   join schedule in scheduleQuery on info.Id equals schedule.Id
                   select new LabTestJoinDto   // 强类型匿名对象 → DTO
                   {
                       Info = info,
                       Schedule = schedule
                   };
        }
    }
}
