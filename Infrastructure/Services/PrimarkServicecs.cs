using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories;
using NX_lims_Softlines_Command_System.Infrastructure.Providers;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;

namespace NX_lims_Softlines_Command_System.Infrastructure.Services
{
    public class PrimarkService : IBuyerService
    {
        private readonly PrimarkRepository _repo;
        private readonly FiberContentHelper _helper;

        public PrimarkService(PrimarkRepository repo, FiberContentHelper helper)
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

            return groupedCheckLists;//去重后
        }

        public async Task<object?> ShowParameterAsync([FromBody] RequiredInfoDto infoDto)
        {
            var items = infoDto.items;
            PrimarkParameterProvider helper = new PrimarkParameterProvider(_helper);
            // 生成对应 DTO
            try
            {
                var dtos = new List<object>();
                foreach (var item in items!)
                {
                    var wetParams = await _repo.GetOrCreateWetParamsAsync<WetParameterIso>(
                        new ParamsInput
                        {
                            MenuName = infoDto.menuName!,
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
                            additionalRequire = infoDto.additionalRequire,
                            SampleDescription = infoDto.sampleDescription
                        }, item.itemName!);
                    string? param = await helper.CreateParameters(infoDto, item.itemName!)!;
                    dtos.Add(CreateResponse(item.itemName!, wetParams ?? new WetParameterIso { ContactItem = item.itemName! }, param!));
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
            "Colour Fastness to Washing" => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", p.Program, p.SteelBallNum, null,p.SpecialCareInstruction, null, null, null, null, null, Param),
            "Absorbency of Textiles" => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", p.Program, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, p.AfterWash, null),
            "Colour Fastness to Hot Pressing" => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", null, null, null, null, null, null, null, null, null, p.Iron),
            "Dimensional and Bra Wire Casing Stability" => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", p.Program, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, p.AfterWash, null),
            "Martindale Pilling" => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", p.Program, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, p.AfterWash, Param),
            "Print / Motif / Flock Durability" => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", null, null,null, null, p.DryProcedure,null, null, null, p.AfterWash, null),
            "Print Durability" => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", null, null, null, null, p.DryProcedure, null, null, null, p.AfterWash, null),
            "Shower Resistant Claims Spray Rating" => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", p.Program, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, p.AfterWash, null),
            "Spirality" => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", p.Program, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, p.AfterWash, null),
            "Stability to Washing" => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", p.Program, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, p.AfterWash, null),
            "Waterproof Claims Hydrostatic Head" => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", p.Program, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, p.AfterWash, null),
            "Dimensional Stability" => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", p.Program, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, p.AfterWash, null),
            "Stability to Dry Cleaning" => new(p.ContactItem!, p.ReportNumber,null, null, null, null, null, null, null, p.Sensitive, null, p.AfterWash, null),
            "Abrasion of Knitted Footwear Garments - Modified Martindale" => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, Param),
            "Accelerotor" => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, Param),
            "Bursting Strength" => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, Param),
            "Colour Fastness to Chlorinated Water" => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, Param),
            "Colour Fastness to Dry Cleaning" => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, Param),
            "Colour Fastness to Light" => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, Param),
            "Colour Fastness to Water" => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, Param),
            "Martindale Abrasion" => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, Param),
            "Nap Stability" => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, Param),
            "Residual Elogation" => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, Param),
            "Residual Elongation SHAPEWEAR" => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, Param),
            "Tear Strength" => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, Param),
            "Tensile Strength" => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, Param),
            "Unrecovered Elongation" => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, Param),
            "Elastic Extension and Modulus Test" => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, Param),
            "Vertical Wicking of Textiles" => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, Param),
            "Back Pocket Application Strength" => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, Param),
            "Belt Loop Application Strength" => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, Param),
            "Colour Fastness to Non Chlorine Bleach" => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, Param),
            "Colour Fastness to Chlorine Bleach" => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, Param),
            "Quick Dry" => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, Param),
            _ => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, null)
        };
    }
}
