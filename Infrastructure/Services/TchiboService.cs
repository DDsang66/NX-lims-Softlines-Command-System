using DocumentFormat.OpenXml.Drawing.Diagrams;
using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories.BuyerRepos;
using NX_lims_Softlines_Command_System.Infrastructure.Providers.ParamProvider;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;
using System.Drawing;

namespace NX_lims_Softlines_Command_System.Infrastructure.Services
{
    public class TchiboService : IBuyerService
    {
        private readonly TchiboRepository _repo;
        private readonly FiberContentHelper _helper;

        public TchiboService(TchiboRepository repo, FiberContentHelper helper)
        {
            _repo = repo;
            _helper = helper;
        }

        public async Task<object?> ShowItemAsync([FromBody] RequiredInfoDto infoDto)
        {
            string MenuName = infoDto.menuName!;
            var checkLists = await _repo.GetCheckListAsync(MenuName);//返回CheckListDto类型的对象
            if (checkLists == null) return null;

            var groupedCheckLists = checkLists
                .Select(cl => new
                {
                    ItemName = cl.ItemName,
                    Standards = cl.Standard != null ? new List<string> { cl.Standard } : new List<string>(),
                    Types = cl.Type != null ? new List<string> { cl.Type } : new List<string>(),
                    Parameters = cl.Parameter != null ? new List<string> { cl.Parameter } : new List<string> { "-" }
                })
                .ToList();

            return groupedCheckLists;//去重后，返回
        }

        public async Task<object?> ShowParameterAsync([FromBody] RequiredInfoDto infoDto)
        {
            var items = infoDto.items;
            TchiboParamProvider helper = new TchiboParamProvider(_helper);
            // 生成对应 DTO
            try
            {
                var dtos = new List<object>();
                foreach (var item in items!)
                {
                    var wetParams = await _repo.GetOrCreateWetParamsAsync<WetParameterIso>(
                        new ParamsInput
                        {
                            WashingProcedure = infoDto.washingProcedure,
                            DryProcedure = infoDto.dryProcedure,
                            Sci = infoDto.sci,
                            Iron = infoDto.ironProcedure,
                            IronMethod = infoDto.ironMethod,
                            Bleach = infoDto.bleachProcedure,
                            Detergent = infoDto.detergent,
                            FiberContent = infoDto.fiberComposition,
                            OrderNumber = infoDto.reportNumber,
                            DCProcedure = infoDto.dcProcedure,
                            AfterWash = infoDto.afterWash,
                            ItemName = item.itemName,
                            Standard = item.standards,
                            additionalRequire = infoDto.additionalRequire,
                            SampleDescription = infoDto.sampleDescription,
                        }, item.itemName!);
                    string? param = await helper.CreateParameters(infoDto, item.itemName!,item.standards!)!;
                    dtos.Add(CreateResponse(item.itemName!, item.standards!,wetParams ?? new WetParameterIso { ContactItem = item.itemName }, param!));
                }
                return dtos;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"{ex.Message}");
            }
            return null;
        }

        //返回前端需要的实体对象
        private static ParamDto CreateResponse(string itemName, string standard,WetParameterIso p, string Param) =>( itemName,standard) switch
        {
            ("CF to Washing",_) => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", p.Program, p.SteelBallNum, null, null, null, p.WashingProcedure, null, null, null, p.Iron),
            ("DS to Washing",_) => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", null, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, p.Program, p.AfterWash, p.Detergent),
            ("DS to Dry-clean",_) => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, p.Sensitive, null, null, null),
            ("Pilling Resistance", "DIN EN ISO 12945-2:2021") => new(itemName, null, null, null, null, null, null, null, null, null, null, null, "2000 revs"),
            ("Pilling Resistance", "DIN EN ISO 12945-1:2021") => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            ("Air Permeability", _) => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            ("Absorbency", _) => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", p.Program, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, p.AfterWash, null),
            ("Abrasion Resistance", _) => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            ("Snagging Resistance", _) => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            ("Water Resistance-Hydrostatic Pressure", _) => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            ("Extension and Recovery", _) => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            ("Seam Slippage", _) => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            ("CF to Sublimation in Storage", _) => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", null, null, null, null, null, null, null, null, null, "48h"),
            ("CF to Hot Pressing", _) => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", null, null, null, null, null, null, null, null, null,p.Iron),
            ("CF to Saliva", _) => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            ("CF to Sweat", _) => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            ("CF to Light",_) => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            ("CF to Water", _) => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            ("Appearance", _) => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            _ => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, null)
        };
    }
}
