using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using System.Linq.Expressions;

namespace NX_lims_Softlines_Command_System.Infrastructure.Providers
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
                            query = query.Where(o => o.ReportNumber.Contains(reportNumber));
                        }
                        break;

                    case "express":
                        string express = param.Value?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(express) && express != "All")
                        {
                            query = query.Where(o => o.Express.Contains(express));
                        }
                        break;

                    case "group":
                        string testGroup = param.Value?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(testGroup) && testGroup != "All")
                        {
                            query = query.Where(o => o.TestGroup.Contains(testGroup));
                        }
                        break;

                    case "orderEnrty":
                        string entryPerson = param.Value?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(entryPerson))
                        {
                            query = query.Where(o => o.OrderEntryPerson.Contains(entryPerson));
                        }
                        break;

                    case "status":
                        if (int.TryParse(param.Value?.ToString(), out int status))
                        {
                            query = query.Where(o => o.Status == status);
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
        public IQueryable<LabTestSchedule> QueryLabTestSchedule(
            Dictionary<string, object> queryParams, 
            LabDbContextSec _db)
        {
            var query = _db.LabTestSchedules.AsQueryable();
            // 获取时间相关参数
            if (queryParams == null || !queryParams.Any())
                return query;

            // 应用时间筛选
            //query = ApplyTimeFilter(query, queryParams);

            return query;
        }


        /// <summary>
        /// 合并两个表的结果
        /// </summary>
        /// <param name="infoQuery">LabTestInfo 查询结果</param>
        /// <param name="scheduleQuery">LabTestSchedule 查询结果</param>
        /// <returns>合并后的结果</returns>
        public IQueryable<object> MergeResults(
            IQueryable<LabTestInfo> infoQuery,
            IQueryable<LabTestSchedule> scheduleQuery)
        {
            var infoList = infoQuery.ToList();
            var scheduleList = scheduleQuery.ToList();

            var result = infoList.Select(info => new
            {
                Info = info,
                Schedule = scheduleList.FirstOrDefault(s => s.IdSchedule == info.Id)
            });

            return result.AsQueryable();
        }



    }
}
