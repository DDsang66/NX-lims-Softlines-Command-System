using NX_lims_Softlines_Command_System.Infrastructure.Providers;
using NX_lims_Softlines_Command_System.Application.DTO;
using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;
using NX_lims_Softlines_Command_System.Domain;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Domain.Model.Interface;
namespace NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories
{


    public class JakoRepository : IRepository
    {
        private readonly LabDbContextSec _db;
        private readonly FiberContentHelper _helper;
        public JakoRepository(LabDbContextSec db, FiberContentHelper helper)
        {
            _db = db;
            _helper = helper;
        }

        public async Task<List<CheckListDto>?> GetCheckListAsync(dynamic input) 
        {
            try
            {
                string menuName = input;
                var Menu = await _db.Menus.FirstOrDefaultAsync(m => m.MenuName == menuName);
                if (Menu == null) return null;

                var properties = typeof(Menu).GetProperties();
                var standards = properties
                    .Where(p => p.Name.StartsWith("StandardIndex"))
                    .Select(p => p.GetValue(Menu))
                    .OfType<int?>()
                    .Where(v => v.HasValue)
                    .ToList();

                var checkLists = new List<CheckListDto>();
                foreach (var standard in standards)
                {
                    try
                    {
                        int? itemID = _db.Standards.FirstOrDefault(s => s.StandardId == standard)!.ItemIndex;
                        string? standardCore = _db.Standards.FirstOrDefault(s => s.StandardId == standard)!.StandardCode;
                        var item = await _db.Items.FindAsync(itemID);
                        if (item != null)
                        {
                            checkLists.Add(new CheckListDto
                            {
                                MenuName = menuName,
                                ItemName = item.ItemName,
                                Standard = standardCore,
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

        public async Task<T?> GetOrCreateWetParamsAsync<T>(ParamsInput input, string itemName) where T :IWetParam, new()
        { 
            // 只处理指定 item 类型
            if (!new[] { "CF to Washing", "Appearance", "DS to Dry-clean", "Print Durability For JAKO","Heat Press Test For JAKO","CF to Hot Pressing" }
                 .Contains(itemName))
                return default(T);
            var Param = await _db.WetParameterIsos
                              .FirstOrDefaultAsync(p => p.ContactItem == itemName && p.ReportNumber == input.OrderNumber);
            JakoParameterProvider wetParam = new JakoParameterProvider(_helper);
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
                    if (prop.CanWrite && prop.Name != "ParamId") // 跳过主键字段
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
            return (T)(object)Param;
        }

    }
}
