using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.Application.Services.AuthenticationService;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Infrastructure.Providers.Order;
using System.Collections.Concurrent;
using System.Linq.Dynamic.Core;

namespace NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories.RenderRepos
{
    public class RenderRepos
    {
        private readonly LabDbContextSec _db;
        public RenderRepos(
            LabDbContextSec db)
        {
            _db = db;
        }

        /// <summary>
        /// 表单数据获取
        /// </summary>
        public async Task<object> RenderAsync(string buyername)
        {
            var sampleDescList = await _db.SampleDescriptions.Where(x => x.BuyerName == buyername).ToArrayAsync();
            var groupedsampleDescList = sampleDescList
                .GroupBy(cl => cl.PropertyName)
                .Select(group => new
                {
                    PropertyName = group.Key,
                    PropertyValue = group.Select(cl => cl.PropertyValue).Distinct().ToList(),
                    type = group.Select(cl => cl.Type).Distinct().FirstOrDefault(),
                    defaultValue = group.Select(cl => cl.DefaultValue).Distinct().FirstOrDefault(),
                    isNecessary = group.Select(cl => cl.IsNecessary).Distinct().FirstOrDefault()
                })
                .ToList();
            return groupedsampleDescList;
        }

        ///// <summary>
        ///// 获取纤维成分列表（从 fiber_database 表）
        ///// </summary>
        //public async Task<object> CompostionSearchAsync()
        //{
        //    var list = await _db.FiberDatabases
        //        .Where(x => x.IsActive)
        //        .OrderBy(x => x.FiberNameEn)
        //        .Select(x => x.FiberNameEn)
        //        .ToListAsync();
        //    return list;
        //}

        /// <summary>
        /// 获取纤维成分列表（从 fiber_database 表）
        /// </summary>
        public async Task<object> CompostionSearchAsync()
        {
            var list = await _db.FiberDatabases
                .Where(x => x.IsActive == null || x.IsActive == true)
                .Select(x => x.FiberNameEn)
                .ToListAsync();
            return list;
        }
    }
}
