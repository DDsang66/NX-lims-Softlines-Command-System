using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TemplateContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Service
{
    /// <summary>
    /// 模板查询器（领域服务）
    /// </summary>
    public interface ITemplateSelectService:IScopedDependency
    {
        /// <summary>
        /// 根据索引条件查询唯一模板
        /// </summary>
        /// <param name="conditions">索引条件，如 { "TestMethod": "4N", "Site": "NB" }</param>
        /// <returns>命中的模板；未命中返回 null</returns>
        /// <exception cref="InvalidOperationException">命中多个模板时抛出</exception>
        Template? FindByIndex(IReadOnlyDictionary<string, object?> conditions, string? preFilterKey = null);

        /// <summary>
        /// 根据文件路径查询模板
        /// </summary>
        Template? FindByUrl(string templateUrl);
    }
}
