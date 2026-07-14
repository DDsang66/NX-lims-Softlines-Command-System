using NX_lims_Softlines_Command_System.Domain.Share.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext
{
    public sealed class StandardFamily : AggregateRoot
    {
        private readonly List<StandardId> _standardIds = new();
        private readonly List<FormulaId> _formulaIds = new();
        private readonly List<ParamStructureId> _paramStructureIds = new();
        private readonly List<ParamRuleId> _sharedRuleIds = new();

        /// <summary>
        /// 标准族id
        /// </summary>
        public StandardFamilyId Id { get; private set; }

        /// <summary>
        /// 标准族名称
        /// </summary>
        public string StandardFamilyCode { get; private set; } 

        /// <summary>
        /// 标准id集合
        /// </summary>
        public IReadOnlyCollection<StandardId> StandardIds => _standardIds.AsReadOnly();

        /// <summary>
        /// 公式id集合
        /// </summary>
        public IReadOnlyCollection<FormulaId> FormulaIds => _formulaIds.AsReadOnly();

        /// <summary>
        /// 参数结构id集合
        /// </summary>
        public IReadOnlyCollection<ParamStructureId> ParamStructureIds => _paramStructureIds.AsReadOnly();

        /// <summary>
        /// 共享规则id集合
        /// </summary>
        public IReadOnlyCollection<ParamRuleId> SharedRuleIds => _sharedRuleIds.AsReadOnly();

        /// <summary>
        /// 版本
        /// </summary>
        public int Version { get; private set; }

        /// <summary>
        /// 生效日期
        /// </summary>
        public DateTime EffectiveDate { get; private set; }

        private StandardFamily() { }

        public static StandardFamily Create(
            StandardFamilyId id,
            string name
            //IEnumerable<StandardId> standardIds,
            //string version
            )
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name required", nameof(name));

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name required", nameof(name));

            var family = new StandardFamily
            {
                Id = id,
                StandardFamilyCode = name,
                Version = 1,
                EffectiveDate = DateTime.UtcNow
            };

            //if (standardIds != null)
            //{
            //    foreach (var sid in standardIds.Distinct())
            //        family._standardIds.Add(sid);
            //}

            //family.AddDomainEvent(new StandardFamilyCreatedEvent(id, name));

            return family;
        }

        /// <summary>
        /// 重建标准族
        /// </summary>
        /// <param name="id"></param>
        /// <param name="code"></param>
        /// <param name="standardIds"></param>
        /// <param name="formulaIds"></param>
        /// <param name="paramStructureIds"></param>
        /// <param name="sharedRuleIds"></param>
        /// <param name="version"></param>
        /// <param name="effectiveDate"></param>
        /// <returns></returns>
        public static StandardFamily Reconstitute(
            StandardFamilyId id,
            string code,
            List<StandardId> standardIds,
            List<FormulaId> formulaIds,
            List<ParamStructureId> paramStructureIds,
            List<ParamRuleId> sharedRuleIds,
            int version,
            DateTime effectiveDate)
        {
            var family = new StandardFamily
            {
                Id = id,
                StandardFamilyCode = code,
                Version = version,
                EffectiveDate = effectiveDate
            };

            // 通过领域方法或反射填充私有集合
            foreach (var sid in standardIds) family._standardIds.Add(sid);
            foreach (var fid in formulaIds) family._formulaIds.Add(fid);
            foreach (var pid in paramStructureIds) family._paramStructureIds.Add(pid);
            foreach (var rid in sharedRuleIds) family._sharedRuleIds.Add(rid);

            return family;
        }

        /// <summary>
        /// 更新标准族
        /// </summary>
        /// <param name="standardFamilyCode"></param>
        /// <param name="effectiveDate"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public void Update(
            string standardFamilyCode,
            DateTime effectiveDate
            ) 
        {
            if (standardFamilyCode != null)
            {
                if (string.IsNullOrWhiteSpace(standardFamilyCode))
                    throw new ArgumentException("Name required", nameof(standardFamilyCode));
                StandardFamilyCode = standardFamilyCode.Trim();
            }

            if (effectiveDate != null)
            {
                EffectiveDate = effectiveDate;
            }

            // 版本自增
            Version++;
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

        /// <summary>
        /// 添加结构
        /// </summary>
        /// <param name="ruleId"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void AddParamStructure(ParamStructureId structureId)
        {
            if (_paramStructureIds.Contains(structureId))
                throw new InvalidOperationException($"规则 {structureId} 已存在");

            _paramStructureIds.Add(structureId);

            //AddDomainEvent(new SharedRuleAddedToFamilyEvent(Id, ruleId));
        }

        // 领域方法：变更版本
        public void UpdateVersion()
        {
            var newVersion = Version++;

            Version = newVersion;

            //AddDomainEvent(new FamilyVersionUpdatedEvent(Id, oldVersion, newVersion));
        }
    }
}
