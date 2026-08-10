# 限界上下文划分

本文件定义系统中的限界上下文（Bounded Context），明确每个上下文的职责、聚合根、领域事件、责任边界与与其他上下文的集成方式。目标是保证领域模型清晰、数据所有权明确、跨上下文一致性可控（优先采用最终一致性）。

## 概览
系统基于 DDD 分层（Domain / Application / Infrastructure），并包含一个可版本化的规则/参数引擎。上下文划分以业务能力与数据所有权为准，尽量将变化快速、规则频繁的逻辑隔离到独立上下文。

---

## StandardManagement（标准管理）
- 职责：管理检测标准、参数 Schema、规则 DSL 与规则版本；提供规则编译/校验服务（Tokenize → Parse → Compile → Execute）。
- 聚合根：`Standard`、`ParameterSchema`、`ParameterRule`（支持版本与变更审计）。
- 领域事件：`StandardCreated`、`StandardUpdated`、`RulePublished`、`RuleDeprecated`
- 数据所有权：标准、规则与 Schema 的主数据归此上下文所有。
- 对外契约：通过只读 API / 事件导出已发布规则的元数据与编译产物（可序列化以便缓存）。

---

## BuyerManagement（买家管理）
- 职责：买家信息、买家配置（Buyer-specific overrides）、买家套餐（BuyerMenu）的生命周期与访问控制。
- 聚合根：`Buyer`、`BuyerMenu`（指向 Menu/标准的引用） 、`BuyerSetting`
- 领域事件：`BuyerRegistered`、`BuyerMenuConfigured`、`BuyerSettingChanged`
- 数据所有权：买家相关主数据与与买家相关的配置权归此上下文所有。
- 对外契约：暴露买家身份、权限与买家级别的菜单/规则覆盖点。

---

## MenuManagement（套餐/菜单管理）
- 职责：套餐（Menu）与菜单项（MenuItem）的建模、编辑、验证（含 Requirement 正则/DSL 校验）、聚合一致性维护与持久化。
- 聚合根：`Menu`（聚合根）、`MenuItem`（实体）
- 领域事件：`MenuCreated`、`MenuItemAdded`、`MenuItemUpdated`、`MenuDeleted`
- 数据所有权：套餐定义的主数据归此上下文所有。
- 本项目示例：`src/Application/Service/MenuContext/*` 与 `src/Infrastructure/Data/Repository/MenuRepository.cs`
- 对外契约：应用服务 API（创建/修改/查询/删除），在规则变化或标准变更时订阅/接收 StandardManagement 的事件以触发再验证或重计算。


---

## ParamEngine（参数规则引擎 / 计算上下文）
- 职责：将规则 DSL 编译为可执行规则（中间表示或表达式树），执行参数匹配、评估与参数集合（ParamSet）生成；支持缓存编译结果与版本回溯。
- 组件：Tokenizer、Parser、Compiler、Execution Engine、Compensation/ApplyRules Service。
- 领域接口：`IParamRule`（`Matches(ConditionPool)`、`Evaluate(ConditionPool)`），`ParamGenerationEngine`。
- 事件与集成：接受 `StandardRule` 或 `Buyer` 配置变更事件以触发重新编译/缓存失效与回算任务。
- 可运维性：规则与 Schema 均版本化并支持热加载、回滚与模拟（sandbox）。

### ParamEngine（参数规则引擎 / 计算上下文）——聚合根职责与对应服务

本小节详细说明 ParamEngine 内的主要聚合根、它们的职责/不变式/功能，以及与之对应的仓储、领域服务与应用层协调器的映射关系与典型调用流。

-----------------------------------------------------------------------
聚合根：`ParamStructure`
- 职责
  - 描述生成某一参数所需的数据结构（`ParamSchema`）、适用的标准族（`StandardFamilyId`）与适用的规则列表（`ApplicableRuleIds`）。
  - 管理参数生效周期（Draft/Active/Deprecated 等）、版本与生效日期。
  - 提供主参数定义入口（`MainParamDefinition`），并触发结构层面变更事件（例如 `ParamStructureUpdatedEvent`）。
- 关键字段
  - `Id: ParamStructureId`、`ParamName`、`Schema: ParamSchema`、`ApplicableRuleIds`、`FormulaId?`、`Status`、`EffectiveDate`
