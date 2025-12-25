using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories.BuyerRepos;
using NX_lims_Softlines_Command_System.Infrastructure.Providers.Mapper;
using NX_lims_Softlines_Command_System.Infrastructure.Providers.ParamProvider;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;

namespace NX_lims_Softlines_Command_System.Infrastructure.Services
{
    public class LPPService : IBuyerService
    {
        private readonly LPPRepository _repo;
        private readonly FiberContentHelper _helper;

        public LPPService(LPPRepository repo, FiberContentHelper helper)
        {
            _repo = repo;
            _helper = helper;
        }
        /// <summary>
        /// 根据传入的参数，返回CheckListDto类型的对象
        /// </summary>
        /// <param name="infoDto"></param>
        /// <returns></returns>
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


        /// <summary>
        /// 根据传入的参数，返回WetParameterDto类型的对象
        /// </summary>
        /// <param name="infoDto"></param>
        /// <returns></returns>
        public async Task<object?> ShowParameterAsync([FromBody] RequiredInfoDto infoDto)
        {
            var items = infoDto.items;
            LPPParameterProvider helper = new LPPParameterProvider(_helper);
            // 生成对应 DTO
            try
            {
                var dtos = new List<object>();
                foreach (var item in items!)
                {
                    var wetParams = await _repo.GetOrCreateWetParamsAsync<WetParameterIso>(
                          new ParamsInput().CreateParamsInput(infoDto, item.itemName!.ToString(), item.standards!.ToString()), item.itemName!);

                    string? param = await helper.CreateParameters(infoDto, item.itemName!)!;

                    dtos.Add(LPPParameterMapper.Map(item.itemName!,wetParams ?? new WetParameterIso { ContactItem = item.itemName!, Standard = item.standards }, param!));
                }
                return dtos;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"{ex.Message}");
            }
            return null;
        }

    }
}
