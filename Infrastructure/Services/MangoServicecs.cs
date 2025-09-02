using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories;
using NX_lims_Softlines_Command_System.Infrastructure.Providers;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;
using NX_lims_Softlines_Command_System.Models;

namespace NX_lims_Softlines_Command_System.Infrastructure.Services
{
    public class MangoService : IBuyerService
    {
        private readonly MangoRepository _repo;
        private readonly FiberContentHelper _helper;

        public MangoService(MangoRepository repo, FiberContentHelper helper)
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

            return groupedCheckLists;//去重后，返回给Mango类
        }

        public async Task<object?> ShowParameterAsync([FromBody] RequiredInfoDto infoDto)
        {
            var itemNames = infoDto.itemName;
            MangoParameterProvider helper = new MangoParameterProvider(_helper);
            // 生成对应 DTO
            try
            {
                var dtos = new List<object>();
                foreach (var item in itemNames!)
                {
                    var wetParams = await _repo.GetOrCreateWetParamsAsync<WetParameters>(
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
                            ItemName = item
                        }, item);
                    string? param = await helper.CreateParameters(infoDto, item)!;
                    dtos.Add(CreateResponse(item, wetParams ?? new WetParameters { Item = item }, param!));
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
        private static ParamDto CreateResponse(string itemName, WetParameters p, string Param) => itemName switch
        {
            "CF to Washing" => new(p.Item, p.OrderNumber, p.Temperature + "°C", p.Program, p.SteelBall, null, null, null, p.WashingProcedure, null, null, null, null, null),
            "DS to Washing" => new(p.Item, p.OrderNumber, p.Temperature + "°C", null, null, p.Ballast, p.SCI, p.DryProcedure, p.WashingProcedure, null, null, null, null, null),
            "DS to Dry-clean" => new(p.Item, p.OrderNumber, null, null, null, null, null, null, null, p.IsSensitive, null, null, null, null),
            "Pilling Resistance" => new(itemName, null, null, null, null, null, null, null, null, null, Param, null, null, null),
            "Abrasion Resistance" => new(itemName, null, null, null, null, null, null, null, null, null, Param, null, null, null),
            "Snagging Resistance" => new(itemName, null, null, null, null, null, null, null, null, null, Param, null, null, null),
            "Water Resistance-Hydrostatic Pressure" => new(itemName, null, null, null, null, null, null, null, null, null, null, null, Param, null),
            "CF to Light" => new(itemName, null, null, null, null, null, null, null, null, null, null, Param, null, null),
            _ => new(p.Item, p.OrderNumber, null, null, null, null, null, null, null, null, null, null, null, null)
        };
    }
}