- 核心行为（方法）
  - `Create(...)`：工厂，集中做参数名与 Schema 校验（不变式）。
  - `Reconstitute(...)`：从持久化恢复（仓储使用）。
  - `Update(...)`：更改名称/Schema 并触发领域事件。
  - `Active()/Draft()/Deprecated()/Superseded()`：状态转换。
- 不变式
  - `Schema` 必须包含至少一个主参数定义；激活状态下禁止无效 Schema。
- 对外契约 / 映射服务
  - 仓储接口：`IParamStructureRepository`（`GetByIdAsync`、`GetByParamNameAsync`、`AddAsync`、`UpdateAsync`等）
  - 应用服务：`ParamStructureAppService`（创建/编辑/发布/回退等操作）
  - 事件订阅者：`Formula` 或 `ParamRule` 生命周期变化监听（当结构变更影响公式/规则时触发校验或回算）

-----------------------------------------------------------------------
聚合根：`ParamRule`
- 职责
  - 描述单条生成规则的匹配模式（`ConditionPattern`）、优先级、生成的 `ParamValue`、是否 `StopOnMatch`、激活状态等。
  - 在运行时负责“声明式”匹配逻辑（`Match(ConditionPool, accessor, comparer)`）——该方法为无副作用的只读判断。
  - 提供规则生命周期的管理（创建/更新/激活/停用）。
- 关键字段
  - `Id: ParamRuleId`、`ParamName`、`Pattern: ConditionPattern`、`Result: ParamValue`、`Priority`、`StopOnMatch`、`IsActive`
- 核心行为
  - `Create(...)`、`Reconstitute(...)`、`Update(...)`
  - `Match(...)`：运行时使用注入的 `IConditionPoolDomainService`（用于读取 `ConditionPool`）和 `IValueComparer`（执行比较/布尔逻辑）完成匹配判断。
- 不变式
  - 激活时必须有有效的匹配条件与有效结果值；`Priority` >= 1。
- 对外契约 / 映射服务
  - 仓储接口：`IParamRuleRepository`（按公式/ID 查询、批量加载）
  - 应用服务：`ParamRuleAppService`（规则的增删改查、激活发布）
  - 运行时由 `IParamGenerationEngine` 提供规则集合并行/顺序执行

-----------------------------------------------------------------------
聚合根：`Formula`
- 职责
  - 将若干 `ParamStructure` 组合为“公式”级别的模板/表达（`ExpressionTemplate`），并定义公式的条件字段集合（`ConditionFields`）。
  - 管理公式版本、适用标准族与生效规则。
  - 当公式变更时，通知下游（如 `ParamStructure` 校验、规则重新编译或触发回算任务）。
- 关键字段
  - `Id: FormulaId`、`Name`、`ParamName`、`ConditionFields`、`ExpressionTemplate`、`ParamStructureIds`、`Version`、`IsActive`
- 核心行为
  - `Create(...)`、`Reconstitute(...)`、`Update(...)`、版本/激活管理
- 对外契约 / 映射服务
  - 仓储：`IFormulaRepository`
  - 应用服务：`FormulaAppService` / `FormulaQueryService`
  - 在 `ParamGenerationCoordinator` 中负责加载公式模板以便做更高层次的规则组合与校验

-----------------------------------------------------------------------
聚合根：`ConditionPool`
- 职责
  - 在一次计算上下文中持有所有候选条件（键值对）与元信息（关联 CheckList、使用的测点列表、状态等）。
  - 提供对 Condition 值的安全访问（`TryGet` / `GetConditionValue<T>`）与合并逻辑（`MergeFrom`）。
  - 管理条件池状态（Draft、Validated、Expired）。
- 关键字段
  - `Id: ConditionPoolId`、`CheckListId`、`Conditions: Dictionary<string, object?>`、`TestPoints`、`CreatedAt`、`Status`
- 核心行为
  - `Create(...)`、`Reconstitute(...)`、`Update(...)`、`MergeFrom(...)`、状态变更方法
  - 校验与字段存在性检查（`HasCondition`）
- 对外契约 / 映射服务
  - 应用服务：`ConditionPoolAppService`（创建/提交/验证）
  - 领域服务：`IConditionPoolDomainService`（运行时访问/抽象化 Condition 的获取逻辑）
  - 验证服务：`IConditionPoolValidateService`（在 `ParamGenerationCoordinator` 调用以确保条件满足结构/公式要求）

-----------------------------------------------------------------------
支撑值对象：`ParamSet`
- 职责
  - 运行时保存引擎计算出的参数键值对集合，支持合并、覆盖、回退策略与补偿接口（`SetValueOrFallback`）。
