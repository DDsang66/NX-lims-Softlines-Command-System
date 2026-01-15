using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Domain.Model.Interface;
using NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories.BuyerRepos;
using NX_lims_Softlines_Command_System.Infrastructure.Providers.Mapper;
using NX_lims_Softlines_Command_System.Infrastructure.Providers.ParamProvider;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;

namespace NX_lims_Softlines_Command_System.Infrastructure.Services
{
    public class PrimarkService
    {
        private readonly PrimarkRepository _repo;
        private readonly FiberContentHelper _helper;

        public PrimarkService(PrimarkRepository repo, FiberContentHelper helper)
        {
            _repo = repo;
            _helper = helper;
        }
        /// <summary>
        /// 生成CheckList服务
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
                    Parameters = cl.Parameter != null ? new List<string> { cl.Parameter } : new List<string> { "-" }
                })
                .ToList();

            return groupedCheckLists;//去重后
        }

        /// <summary>
        /// 生成参数服务
        /// </summary>
        /// <param name="infoDto"></param>
        /// <returns></returns>
        public async Task<object?>ParameterAsync([FromBody] RequiredInfoDto infoDto)
        {
            var items = infoDto.items;

            PrimarkParameterProvider paramHelper = new PrimarkParameterProvider(_helper, _repo);

            SaveSampleInfo(infoDto.sampleDescripBoundSingle!, infoDto.reportNumber!, infoDto.buyer!);

            var dtos = new List<object>();
            foreach (var item in items!)
            {

                paramHelper.CreateParamGeneratorAsync(infoDto, item.itemName!,item.standards!);
                #region
                //var paramInput = new ParamsInput().CreateParamsInput(infoDto, item.itemName!.ToString(), item.standards!.ToString());

                //var wetParams = paramHelper.CreateWetParameters(paramInput, item.samples!);

                //var existWetParam = await _repo.GetWetParamAsync(infoDto.reportNumber!, item.itemName!);

                //if (existWetParam != null)
                //{
                //    //如果存在，更新
                //    _repo.UpdateWetParamAsync(wetParams, existWetParam);
                //}
                //else
                //{
                //    //如果不存在，创建后更新
                //    var newWetParam = await _repo.CreateWetParamAsync(paramInput);
                //    _repo.UpdateWetParamAsync(wetParams, newWetParam);
                //}
                #endregion
                //string? param = await paramHelper.CreateParameters(infoDto, item.itemName!)!;//这里要转化为物理参数去保存

                //dtos.Add(PrimarkParameterMapper.Map(item.itemName!, wetParams ?? new WetParameterIso { ContactItem = item.itemName!, Standard = item.standards }, param!));
                //这里改成动态去数据库去生成好的参数，最后注入到dto返回给前端
            }
            return dtos;
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
