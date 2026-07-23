using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TestItemContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TestItemContext.ValueObj
{
    public class ParamRequireDefinition:ValueObject
    {
        public string ParamName { get; private set; } = string.Empty;
        public string ParamTypeName { get; private set; } = "System.String";
        public bool IsRequired { get; private set; }

        /// <summary>
        /// 通用默认值（所有标准适用）
        /// </summary>
        public string? UniversalDefault { get; private set; }

        /// <summary>
        /// 标准特定默认值（覆盖通用值）
        /// Key: StandardType 字符串
        /// Value: 默认值字符串
        /// </summary>
        public IReadOnlyDictionary<string, string> StandardDefaults { get; private set; }
            = new Dictionary<string, string>();

        private ParamRequireDefinition() { }

        /// <summary>
        /// 工厂创建参数定义
        /// </summary>
        /// <param name="paramName"></param>
        /// <param name="paramTypeName"></param>
        /// <param name="universalDefault"></param>
        /// <param name="isRequired"></param>
        /// <returns></returns>
        public static ParamRequireDefinition Create(
            string paramName,
            string paramTypeName,
            string? universalDefault = null,
            bool isRequired = true)
        {
            return new ParamRequireDefinition
            {
                ParamName = paramName,
                ParamTypeName = paramTypeName,
                UniversalDefault = universalDefault,
                IsRequired = isRequired
            };
        }

        /// <summary>
        /// 设置通用默认值
        /// </summary>
        /// <param name="standardType"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public ParamRequireDefinition WithStandardDefault(
            StandardType standardType,
            string defaultValue)
        {
            var dict = new Dictionary<string, string>(StandardDefaults);
            dict[standardType.ToString()] = defaultValue;
            StandardDefaults = dict;
            return this;
        }

        /// <summary>
        /// 获取默认值（优先标准特定，其次通用）
        /// </summary>
        public string? GetDefaultValue(StandardType? standardType = null)
        {
            if (standardType.HasValue &&
                StandardDefaults.TryGetValue(standardType.Value.ToString(), out var specific))
            {
                return specific;
            }
            return UniversalDefault;
        }

        /// <summary>
        /// 判断当前参数是否适用于指定标准类型
        /// 条件：有通用默认值 或 有该标准的特定默认值
        /// </summary>
        public bool IsApplicableTo(StandardType standardType)
        {
            // 有通用默认值 → 适用所有标准
            if (!string.IsNullOrEmpty(UniversalDefault))
                return true;

            // 有该标准的特定默认值 → 适用
            return StandardDefaults.ContainsKey(standardType.ToString());
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return ParamName;
        }
    }
}