- 对外契约 / 映射服务
  - 作为 `ParamGenerationCoordinator` 输出，传递到补偿/验证/持久化逻辑。

-----------------------------------------------------------------------
对应的服务与职责（概览）
- 仓储（Repository）
  - `IParamStructureRepository`：读写 `ParamStructure`（`GetById`、按 ParamName 查询、列表、Add/Update）
  - `IParamRuleRepository`：按公式/ID 查询规则集，支持批量加载
  - `IFormulaRepository`：公式的查询/保存
  - `IConditionPoolRepository`（可选）：在需要持久化 ConditionPool 时提供持久化支持
  - 基础设施实现位置：`src/Infrastructure/Data/Repository/*`（例如 `ParamStructureRepository`、`ParamRuleRepository`、`FormulaRepository`）

- 领域服务（Domain Services）
  - `IParamGenerationEngine`（实现类如 `ParamGenerationEngine`）：将 `ConditionPool` 与已加载的 `ParamRule` 集合作为输入，返回 `ParamSet`（纯计算、无副作用）。
  - `IConditionPoolDomainService`：运行时抽象，用于安全读 ConditionPool、支持路径取值与复杂访问逻辑（`ParamRule.Match` 使用）。
  - `IValueComparer`：比较/布尔表达式执行器（Equal/Compare/In/Composite 运算）。
  - `IParamCompensationService`：补偿缺失或非法参数（例如 `CompensateParamWithStructure`、`CompensateWithItemDefinitions`）。
  - `IParamValidateService`：对生成结果与结构、测试项定义进行业务校验（类型、范围、是否缺失）。

- 应用层（协调器 / AppService）
  - `ParamGenerationCoordinator`（`src/Application/Service/ParamGenerateService`）：
    - 协调跨聚合加载（`ParamStructure`、`Formula`、`ParamRule`、`TestItem` 等）。
    - 调用 `IConditionPoolValidateService` 做前置验证。
    - 调用 `IParamGenerationEngine.Generate(pool, rules)` 获得 `ParamSet`。
    - 调用 `IParamValidateService` 进行验证并通过 `IParamCompensationService` 执行必要补偿。
    - 返回 `Result<ParamSet>` 给上层（例如检测执行流程或 API 层）。
  - 此外存在的 AppService：`ParamRuleAppService`、`ParamStructureAppService`、`FormulaAppService` 用于规则/结构/公式的 CRUD 与发布流程。

-----------------------------------------------------------------------
典型调用流程（示例）
1. 编辑/发布流程（管理端）
   - 通过 `ParamStructureAppService` 或 `ParamRuleAppService` 修改聚合，仓储保存后触发领域事件（如 `ParamStructureUpdated`、`RulePublished`）。
   - `ParamEngine` 的订阅者（或后台任务）响应事件触发规则编译/缓存刷新。

2. 运行时计算（执行端）
   - 上游创建 `ConditionPool`（从 CheckList / TestOrder 提取数据）。
   - 调用 `ParamGenerationCoordinator.GenerateAsync(structure, pool)`：
     - 验证 `ConditionPool`（`IConditionPoolValidateService`）。
     - 加载相关规则（`IParamRuleRepository.GetByIdsAsync`）。
     - 调用 `IParamGenerationEngine.Generate(pool, rules)`（返回 `ParamSet`）。
     - 验证并补偿（`IParamValidateService`、`IParamCompensationService`）。
     - 返回最终 `ParamSet`。

-----------------------------------------------------------------------
设计与演进建议（针对聚合与服务的实践）
- 将“运行时行为”与“持久化/管理行为”分离：`ParamRule.Match`、`IParamGenerationEngine.Generate` 为无副作用的计算；CRUD 与发布、版本管理通过 AppService/Repository 完成。
- 规则编译：若规则 DSL 复杂，建议引入编译步骤（`Parser` → `Compiler`）产出中间表示并缓存（降低运行时开销）。
- 规约（Invariants）：在聚合工厂/Update 中尽早校验并抛出具体错误，避免在运行时出现隐藏错误。
- 日志与可观测性：引擎入口记录输入摘要与耗时；规则编译/缓存刷新等操作需保留审计记录与版本号。

