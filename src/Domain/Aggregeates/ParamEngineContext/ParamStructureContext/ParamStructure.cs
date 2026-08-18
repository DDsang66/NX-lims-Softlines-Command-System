using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Domain.Share.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.BuyerContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Events;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.Enums;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext
{
    public sealed class ParamStructure : AggregateRoot<ParamStructureId,string>
    {
        /// <summary>
        /// 参数结构ID
        /// </summary>
        //public ParamStructureId Id { get; private set; }

        ///<summary>
        /// 适用标准族id集合
        ///<summary>
        private readonly List<StandardFamilyId?> _standardFamilyIds = new();

        /// <summary>
        /// 挂载的规则
        /// </summary>
        private readonly List<ParamRuleId> _ruleIds  = new();

        /// <summary>
        /// 适用买家id集合
        /// </summary>
        private readonly List<BuyerId?> _buyerIds = new();

        /// <summary>
        /// 适用标准族
        /// </summary>
        public IReadOnlyCollection<StandardFamilyId?> StandardFamilyIds => _standardFamilyIds.AsReadOnly();

        /// <summary>
        /// 适用规则
        /// </summary>
        public IReadOnlyCollection<ParamRuleId> ApplicableRuleIds => _ruleIds.AsReadOnly();

        /// <summary>
        /// 买家关联 Id
        /// </summary>
        public IReadOnlyCollection<BuyerId?> BuyerIds => _buyerIds.AsReadOnly();

        /// <summary>
        /// 适用公式
        /// </summary>
        public FormulaId? FormulaId {get; private set; }
        
        /// <summary>
        /// 参数名称
        /// </summary>
        public string ParamName { get; private set; } = string.Empty;  // 例如 "Ballast"
        
        /// <summary>
        /// 参数定义
        /// </summary>
        public ParamSchema Schema { get; private set; }

        /// <summary>
        /// 状态
        /// </summary>
        public Status Status { get; private set; } = Status.Draft;

        /// <summary>
        /// 公式所属的引擎层级（默认 Standard 层）
        /// </summary>
        public EngineLayer EngineLayer { get; private set; } = EngineLayer.Standard;

        /// <summary>
        /// 生效日期
        /// </summary>
        public DateTime EffectiveDate { get; private set; }

        /// <summary>
        /// 工厂：创建单参数结构，保证 Schema 至少包含一项主参数定义
        /// </summary>
        public static ParamStructure Create(
            ParamStructureId id,
            IEnumerable<StandardFamilyId?> standardFamilyIds,
            FormulaId? formulaId,
            string paramName,
            ParamSchema schema,
            IEnumerable<ParamRuleId?> ruleIds,
            IEnumerable<BuyerId?> buyerIds,
            EngineLayer engineLayer,
            DateTime? effectiveDate = null)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrWhiteSpace(paramName))
                throw new ArgumentException("paramName required", nameof(paramName));
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));
            if (schema.RequiredParam == null)
                throw new ArgumentException("Schema must contain at least one ParamDefinition", nameof(schema));

            var ps = new ParamStructure
            {
                Id = id,
                ParamName = paramName.Trim(),
                FormulaId = formulaId,
                Schema = schema,
                Status = Status.Draft,
                EngineLayer = engineLayer,
                EffectiveDate = effectiveDate ?? DateTime.UtcNow
            };

            // 2. 初始化集合：将传入的 Id 集合添加到私有字段中
            if (standardFamilyIds != null)
            {
                foreach (var familyId in standardFamilyIds.Where(f => f != null))
                {
                    ps._standardFamilyIds.Add(familyId);
                }
            }

            if (ruleIds != null) 
            {
                foreach (var ruleId in ruleIds.Where(f => f != null)) 
                {
                    ps._ruleIds.Add(ruleId);
                }
            }

            if (buyerIds != null) 
            {
                foreach (var buyerId in buyerIds.Where(f => f != null)) 
                { 
                    ps._buyerIds.Add(buyerId);
                }
            }

            return ps;
        }

        /// <summary>
        /// 从数据库重建
        /// </summary>
        /// <param name="id"></param>
        /// <param name="familyId"></param>
        /// <param name="formulaId"></param>
        /// <param name="paramName"></param>
        /// <param name="schema"></param>
        /// <param name="ruleIds"></param>
        /// <param name="effectiveDate"></param>
        /// <returns></returns>
        public static ParamStructure Reconstitute(
            ParamStructureId id,
            IEnumerable<StandardFamilyId?> standardFamilyIds, // 3. 修改为集合
            IEnumerable<ParamRuleId>? ruleIds,
            IEnumerable<BuyerId?> buyerIds,
            FormulaId? formulaId,               // 4. 修改为集合
            string paramName,
            ParamSchema schema,
            Status status,
            EngineLayer engineLayer,
            DateTime effectiveDate
            )
        {
            var ps = new ParamStructure
            {
                Id = id,
                ParamName = paramName.Trim(),
                FormulaId = formulaId,
                Status = status,
                Schema = schema,
                EngineLayer = engineLayer,
                EffectiveDate = effectiveDate
            };

            // 5. 重建集合：将数据库读取的 Id 集合还原到私有字段中
            if (standardFamilyIds != null)
            {
                foreach (var familyId in standardFamilyIds.Where(f => f != null))
                {
                    ps._standardFamilyIds.Add(familyId);
                }
            }

            if (ruleIds != null)
            {
                foreach (var ruleId in ruleIds.Where(f => f != null))
                {
                    ps._ruleIds.Add(ruleId);
                }
            }

            if (buyerIds != null)
            {
                foreach (var buyerId in buyerIds.Where(f => f != null)) 
                {
                    ps._buyerIds.Add(buyerId);
                }
            }

            return ps;
        }

        /// <summary>
        /// 更新除外键以外的字段
        /// </summary>
        public void Update(string paramName, ParamSchema schema)
        {
            if (string.IsNullOrWhiteSpace(paramName))
                throw new ArgumentException("paramName required", nameof(paramName));

            if (schema == null)
                throw new ArgumentNullException(nameof(schema));

            ParamName = paramName.Trim();
            Schema = schema;
            EffectiveDate = DateTime.UtcNow;

            //领域事件，通知对应formula参数结构更新
            AddDomainEvent(new ParamStructureUpdatedEvent(Id, ParamName, Schema));
        }

        public void CombineToFormula() 
        {

            //领域事件 add
        }


        /// <summary>
        /// 主参数定义（Schema.RequiredParam）
        /// </summary>
        public ParamDefinition MainParamDefinition => Schema.RequiredParam;

        /// <summary>
        /// 更新生效日期
        /// </summary>
        /// <param name="effective"></param>
        public void UpdateEffectiveDate(DateTime effective) => EffectiveDate = effective;

        /// <summary>
        /// 将当前结构设置为“激活”状态
        /// </summary>
        public void Active() => this.Status = Status.Active;

        /// <summary>
        /// 将当前j结构回退为草稿状态，要求必须满足某些条件，
        /// </summary>
        /// <returns></returns>
        public void Draft() => this.Status = Status.Draft;

        /// <summary>
        /// 将当前结构设置为“已废弃”状态
        /// </summary>
        public void Deprecated() => this.Status = Status.Deprecated;

        /// <summary>
        /// 将当前结构设置为“被替代”状态
        /// </summary>
        public void Superseded() => this.Status = Status.Superseded;

        /// <summary>
        /// 将当前结构设置为“待审核”状态
        /// </summary>
        public void Pending() => this.Status = Status.Pending;
    }
}
