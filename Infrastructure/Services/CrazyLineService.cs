using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories;
using NX_lims_Softlines_Command_System.Infrastructure.Providers;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;

namespace NX_lims_Softlines_Command_System.Infrastructure.Services
{
    public class CrazyLineService : IBuyerService
    {
        private readonly CrazyLineRepository _repo;
        private readonly FiberContentHelper _helper;

        public CrazyLineService(CrazyLineRepository repo, FiberContentHelper helper)
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
            CrazyLineParameterProvider helper = new CrazyLineParameterProvider(_helper);
            // 生成对应 DTO
            try
            {
                var dtos = new List<object>();
                foreach (var item in itemNames!)
                {
                    var wetParams = await _repo.GetOrCreateWetParamsAsync<WetParameterAatcc>(
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
                            sampleDescription = infoDto.sampleDescription,
                            ItemName = item
                        }, item);
                    string? param = await helper.CreateParameters(infoDto, item)!;
                    dtos.Add(CreateResponse(item, wetParams ?? new WetParameterAatcc { ContactItem = item }, param!));
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
        private static ParamDto CreateResponse(string itemName, WetParameterAatcc p, string Param) => itemName switch
        {
            "CF to Washing" => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°F", p.Program, p.SteelBallNum, null, null, null, p.WashingProcedure, null, null, null, null),
            "DS to Washing" => new(p.ContactItem!, p.ReportNumber, p.Temperature + "°F", null, null, null, p.SpecialCareInstruction, p.DryProcedure, null, null, p.Cycle, null, null),
            "DS to Dry-clean" => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, p.Sensitive, null, null, null),
            "Pilling Resistance" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Snagging Resistance" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "CF to Light" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            "Spriality/Skewing" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param),
            _ => new(p.ContactItem!, p.ReportNumber, null, null, null, null, null, null, null, null, null, null, null)
        };

    }
}
