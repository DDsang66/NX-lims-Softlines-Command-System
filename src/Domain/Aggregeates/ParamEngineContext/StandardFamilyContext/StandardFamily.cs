using NX_lims_Softlines_Command_System.Domain.Aggregeates.Standard;
using NX_lims_Softlines_Command_System.Domain.Shared.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext
{
    public sealed class StandardFamily : IAggregateRoot
    {
        private readonly List<StandardId> _standardIds = new();
        private readonly List<FormulaId> _formulaIds = new();
        private readonly List<ParamStructureId> _paramStructureIds = new();
        private readonly List<ParamRuleId> _sharedRuleIds = new();
        public StandardFamilyId Id { get; private set; }
        public string StandardFamilyCode { get; private set; }  // "ISO 6330 Family"
        public IReadOnlyCollection<StandardId> StandardIds => _standardIds.AsReadOnly();
        public IReadOnlyCollection<FormulaId> FormulaIds => _formulaIds.AsReadOnly();
        public IReadOnlyCollection<ParamStructureId> ParamStructureIds => _paramStructureIds.AsReadOnly();
        public IReadOnlyCollection<ParamRuleId> SharedRuleIds => _sharedRuleIds.AsReadOnly();
        public string Version { get; private set; }
        public DateTime EffectiveDate { get; private set; }

        private StandardFamily() { }

        public static StandardFamily Create(
            StandardFamilyId id,
            string name,
            IEnumerable<StandardId> standardIds,
            string version)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required", nameof(name));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name required", nameof(name));

            var family = new StandardFamily
            {
                Id = id,
                StandardFamilyCode = name,
                Version = version,
                EffectiveDate = DateTime.UtcNow
            };

            if (standardIds != null)
            {
                foreach (var sid in standardIds.Distinct())
                    family._standardIds.Add(sid);
            }

            //family.AddDomainEvent(new StandardFamilyCreatedEvent(id, name));

            return family;
        }

        /// <summary>
        /// 判断是否包含指定标准（通过 ID）
        /// </summary>
        public bool ContainsStandard(StandardId standardId) => _standardIds.Contains(standardId);

        /// <summary>
        /// 判断是否已有先行标准
        /// </summary>
        public bool ExistStandard() => _standardIds.Count> 0;

        /// <summary>
        /// 添加标准到族
        /// </summary>
        public void AddStandard(StandardId standardId)
        {
            if (_standardIds.Contains(standardId))
                throw new InvalidOperationException($"标准 {standardId} 已存在");

            _standardIds.Add(standardId);

            //AddDomainEvent(new StandardAddedToFamilyEvent(Id, standardId));
        }

        /// <summary>
        /// 从族中移除标准
        /// </summary>
        public void RemoveStandard(StandardId standardId)
        {
            if (!_standardIds.Remove(standardId))
                throw new InvalidOperationException($"标准 {standardId} 不存在");

            //AddDomainEvent(new StandardRemovedFromFamilyEvent(Id, standardId));
        }

        /// <summary>
        /// 添加公式
        /// </summary>
        public void AddFormula(FormulaId formulaId)
        {
            if (_formulaIds.Contains(formulaId))
                throw new InvalidOperationException($"公式 {formulaId} 已存在");

            _formulaIds.Add(formulaId);

            //AddDomainEvent(new FormulaAddedToFamilyEvent(Id, formulaId));
        }

        /// <summary>
        /// 添加共享规则
        /// </summary>
        public void AddSharedRule(ParamRuleId ruleId)
        {
            if (_sharedRuleIds.Contains(ruleId))
                throw new InvalidOperationException($"规则 {ruleId} 已存在");

            _sharedRuleIds.Add(ruleId);

            //AddDomainEvent(new SharedRuleAddedToFamilyEvent(Id, ruleId));
        }

        // 领域方法：变更版本
        public void UpdateVersion(string newVersion)
        {
            if (string.IsNullOrWhiteSpace(newVersion))
                throw new ArgumentException("Version required", nameof(newVersion));

            var oldVersion = Version;

            Version = newVersion;

            //AddDomainEvent(new FamilyVersionUpdatedEvent(Id, oldVersion, newVersion));
        }
    }
}
