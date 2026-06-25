using NX_lims_Softlines_Command_System.Domain.Aggregeates.Standard;
using NX_lims_Softlines_Command_System.Domain.Shared.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext
{
    public sealed class StandardFamily : IAggregateRoot
    {
        public StandardFamilyId Id { get; private set; }
        public string Name { get; private set; }  // "ISO 6330 Family"
        public List<StandardId> StandardIds { get; private set; }  
        public List<FormulaId> FormulaIds { get; private set; }  // 该标准族的所有公式
        public List<ParamStructureId> ParamStructureIds { get; private set; }
        public List<ParamRuleId> SharedRuleIds { get; private set; }  // 共享规则
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

            var ids = standardIds?.ToList() ?? new List<StandardId>();
            if (!ids.Any()) throw new ArgumentException("至少需要一个标准", nameof(standardIds));

            return new StandardFamily
            {
                Id = id,
                Name = name,
                StandardIds = ids,
                FormulaIds = new List<FormulaId>(),
                ParamStructureIds = new List<ParamStructureId>(),
                SharedRuleIds = new List<ParamRuleId>(),
                Version = version,
                EffectiveDate = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 判断是否包含指定标准（通过 ID）
        /// </summary>
        public bool ContainsStandard(StandardId standardId) => StandardIds.Contains(standardId);

        /// <summary>
        /// 添加标准到族
        /// </summary>
        public void AddStandard(StandardId standardId)
        {
            if (StandardIds.Contains(standardId))
                throw new InvalidOperationException($"标准 {standardId} 已存在于当前族");

            StandardIds.Add(standardId);
        }

        /// <summary>
        /// 从族中移除标准
        /// </summary>
        public void RemoveStandard(StandardId standardId)
        {
            StandardIds.Remove(standardId);
        }

        /// <summary>
        /// 添加公式
        /// </summary>
        public void AddFormula(FormulaId formulaId)
        {
            if (!FormulaIds.Contains(formulaId))
                FormulaIds.Add(formulaId);
        }

        /// <summary>
        /// 添加共享规则
        /// </summary>
        public void AddSharedRule(ParamRuleId ruleId)
        {
            if (!SharedRuleIds.Contains(ruleId))
                SharedRuleIds.Add(ruleId);
        }

        // 领域方法：变更版本
        public void UpdateVersion(string newVersion)
        {
            if (string.IsNullOrWhiteSpace(newVersion)) throw new ArgumentException("Version required");
            Version = newVersion;
        }
    }
}
