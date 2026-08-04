using Microsoft.EntityFrameworkCore.ChangeTracking;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.MenuContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TestItemContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.Enums;
using System.Text.RegularExpressions;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.MenuContext
{
    public class MenuItem:Entity
    {
        /// <summary>
        /// 测试项目ID
        /// </summary>
        public TestItemId? TestItemId { get; set; }

        /// <summary>
        /// 买家自定义测试项目名称
        /// </summary>
        public string BuyerOwnName { get; set; } = string.Empty;

        /// <summary>
        /// 买家自定义测试项目ID
        /// </summary>
        public string? BuyerModifiedTestItemId { get; set; } = string.Empty;

        /// <summary>
        /// 标准ID
        /// </summary>
        public IEnumerable<StandardId?> StandardIds { get; set; } = Enumerable.Empty<StandardId>();

        /// <summary>
        /// 买家自定义测试方法ID
        /// </summary>
        public string? BuyerModifiedTextMethodId { get; set; } = string.Empty;

        /// <summary>
        /// 买家自定义套餐分组
        /// </summary>
        public string? BuyerModifiedGroup { get; set; } = string.Empty;

        /// <summary>
        /// 限值
        /// </summary>
        private string? _requirement = string.Empty;

        /// <summary>
        /// 限值
        /// 格式必须为: "字段名" 运算符 "值" (例如: "Temperature" > "100")
        /// </summary>
        public string? Requirement
        {
            get => _requirement;
            private set => _requirement = value;
        }

        /// <summary>
        /// 更新限值。如果格式不符合 "字段名" 运算符 "值" 的规范，将抛出格式异常。
        /// </summary>
        /// <param name="requirement">新的限值表达式</param>
        /// <exception cref="ArgumentException">当限值格式不符合正则要求时抛出</exception>
        public void UpdateRequirement(string? requirement)
        {
            if (string.IsNullOrWhiteSpace(requirement))
            {
                _requirement = string.Empty;
                return;
            }

            // 使用正则表达式校验格式
            if (!RequirementRegex.IsMatch(requirement))
            {
                throw new ArgumentException(
                    $"限值格式无效。要求格式为: \"字段名\" 运算符 \"值\" (例如: \"Temperature\" > \"100\")。当前输入: {requirement}",
                    nameof(requirement));
            }

            _requirement = requirement.Trim();
        }

        /// <summary>
        /// 仓储层专用：从持久化数据重建对象状态，跳过业务校验逻辑。
        /// </summary>
        internal void ReconstituteRequirement(string? requirement)
        {
            // 直接赋值给私有字段，绕过正则校验，因为数据库中的数据默认是历史合法的
            _requirement = requirement;
        }

        /// <summary>
        /// 校验 "字段名" 运算符 "值" 格式的正则表达式。
        /// 解释：
        /// ^\s*\"[^"]+\"\s* : 匹配开头，双引号包裹的字段名（字段名不能包含双引号）
        /// (?:=|!=|>=|<=|>|<|contains|equals)\s* : 匹配运算符（支持 =, !=, >=, <=, >, <, contains, equals）
        /// \"[^"]*\"\s*$ : 匹配结尾，双引号包裹的值（值可以为空字符串）
        /// </summary>
        private static readonly Regex RequirementRegex = new Regex(
            @"^\s*""[^""]+""\s*(?:=|!=|>=|<=|>|<|contains|equals)\s*""[^""]*""\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    }
}
