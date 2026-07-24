using DocumentFormat.OpenXml.Office2010.Excel;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TestItemContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TestItemContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.Enums;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TestItemContext
{
    public sealed class TestItem : AggregateRoot<TestItemId, string>
    {
        /// <summary>
        /// TestItemId
        /// </summary>
        //public TestItemId Id { get; private set; }

        ///<summary>
        /// 英文名称
        ///</summary>
        public string NameEN { get; private set; } = string.Empty;

        /// <summary>
        /// 中文名称
        /// </summary>
        public string NameChn { get; private set; } = string.Empty;

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; private set; } = string.Empty;

        /// <summary>
        /// 测试组别
        /// </summary>
        public TestGroup Group { get; private set; } = TestGroup.Physics;

        /// <summary>
        /// 是否在能力范围内
        /// </summary>
        public bool IsFeasible { get; private set; }

        /// <summary>
        /// 测试项目级别的参数要求定义
        /// </summary>
        private readonly List<ParamRequireDefinition> _paramRequireDefinitions = new();
        public IReadOnlyList<ParamRequireDefinition> ParamRequireDefinitions => _paramRequireDefinitions;

        /// <summary>
        /// 状态
        /// </summary>
        public Status Status { get; private set; }

        /// <summary>
        /// 工厂
        /// </summary>
        /// <param name="id"></param>
        /// <param name="nameEN"></param>
        /// <param name="nameChn"></param>
        /// <param name="description"></param>
        /// <param name="isFeasible"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static TestItem Create(
            TestItemId id,
            string nameEN, 
            string nameChn, 
            string description,
            bool isFeasible,
            TestGroup group,
            Status status)
        {
            //validate
            if (id == null) 
                throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrEmpty(nameEN))
                throw new ArgumentNullException("NameEN cannot be null or empty.", nameof(nameEN));
            if (string.IsNullOrEmpty(nameChn))
                throw new ArgumentNullException("NameChn cannot be null or empty.", nameof(nameChn));
            if (string.IsNullOrEmpty(description))
                throw new ArgumentNullException("Description cannot be null or empty.", nameof(description));

            var testItem = new TestItem
            {
                Id = id,
                NameEN = nameEN,
                NameChn = nameChn,
                Description = description,
                IsFeasible = isFeasible,
                Group = group,
                Status = status
            };
            return testItem;

        }

        /// <summary>
        /// 重建
        /// </summary>
        /// <param name="id"></param>
        /// <param name="nameEN"></param>
        /// <param name="nameChn"></param>
        /// <param name="description"></param>
        /// <param name="isFeasible"></param>
        /// <param name="group"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public static TestItem Reconstitute(
            TestItemId id,
            string nameEN,
            string nameChn,
            string description,
            bool isFeasible,
            TestGroup group,
            Status status,
            List<ParamRequireDefinition> paramRequireDefinitions)
        {
            var testItem = new TestItem
            {
                Id = id,
                NameEN = nameEN,
                NameChn = nameChn,
                Description = description,
                IsFeasible = isFeasible,
                Group = group,
                Status = status
            };

            if (paramRequireDefinitions != null)
            {
                foreach (var p in paramRequireDefinitions.Distinct())
                {
                    if (p != null)
                        testItem._paramRequireDefinitions.Add(p);
                }
            }

            return testItem;
        }

        /// <summary>
        /// 更新聚合根（选择性更新字段；当 paramRequireDefinitions 非 null 时替换整个集合）
        /// </summary>
        /// <param name="nameEN">当为 null 时不修改；当非空但为空白串则抛错</param>
        /// <param name="nameChn">当为 null 时不修改；当非空但为空白串则抛错</param>
        /// <param name="description">当为 null 时不修改；当非空但为空白串则抛错</param>
        /// <param name="isFeasible">当为 null 时不修改</param>
        /// <param name="group">当为 null 时不修改</param>
        /// <param name="status">当为 null 时不修改</param>
        /// <param name="paramRequireDefinitions">当为 null 时保留原定义；否则替换为去重后的新集合</param>
        public void Update(
            string? nameEN = null,
            string? nameChn = null,
            string? description = null,
            bool? isFeasible = null,
            TestGroup? group = null,
            Status? status = null,
            List<ParamRequireDefinition>? paramRequireDefinitions = null)
        {
            if (nameEN != null)
            {
                if (string.IsNullOrWhiteSpace(nameEN))
                    throw new ArgumentException("NameEN cannot be empty.", nameof(nameEN));
                NameEN = nameEN.Trim();
            }

            if (nameChn != null)
            {
                if (string.IsNullOrWhiteSpace(nameChn))
                    throw new ArgumentException("NameChn cannot be empty.", nameof(nameChn));
                NameChn = nameChn.Trim();
            }

            if (isFeasible.HasValue)
                IsFeasible = isFeasible.Value;

            if (group.HasValue)
                Group = group.Value;

            if (status.HasValue)
                Status = status.Value;

            // 如果提供了新的参数定义集合，则替换（去重）
            if (paramRequireDefinitions != null)
            {
                _paramRequireDefinitions.Clear();
                foreach (var p in paramRequireDefinitions.Distinct())
                {
                    if (p != null)
                        _paramRequireDefinitions.Add(p);
                }
            }

            // 若需要，可在此处添加领域事件，例如 TestItemUpdatedEvent
            // AddDomainEvent(new TestItemUpdatedEvent(Id));
        }

        /// <summary>
        /// 根据标准类型，获取适用的参数名列表
        /// </summary>
        public List<string> GetRequiredParamNames(StandardType standardType)
        {
            return _paramRequireDefinitions
                .Where(p => p.IsApplicableTo(standardType))
                .Select(p => p.ParamName)
                .ToList();
        }

        /// <summary>
        /// 根据标准类型，获取完整的参数定义列表
        /// </summary>
        public List<ParamRequireDefinition> GetRequiredParams(StandardType standardType)
        {
            return _paramRequireDefinitions
                .Where(p => p.IsApplicableTo(standardType))
                .ToList();
        }
        /// <summary>
        /// 根据标准类型，获取参数名+默认值的字典
        /// 用于初始化 ConditionPool
        /// </summary>
        public Dictionary<string, object?> GetParamDefaults(StandardType standardType)
        {
            return _paramRequireDefinitions
                .Where(p => p.IsApplicableTo(standardType))
                .ToDictionary(
                    p => p.ParamName,
                    p => (object?)p.GetDefaultValue(standardType));
        }
    }
}
