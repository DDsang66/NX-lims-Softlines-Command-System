using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Domain.Model.Interface;
using NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories;
using NX_lims_Softlines_Command_System.Infrastructure.Providers;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;

namespace NX_lims_Softlines_Command_System.Infrastructure.Services
{
    public class JakoService : IBuyerService
    {
        private readonly JakoRepository _repo;
        private readonly FiberContentHelper _helper;
        public JakoService(JakoRepository repo, FiberContentHelper helper)
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

            return groupedCheckLists;
        }


        public async Task<object?> ShowParameterAsync([FromBody] RequiredInfoDto infoDto)
        {
            var itemNames = infoDto.itemName;
            JakoParameterProvider helper = new JakoParameterProvider(_helper);
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
                            FiberContent = infoDto.fiberComposition,
                            OrderNumber = infoDto.reportNumber,
                            DCProcedure = infoDto.dcProcedure,
                            ItemName = item,
                            additionalRequire = infoDto.additionalRequire,
                            sampleDescription = infoDto.sampleDescription
                        }, item);
                    string? param = await helper.CreateParameters(infoDto, item)!;
                    dtos.Add(CreateResponse(item, wetParams ?? new WetParameterIso { ContactItem = item,ReportNumber = infoDto.reportNumber! }, param!));
                }
                return dtos;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"{ex.Message}");
            }
            return null;
        }


        private static ParamDto CreateResponse(string itemName, WetParameterIso p, string Param) => itemName switch
        {
            "CF to Washing" => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", p.Program, p.SteelBallNum, null, null, null, null, null, null, null, null),
            "CF to Hot Pressing" => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", null, null, null, null, null, null, null, null, null, null),
            "Appearance" => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", p.Program, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, p.AfterWash, null),
            "DS to Dry-clean" => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, p.Sensitive, null, null, null),
            "Pilling Resistance" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Print Durability For JAKO" => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", p.Program, null, null, null, p.DryProcedure, null, null, null, null, null),
            "Heat Press Test For JAKO" => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", p.Program, null, null, null, null, null, null, null, null, null),
            "Snagging Resistance" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Abrasion Resistance" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "CF to Light" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Seam Slippage" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Bursting Strength" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Tensile Strength" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Extension and Recovery" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Air Permeability" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Water Repellency-Spray Test" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Spriality/Skewing" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "CF to Chlorinated Water" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            _ => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, null)
        };
    }
}
