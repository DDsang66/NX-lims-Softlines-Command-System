using NX_lims_Softlines_Command_System.Infrastructure.Providers;
using NX_lims_Softlines_Command_System.Models;
using NX_lims_Softlines_Command_System.Application.DTO;
using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;
using NX_lims_Softlines_Command_System.Domain;
using NX_lims_Softlines_Command_System.Domain.Model.Interface;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;

namespace NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories
{
    
    //与数据库交互
    public class CrazyLineRepository : IRepository
    {
        private readonly LabDbContext _db;
        private readonly FiberContentHelper _helper;
        public CrazyLineRepository(LabDbContext db,FiberContentHelper helper)
        {
            _db = db;
            _helper = helper;
        }

        public async Task<List<CheckListDto>?> GetCheckListAsync(dynamic input)
        {
            try
            {
                string menuName = input;
                var Menu = await _db.Menu.FirstOrDefaultAsync(m => m.MenuName == menuName);
                if (Menu == null) return null;

                var properties = typeof(Menus).GetProperties();
                var standards = properties
                    .Where(p => p.Name.StartsWith("Item"))
                    .Select(p => p.GetValue(Menu) as string)
                    .Where(v => !string.IsNullOrEmpty(v))
                    .ToList();

                var checkLists = new List<CheckListDto>();
                foreach (var standard in standards)
                {
                    try
                    {
                        int itemID =  _db.Standards.FirstOrDefault(s => s.Value_data == standard)!.ItemID;

                        var item = await _db.Item.FindAsync(itemID);
                        if (item != null)
                        {
                            checkLists.Add(new CheckListDto
                            {
                                MenuName = menuName,
                                ItemName = item.ItemName,
                                Standard = standard,
                                Type = item.Type,
                                Parameter = null
                            });

                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing standard {standard}: {ex.Message}");
                    }
                }


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
                return default(T);
            var Param = await _db.WetParameters
                              .FirstOrDefaultAsync(p => p.Item == itemName && p.OrderNumber == input.OrderNumber);
            CrazyLineParameterProvider wetParam = new CrazyLineParameterProvider(_helper);
            if (Param != null) {
                var updatedParam = wetParam.CreateWetParameters(input);
                updatedParam.ID = Param.ID;
                _db.Entry(Param).CurrentValues.SetValues(updatedParam);
                await _db.SaveChangesAsync();
                Param = updatedParam;
            }
            else
            {
                var newParam = new WetParameters//没有找到对应的对象，随即构造一个
                {
                    StandardType = "AATCC",
                    IsSensitive = "N",
                    OrderNumber = input.OrderNumber,
                    Item = itemName
                };
                Param = wetParam.CreateWetParameters(input);
                foreach (var prop in typeof(WetParameters).GetProperties())
                {
                    if (prop.CanWrite && prop.Name != "ID") // 跳过主键字段
                    {
                        var value = prop.GetValue(Param);
                        if (value != null)
                        {
                            prop.SetValue(newParam, value);
                        }
                    }
                }

                await _db.WetParameters.AddAsync(newParam);
                await _db.SaveChangesAsync();
                Param = newParam;
            }
            return (T)(object)Param;//返回WetParameters类型的对象
        }
    }
}
