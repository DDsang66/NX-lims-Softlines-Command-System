using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories.BuyerRepos;
using NX_lims_Softlines_Command_System.Infrastructure.Providers.Mapper;
using NX_lims_Softlines_Command_System.Infrastructure.Providers.ParamProvider;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;

namespace NX_lims_Softlines_Command_System.Infrastructure.Services
{
    public class OldOvsService
    {
        private readonly OvsRepository _repo;
        private readonly FiberContentHelper _helper;

        public OldOvsService(OvsRepository repo, FiberContentHelper helper)
        {
            _repo = repo;
            _helper = helper;
        }


        /// <summary>
        /// 根据传入的参数，生成对应的参数
        /// </summary>
        /// <param name="infoDto"></param>
        /// <returns></returns>
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
                    Parameters = cl.Parameter != null ? new List<string> { cl.Parameter } : null
                })
                .ToList();

            return groupedCheckLists;//去重后，返回给Ovs类
        }


        /// <summary>
        /// 根据传入的参数，生成对应的参数
        /// </summary>
        /// <param name="infoDto"></param>
        /// <returns></returns>
        public async Task<object?> ShowParameterAsync([FromBody] RequiredInfoDto infoDto)
        {
            //var items = infoDto.items;

            //OldOvsParameterProvider helper = new OldOvsParameterProvider(_helper);

            //try
            //{
            //    var dtos = new List<object>();
            //    foreach (var item in items!)
            //    {
            //        var wetParams = await _repo.GetOrCreateWetParamsAsync<WetParameterIso>(
            //            new ParamsInput().CreateParamsInput(infoDto, item.itemName!.ToString(), item.standards!.ToString()), item.itemName!);
                    
            //        string? param = await helper.CreateParameters(infoDto, item.itemName!, item.standards!)!;
                    
            //        dtos.Add(OldOvsParameterMapper.Map(item.itemName!, wetParams ?? new WetParameterIso { ContactItem = item.itemName!, Standard = item.standards }, param!));
            //    }
            //    return dtos;
            //}
            //catch (Exception ex)
            //{
            //    System.Diagnostics.Debug.WriteLine($"{ex.Message}");
            //}
            return null;
        }
    }
}