-----------------------------------------------------------------------
引用代码位置（快速导航）
- `ParamStructure`：`src/Domain/Aggregeates/ParamEngineContext/ParamStructureContext/ParamStructure.cs`
- `ParamRule`：`src/Domain/Aggregeates/ParamEngineContext/ParamRuleContext/ParamRule.cs`
- `Formula`：`src/Domain/Aggregeates/ParamEngineContext/FormulaContext/Formula.cs`
- `ConditionPool`：`src/Domain/Aggregeates/ParamEngineContext/ConditionPoolContext/ConditionPool.cs`
- `ParamGenerationCoordinator`：`src/Application/Service/ParamGenerateService/ParamGenerationCoordinator.cs`
- 接口：`IParamGenerationEngine` / `IParamCompensationService` / `IParamValidateService` / `IConditionPoolValidateService`
- 仓储接口：`src/Domain/Contract/Repository/ParamEngineContext/*`

-----------------------------------------------------------------------
以上为 `ParamEngine` 下聚合根的职责、功能与对应服务的详细说明，便于团队在开发/审查/测试时快速定位责任边界与实现。

---

## TestingExecution（检测执行 / 结果管理）
- 职责：检测任务分派、样本/委托单（CheckList 或 TestOrder）、结果录入、报告生成与结果归档。
- 聚合根：`TestTask`、`TestResult`、`Report`
- 领域事件：`TaskAssigned`、`ResultEntered`、`ReportGenerated`
- 与其它上下文的交互：读取 MenuManagement 的套餐定义、调用 ParamEngine 校验/计算、将结果通知外部系统。

---

## Infrastructure（基础设施上下文）
- 职责：提供持久化、外部系统集成、消息总线、缓存与监控实现；不包含业务规则。
- 内容示例：EF Core `dbContext`、仓储实现（`MenuRepository`）、第三方 API 适配器。
- 注意点：Infrastructure 仅为领域提供实现，不应包含业务决策逻辑；跨上下文的事件发布/订阅与消息持久化由此层负责。

---

## Shared Kernel（共享内核）
- 职责：放置多个上下文公用的小型代码或契约（如 `Result` 类型、基础领域基类 `Entity` / `AggregateRoot`、领域事件接口），但要避免不断膨胀。
- 建议：共享只用于真正不可避免的基础构件；领域模型与规则应尽量在其所属上下文内独立演进。

---

## 边界、集成与一致性
- 同步调用：
  - 应用层内部短期一致性调用（例如：创建 Menu 后立即查询）可采用同步接口。
- 事件与最终一致性：
  - 跨上下文变更应使用领域事件或消息总线（事件驱动）实现最终一致性。例如：`RulePublished` 事件触发 ParamEngine 的编译任务、触发 MenuManagement 的重新验证。
- 事务边界：
  - 事务（UnitOfWork）仅跨同一上下文的写操作。跨上下文写入采用事件/补偿流程（Saga/Orchestration）保证业务流程完整性。

---

## 一致性策略示例
- 标准变更流程：
  1. 在 `StandardManagement` 中提交并发布新规则（事务内完成）。
  2. 发布事件 `RulePublished{ruleId, version}` 到消息总线。
  3. `ParamEngine` 订阅事件并编译新版本规则，编译通过后更新缓存并发布 `RuleCompiled` 事件。
  4. `MenuManagement` 订阅编译/发布事件，按需触发 Menu 校验或向业务用户发起变更审阅。
- 菜单编辑流程：
  - Menu 的所有增删改由 `MenuManagement` 聚合根处理并保证聚合内一致性；同步写入数据库通过同一 `UnitOfWork` 提交。

---

## 数据拥有权与查询模型
- 写权归属：每个实体/聚合的写权由其所属上下文拥有（单一写源）。
- 读模型：为查询性能或跨上下文聚合视图，可在独立的查询上下文构建物化视图或投影（Read Models），由订阅上下文事件进行更新。

---

## 部署与操作关注
- 可将高频变更或规则密集的上下文独立部署（例如 ParamEngine 独立服务），以支持热更新与弹性扩容。
- 对规则变更引入审计与回滚能力；在生产变更前提供模拟/回归计算路径。

---

## 参考（代码位置示例）
- 领域基类：`src/Domain/Share/Entity.cs`, `src/Domain/Share/AggregateRoot.cs`
- Menu 示例：`src/Application/Service/MenuContext/*`, `src/Infrastructure/Data/Repository/MenuRepository.cs`
- 规则引擎草图和说明：`doc/guides/Code Rebuild Guidance.doc`

---

本文件为架构级划分建议，随业务演进与实现反馈应持续调整  