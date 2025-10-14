using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Domain.Model.Interface;
using NX_lims_Softlines_Command_System.Infrastructure.Providers;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;

namespace NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories
{
    public class TchiboRepository : IRepository
    {
        private readonly LabDbContextSec _db;
        private readonly FiberContentHelper _helper;
        public TchiboRepository(LabDbContextSec db, FiberContentHelper helper)
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

        public async Task<T?> GetOrCreateWetParamsAsync<T>(ParamsInput input, string itemName) where T : IWetParam, new()
        {
            //return (T)(object)Param;//返回WetParameters类型的对象
            return default;
        }

        }
}
