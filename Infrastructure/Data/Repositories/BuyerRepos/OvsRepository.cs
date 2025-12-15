using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Domain.Model.Interface;
using NX_lims_Softlines_Command_System.Infrastructure.Providers.ParamProvider;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;
namespace NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories.BuyerRepos
{
        //与数据库交互
        public class OvsRepository : IRepository
        {
            private readonly LabDbContextSec _db;
            private readonly FiberContentHelper _helper;
            public OvsRepository(LabDbContextSec db, FiberContentHelper helper)
            {
                _db = db;
                _helper = helper;
            }

            public async Task<List<CheckListDto>?> GetCheckListAsync(dynamic input)
            {
                try
                {
                string menuName = input.Trim();

                // 先全表拉到内存
                var allMenus = await _db.OvsMenus.AsNoTracking().ToListAsync();

                // 精确匹配
                var hitMenus = allMenus
                    .Where(m => m.BuyerTable!
                        .Split(',')
                        .Select(s => s.Trim())
                        .Contains(menuName, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                if (!hitMenus.Any()) return null;

                var checkLists = hitMenus
                    .Where(m => m.StandardName != null)
                    .Select(m => new CheckListDto
                    {
                        MenuName = menuName,
                        ItemName = m.ItemName,
                        Standard = m.StandardName,
                        Type = m.Type,
                        Parameter = null
                    })
                    .ToList();
                checkLists = checkLists.OrderBy(cl => cl.ItemName).ToList();

                    return checkLists;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($" {ex.Message}");
                }
                return null;
            }

            public async Task<T?> GetOrCreateWetParamsAsync<T>(ParamsInput input, string itemName) where T : IWetParam, new()
            {
                // 只处理指定 item 类型
                if (!new[] { "CF to Washing", "DS to Washing", "DS to Dry-clean" }
                     .Contains(itemName))
                    return default;
                var Param = await _db.WetParameterIsos
                                  .FirstOrDefaultAsync(p => p.ContactItem == itemName && p.ReportNumber == input.OrderNumber);
                OvsParameterProvider wetParam = new OvsParameterProvider(_helper);
                if (Param != null)
                {
                    var updatedParam = wetParam.CreateWetParameters(input);
                    updatedParam.ParamId = Param.ParamId;
                    _db.Entry(Param).CurrentValues.SetValues(updatedParam);
                    await _db.SaveChangesAsync();
                    Param = updatedParam;
                }
                else
                {
                    var newParam = new WetParameterIso//没有找到对应的对象，随即构造一个
                    {
                        StandardType = "ISO",
                        Sensitive = "N",
                        ReportNumber = input.OrderNumber!,
                        ContactItem = itemName
                    };
                    Param = wetParam.CreateWetParameters(input);
                    foreach (var prop in typeof(WetParameterIso).GetProperties())
                    {
                        if (prop.CanWrite && prop.Name != "ParamId") // 跳过主键字段，因为它是自增的
                        {
                            var value = prop.GetValue(Param);
                            if (value != null)
                            {
                                prop.SetValue(newParam, value);
                            }
                        }
                    }

                    await _db.WetParameterIsos.AddAsync(newParam);
                    await _db.SaveChangesAsync();
                    Param = newParam;
                }
                return (T)(object)Param;//返回WetParameters类型的对象
            }
        }
    }

