using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories.BuyerRepos;
using NX_lims_Softlines_Command_System.Infrastructure.Providers.ParamProvider;
using NX_lims_Softlines_Command_System.Infrastructure.Tool;
using static NX_lims_Softlines_Command_System.Infrastructure.Providers.Mapper.OvsParameterMapper;

namespace NX_lims_Softlines_Command_System.Infrastructure.Services
{
    public class OvsService
    {
        private readonly OvsRepository _repo;
        private readonly FiberContentHelper _helper;

        public OvsService(OvsRepository repo, FiberContentHelper helper)
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
                    Parameters = cl.Parameter != null ? new List<string> { cl.Parameter } : null
                })
                .ToList();

            return groupedCheckLists;//去重后
        }

        /// <summary>
        /// 生成参数服务
        /// </summary>
        /// <param name="infoDto"></param>
        /// <returns></returns>
        public async Task<object?> ParameterAsync([FromBody] RequiredInfoDto infoDto)
        {
            // 确保samples不为null且至少有一个元素
            var items = infoDto.items!.Where(x => x.samples != null && x.samples.Any() && x.samples != "").ToList();

            OvsParameterProvider paramHelper = new OvsParameterProvider(_helper, _repo);

            await SaveSampleInfo(infoDto.sampleDescripBoundSingle!, infoDto.reportNumber!, infoDto.buyer!);

            foreach (var item in items!)
            {
                //分测点,逻辑已从CreateParamGeneratorAsync提出

                var samples = item.samples!.Split(',').Select(s => s.Trim()).ToArray();

                foreach (var sample in samples)
                {
                    var (wetParam, normalParam) = await paramHelper.CreateParamGeneratorAsync(infoDto, item.itemName!, item.standards!, sample);

                    OvsParameterMapperMethod.Map(item.itemName!, item.standards!, sample, wetParam, normalParam);
                    //这里改成动态去数据库去生成好的参数，最后注入到dto返回给前端
                }
            }
            var dtos = OvsParameterMapperMethod.GetAllDtos();

            OvsParameterMapperMethod.ClearCache();

            return dtos;
        }

        /// <summary>
        /// 保存SampleInfo服务
        /// </summary>
        /// <param name="sampleDescObjects"></param>
        /// <param name="reportNum"></param>
        /// <param name="buyer"></param>
        private async Task SaveSampleInfo(List<SampleDescObject> sampleDescObjects, string reportNum, string buyer)
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

                await _repo.SaveSampleInfo(sampleObject, reportNum, buyer);
            }
        }
    }
}
