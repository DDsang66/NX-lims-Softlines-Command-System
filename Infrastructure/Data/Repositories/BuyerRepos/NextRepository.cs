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
    public class NextRepository : IRepository
    {
        private readonly LabDbContextSec _db;
        private readonly FiberContentHelper _helper;
        public NextRepository(LabDbContextSec db, FiberContentHelper helper)
        {
            _db = db;
            _helper = helper;
        }

        public async Task<List<CheckListDto>?> GetCheckListAsync(dynamic input)
        {
            try
            {
                string menuName = input;
                var Menu = await _db.NextMenus.Where(m => m.BuyerTable!.Contains(menuName)).ToListAsync();
                if (Menu == null) return null;

                var checkLists = new List<CheckListDto>();
                foreach (var m in Menu)
                {
                    try
                    {
                        if (m.StandardName != null)
                        {
                            checkLists.Add(new CheckListDto
                            {
                                MenuName = menuName,
                                ItemName = m.ItemName,
                                Standard = m.StandardName,
                                Type = m.Type,
                                Parameter = null
                            });

                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing standard {m.StandardName}: {ex.Message}");
                    }
                }
                checkLists = checkLists.OrderBy(cl => cl.Standard).ToList();

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
            if (!new[] { "Fastness to Washing", "Cross Staining to Washing", "Print Durability" ,
                "Embellishment Durability (Childrenswear)","Embellishment Durability (General)","Foil Durability","Appearance Assessment after Wash","Appearance Assessment after Dry Clean",
                "Polar Fleece Assessment","Stability to Washing","Spirality","Spray Rating","Stability to Dry Cleaning","Assessment of Easy to Iron Fabrics"
            }
                 .Contains(itemName))
                return default;
            var Param = await _db.WetParameterIsos
                              .FirstOrDefaultAsync(p => p.ContactItem == itemName && p.ReportNumber == input.OrderNumber);
            NextParameterProvider wetParam = new NextParameterProvider(_helper);
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
                    Standard = input.Standard,
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
            return (T)(object)Param;//返回WetParameters类型的对象
        }
    }
}

