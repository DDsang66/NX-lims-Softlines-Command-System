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
    public class PrimarkRepository : IRepository
    {
        private readonly LabDbContextSec _db;
        private readonly FiberContentHelper _helper;
        public PrimarkRepository(LabDbContextSec db, FiberContentHelper helper)
        {
            _db = db;
            _helper = helper;
        }

        public async Task<List<CheckListDto>?> GetCheckListAsync(dynamic input)
        {
            try
            {
                string menuName = input;
                var Menu = await _db.PrimarkMenus.Where(m => m.BuyerTable!.Contains(menuName)).ToListAsync();
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
            if (!new[] { "Colour Fastness to Washing", "Absorbency of Textiles", "Colour Fastness to Hot Pressing",
                "Dimensional and Bra Wire Casing Stability", "Martindale Pilling", "Print / Motif / Flock Durability",
                "Print Durability","Shower Resistant Claims Spray Rating","Spirality","Stability to Dry Cleaning",
                "Stability to Washing","Waterproof Claims Hydrostatic Head","Dimensional Stability","Security of Attachment(Wash)",
                "Easycare/Non-Iron","Appearance-Common"}
                 .Contains(itemName))
                return default;
            var Param = await _db.WetParameterIsos
                              .FirstOrDefaultAsync(p => p.ContactItem == itemName && p.ReportNumber == input.OrderNumber);
            PrimarkParameterProvider wetParam = new PrimarkParameterProvider(_helper);
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
            return (T)(object)Param;//返回WetParameters类型的对象
        }
    }
}
