using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TemplateContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Service
{
    public interface ITemplateIdGenerator:IScopedDependency
    {
        /// <summary>
        /// 生成模板唯一标识
        /// </summary>
        /// <param name="testType">测试类型 (从 DTO 传入)</param>
        /// <param name="fileName">文件名称 (即模板名称)</param>
        /// <returns>生成的 TemplateId</returns>
        TemplateId Generate(string testType, string fileName);
    }
}
