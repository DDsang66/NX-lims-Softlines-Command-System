using NX_lims_Softlines_Command_System.Application.DTO;

namespace NX_lims_Softlines_Command_System.Application.Services.BuyerService
{
    public class CommonBuyerService
    {
        ///<summary>
        ///获取买家的套餐信息
        /// </summary>
        public async Task<object?> ShowItem(RequiredInfoDto infoDto) 
        {
            //请求dto实例化成实体（通过mapping),实际上择不是一个实体，而是应该去调用通用查询服务
            //调用查询服务中的ShowItem方法
            //返回结果
            return null;
        }

        ///<summary>
        ///获取生成CheckList
        /// </summary>
        public async Task<object?> CreateCheckList(RequiredInfoDto infoDto)
        {
            //调用请求服务解析器，去空、去大小写敏感、去特殊字符
            //在执行生成CheckList之前，需要先判断是否已经生成过CheckList，如果已经生成过，则直接返回
            //调用生成CheckList服务
            //获取CheckList，调用DTO创建器，返回前端需要的DTO
            //返回结果
            return null;
        }

    }
}
