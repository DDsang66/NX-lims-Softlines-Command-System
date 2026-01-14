using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories.BuyerRepos;
using NX_lims_Softlines_Command_System.Infrastructure.Providers.Mapper;
using NX_lims_Softlines_Command_System.Infrastructure.Providers.ParamProvider;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;

namespace NX_lims_Softlines_Command_System.Infrastructure.Services
{
    public class PrimarkService: IBuyerService
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

            SaveSampleInfo(infoDto.sampleDescripBoundSingle!, infoDto.reportNumber!, infoDto.buyer!);

            try
            {
                var dtos = new List<object>();
                foreach (var item in items!)
                {
                    var wetParams = await _repo.GetOrCreateWetParamsAsync<WetParameterIso>(new ParamsInput().CreateParamsInput(infoDto, item.itemName!.ToString(), item.standards!.ToString()), item.itemName!);

                    string? param = await helper.CreateParameters(infoDto, item.itemName!)!;

                    dtos.Add(PrimarkParameterMapper.Map(item.itemName!, wetParams ?? new WetParameterIso { ContactItem = item.itemName!, Standard = item.standards }, param!));
                }
                return dtos;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"{ex.Message}");
            }
            return null;
        }


        /// <summary>
        /// 保存SampleInfo服务
        /// </summary>
        /// <param name="sampleDescObjects"></param>
        /// <param name="reportNum"></param>
        /// <param name="buyer"></param>
        private void SaveSampleInfo (List<SampleDescObject> sampleDescObjects, string reportNum, string buyer)
        {
            // 检查输入参数是否为空
            if (sampleDescObjects == null || !sampleDescObjects.Any())
            {
                return; // 或者抛出新的 ArgumentNullException(nameof(sampleDescObjects));
            }
            foreach (var item in sampleDescObjects) 
            {
                var sampleObject = new SampleDescObject();
                sampleObject.sample = item.sample;
                sampleObject.description = item.description;
                _repo.SaveSampleInfoAsync(sampleObject, reportNum, buyer);
            }
        }
    }
}
