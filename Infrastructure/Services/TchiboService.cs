using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories;
using NX_lims_Softlines_Command_System.Infrastructure.Providers;
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
                .GroupBy(cl => cl.ItemName)
                .Select(group => new
                {
                    ItemName = group.Key,
                    Standards = group.Select(cl => cl.Standard).Distinct().ToList(),
                    Types = group.Select(cl => cl.Type).Distinct().ToList(),
                    Parameters = group.Select(cl => cl.Parameter).Distinct().ToList()
                })
                .ToList();

            return groupedCheckLists;//去重后，返回
        }

        public async Task<object?> ShowParameterAsync([FromBody] RequiredInfoDto infoDto)
        {
            var itemNames = infoDto.itemName;
            TchiboParamProvider helper = new TchiboParamProvider(_helper);
            // 生成对应 DTO
            try
            {
                var dtos = new List<object>();
                foreach (var item in itemNames!)
                {
                    var wetParams = await _repo.GetOrCreateWetParamsAsync<WetParameterIso>(
                        new ParamsInput
                        {
                            WashingProcedure = infoDto.washingProcedure,
                            DryProcedure = infoDto.dryProcedure,
                            Sci = infoDto.sci,
                            Iron = infoDto.ironProcedure,
                            Bleach = infoDto.bleachProcedure,
                            Detergent = infoDto.detergent,
                            FiberContent = infoDto.fiberComposition,
                            OrderNumber = infoDto.reportNumber,
                            DCProcedure = infoDto.dcProcedure,
                            ItemName = item,
                            additionalRequire = infoDto.additionalRequire,
                            SampleDescription = infoDto.sampleDescription
                        }, item);
                    string? param = await helper.CreateParameters(infoDto, item)!;
                    dtos.Add(CreateResponse(item, wetParams ?? new WetParameterIso { ContactItem = item }, param!));
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
        private static ParamDto CreateResponse(string itemName, WetParameterIso p, string Param) => itemName switch
        {
            "CF to Washing" => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", p.Program, p.SteelBallNum, null, null, null, p.WashingProcedure, null, null, null, null),
            "DS to Washing" => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", null, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, p.Program, p.AfterWash, p.Bleach),
            "DS to Dry-clean" => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, p.Sensitive, null, null, null),
            "Pilling Resistance" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Air Permeability" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Absorbency" => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", p.Program, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, p.AfterWash, null),
            "Abrasion Resistance" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Snagging Resistance" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Water Resistance-Hydrostatic Pressure" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Extension and Recovery" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Seam Slippage" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "CF to Sublimation in Storage" => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", null, null, null, null, null, null, null, null, null, "48h"),
            "CF to Hot Pressing" => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", null, null, null, null, null, null, null, null, null,p.Iron),
            "CF to Saliva" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "CF to Sweat" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "CF to Light" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "CF to Water" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Appearance" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            _ => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, null)
        };
    }
}
