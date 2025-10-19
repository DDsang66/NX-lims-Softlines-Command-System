using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.Office2010.CustomUI;
using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;
using System.Linq.Expressions;

namespace NX_lims_Softlines_Command_System.Infrastructure.Providers
{
    public class OrderReportingQueryProvider
    {
        /// <summary>
        /// 查询 某个小组或单号的LabTestInfo 语句
        /// </summary>
        /// <param name="queryParams">查询条件</param>
        /// <returns>符合条件的查询语句</returns>
        public IQueryable<LabTestInfo> QueryGroupInfo(
            string queryParams,
            LabDbContextSec _db)
        {
            var query = _db.LabTestInfos.AsQueryable();
            if (queryParams == null || !queryParams.Any())
                return query;
            if (!string.IsNullOrEmpty(queryParams) && queryParams.ToLower() == "all")
            {
                query = query.Where(o => o.IsDelete == "N");
            }
            else if (!string.IsNullOrEmpty(queryParams) && queryParams.ToLower() != "all")
            {
                query = query.Where(o => o.TestGroup!.Contains(queryParams) && o.IsDelete == "N");
            }
            return query;
        }


        /// <summary>
        /// 查询时间
        /// </summary>
        /// <param name="queryParams">查询条件</param>
        /// <returns>符合条件的查询语句</returns>
        public IQueryable<LabTestInfo> QueryTimeInfo(
            DateTimeOffset timeOffset, string timeType,string Property,
            LabDbContextSec _db)
        {
            var query = _db.LabTestInfos.AsQueryable();
            if (timeType.ToLower() == "date")
            {
                switch (Property!.ToLower())
                {
                    case "reportduedate":
                        query = query.Where(o => o.ReportDueDate.HasValue &&
                                                o.ReportDueDate.Value.Date == timeOffset.Date);
                        break;
                    case "labOuttime":
                        query = query.Where(o => o.LabOutTime.HasValue &&
                                                o.LabOutTime.Value.Date == timeOffset.Date);
                        break;
                }
            }
            else if (timeType.ToLower() == "month") 
            {
                switch (Property!.ToLower())
                {
                    case "reportduedate":
                        query = query.Where(o => o.ReportDueDate.HasValue &&
                                                o.ReportDueDate.Value.Month == timeOffset.Month);
                        break;
                    case "labOuttime":
                        query = query.Where(o => o.LabOutTime.HasValue &&
                                                o.LabOutTime.Value.Month == timeOffset.Month);
                        break;
                }
            }
            else if (timeType.ToLower() == "yaer")
            {
                switch (Property!.ToLower())
                {
                    case "reportduedate":
                        query = query.Where(o => o.ReportDueDate.HasValue &&
                                                o.ReportDueDate.Value.Year == timeOffset.Year);
                        break;
                    case "labOuttime":
                        query = query.Where(o => o.LabOutTime.HasValue &&
                                                o.LabOutTime.Value.Year == timeOffset.Year);
                        break;
                }
            }
            return query;
        }


        /// <summary>
        /// 当日当月当年需要出单量
        /// </summary>
        /// <param name="queryParams">查询条件</param>
        /// <returns>符合条件的查询语句</returns>
        public IQueryable<LabTestInfo> QuerySelect(
          DateTimeOffset timeOffset, string timeType, string DataType,
          LabDbContextSec _db)
        {
            var query = _db.LabTestInfos.AsQueryable();
            switch (DataType.ToLower()) 
            {
                case "needlabout":
                    var baseQuery = QueryTimeInfo(timeOffset, timeType, "ReportDueDate", _db);
                    query = QueryTimeInfo(timeOffset, timeType, "ReportDueDate", _db);
                    break;
                case "actuallylabout":
                    baseQuery = QueryTimeInfo(timeOffset, timeType, "ReportDueDate", _db);
                    query = baseQuery.Where(o => o.LabOutTime.HasValue &&
                                                           o.ReportDueDate.HasValue &&
                                                           o.LabOutTime.Value.Date < o.ReportDueDate.Value.Date);
                    break;
                case "delaylabout":
                    baseQuery = QueryTimeInfo(timeOffset, timeType, "ReportDueDate", _db);
                    // 如果 LabOutTime 无值
                    // 比较与当前时间
                    // 否则比较与 ReportDueDate
                    query = baseQuery.Where(o =>
                        !o.LabOutTime.HasValue ?
                            o.ReportDueDate!.Value < DateTime.Now :
                            o.LabOutTime.Value.Date > o.ReportDueDate!.Value.Date
                    );
                    break;
                case "inadvancelabout":
                    baseQuery = QueryTimeInfo(timeOffset, timeType, "ReportDueDate", _db);
                    query = baseQuery.Where(o => o.LabOutTime.HasValue &&
                                                           o.ReportDueDate.HasValue &&
                                                           o.LabOutTime.Value.Date.AddDays(1) == o.ReportDueDate.Value.Date);
                    break;
            }
            return query;
        }


    }
}
