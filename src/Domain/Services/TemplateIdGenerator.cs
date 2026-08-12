using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TemplateContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Services
{
    public class TemplateIdGenerator: ITemplateIdGenerator,IScopedDependency
    {
        /// <summary>
        /// 生成模板唯一标识
        /// </summary>
        /// <param name="testType"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public TemplateId Generate(string testType, string fileName)
        {
            if (string.IsNullOrWhiteSpace(testType))
                throw new ArgumentException("TestType 不能为空", nameof(testType));

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("文件名称不能为空", nameof(fileName));

            // 1. 处理文件名称：移除可能引起歧义或非法的字符，替换空格
            string safeFileName = fileName.Replace(" ", "_");

            // 2. 生成时间戳 (格式：yyyyMMddHHmmss)
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

            // 3. 拼装字符串: TPL-{TestType}-{文件名称}-v1-{时间戳}
            string idValue = $"TPL-{testType}-{safeFileName}-NB-{timestamp}";

            // 4. 返回强类型 ID
            return new TemplateId(idValue);
        }
    }
}
