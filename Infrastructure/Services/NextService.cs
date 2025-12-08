using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories.BuyerRepos;
using NX_lims_Softlines_Command_System.Infrastructure.Providers.ParamProvider;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;

namespace NX_lims_Softlines_Command_System.Infrastructure.Services
{
    public class NextService : IBuyerService
    {
        private readonly NextRepository _repo;
        private readonly FiberContentHelper _helper;

        public NextService(NextRepository repo, FiberContentHelper helper)
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

            return groupedCheckLists;//去重后，返回给Mango类
        }

        public async Task<object?> ShowParameterAsync([FromBody] RequiredInfoDto infoDto)
        {
            var items = infoDto.items;
            NextParameterProvider helper = new NextParameterProvider(_helper);
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
                            FiberContent = infoDto.fiberComposition,
                            OrderNumber = infoDto.reportNumber,
                            DCProcedure = infoDto.dcProcedure,
                            AfterWash = infoDto.afterWash,
                            ItemName = item.itemName,
                            SampleDescription = infoDto.sampleDescription,
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
            "Fastness to Washing" => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", p.Program, p.SteelBallNum, null, null, null, p.WashingProcedure, null, null, null, Param),
            "Cross Staining to Washing" => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", p.Program, p.SteelBallNum, null, null, null, p.WashingProcedure, null, null, null, Param),
            "Stability to Washing" => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", null, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, null, null),
            "Print Durability" => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", null, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, null, null),
            "Embellishment Durability (Childrenswear)" => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", null, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, null, null),
            "Embellishment Durability (General)" => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", null, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, null, null),
            "Foil Durability" => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", null, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, null, null),
            "Appearance Assessment after Washing" => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", null, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, null, null),
            "Spray Rating" => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", null, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, null, null, null, null),
            "Assessment of Easy to Iron Fabrics" => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°C", null, null, p.Ballast, p.SpecialCareInstruction, p.DryProcedure, p.WashingProcedure, p.Sensitive, null, null, null),
            "Appearance Assessment after Dry Clean" => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, p.Sensitive, null, null, Param),
            "Stability to Dry Cleaning" => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, p.Sensitive, null, null, Param),
            "Pilling Resistance" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Swiss Pilling" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Fastness to Light" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Fastness to Dry Cleaning"=> new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Cross Staining to Dry Cleaning"=> new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Fastness to Water"=> new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Cross Staining to Water"=> new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Fastness to Chlorinated Water" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Grab Strength & Seam Slippage"=> new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Seam Slippage of Garment Seams"=> new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Tear Strength"=> new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Martindale Abrasion"=> new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Abrasion Home"=> new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Bursting Strength" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),      
            "Extension and Recovery"=> new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Extension and Modulus"=> new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Fastness to Saliva"=> new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Fastness to Sea Water"=> new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Fastness to Perspiration"=> new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Air Permeability of Textile Fabrics" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Snagging Resistance" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Hydrostatic Head Test" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Moisture Management" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Accelerotor Pile Loss" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            _ => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, null)
        };
    }
}
