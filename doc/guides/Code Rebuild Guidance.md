# 代码重构指引 —— 参数引擎（DDD）

> 本文档由旧版《Code Rebuild Guidance.doc》按当前 `src/` 新架构修订。
> 旧文档编写于参数引擎设计之初，彼时架构尚未落地；现标准参数层已在 `src/` 实现，本文档以 **实际代码为准** 描述已实现部分，并将指引重心转移到 **尚未实现的买家参数层**。
>
> 状态图例：✅ 已实现（可直接参考代码） · 🚧 待实现（本文档的设计目标）

---

## 0. 状态总览

| 层 | 状态 | 说明 |
|---|---|---|
| 标准参数层（Standard Layer） | ✅ 已实现 | `src/Domain/Aggregeates/ParamEngineContext/*` + 生成协调器 + DSL 管道 + 补偿/校验 |
| 买家参数层（Buyer Modify Layer） | 🚧 未实现 | 新架构仅预留钩子；旧层 `Application/Services/BuyerService/*` 待迁移 |

---

## 1. 参数引擎设计初衷（保留原文意图）

目的是用一个较完善的类集合解决纺织品复杂的业务逻辑。这里的“参数”指**测试参数（测试条件）**，例如 Dimensional Stability to Washing 中的水洗程序、温度、洗涤剂、干燥方式等。

鉴于买家定制化要求过高，参数引擎分上下两层：

```
参数引擎（Param Rule Engine）
   |——买家参数层（Buyer Modify Layer）   ← 🚧 未实现
   |——标准参数层（Standard Layer）        ← ✅ 已实现
```

- **底层**（标准参数层）依据国际化标准（ISO、AATCC、GB、JIS 等）维持不变式；
- **上层**（买家参数层）只输出买家特定的参数，对底层的标准参数进行**覆盖**。

**为什么这样设计？** 例：ISO 6330 中，洗涤程序 4N/4M/4G/4H 对应 40°C，3N/3M/3G/3H 对应 30°C。某买家的技术手册可能要求 4N/4M/4G/4H/3N/3M/3G/3H 全部按 4N（40°C）洗涤，但 6N/7N 仍按标准。我们希望把所有不参照标准的例外单独提取为买家的自有逻辑生成层，在合适的基础条件出现时对标准参数进行覆盖，其余仍走标准逻辑。这样既减少代码重复，也解决**散客**（完全按标准测试的客人）问题。

---

## 2. 标准参数层（✅ 已实现）—— 以 `src/` 实际代码为准

### 2.1 概念 → 实现对照表

旧文档提出的五个核心概念均已落地，命名与职责如下：

| 旧文档概念 | 新架构实现（聚合根/服务） | 位置 |
|---|---|---|
| 条件池 Condition Pool | `ConditionPool`，条件存 `Dictionary<string, object?>`（JSON 值、大小写不敏感），含测点集合与 Draft/Validated/Expired 状态 | `src/Domain/Aggregeates/ParamEngineContext/ConditionPoolContext/ConditionPool.cs` |
| 参数结构 Param Structure | `ParamStructure`，含 `ParamSchema`（主参数/条件要求/限值），关联标准族与规则 | `src/Domain/Aggregeates/ParamEngineContext/ParamStructureContext/ParamStructure.cs` |
| 公式 Formula | `Formula`，槽位表达式模板 `SlotName{field1,field2} + ... -> Result`，激活前校验表达式 | `src/Domain/Aggregeates/ParamEngineContext/FormulaContext/Formula.cs` |
| 参数规则 Param Rule | `ParamRule`，`ConditionPattern` 值对象（Equal/Comparison/In/Composite 四类匹配），`Match()` 运行时求值 | `src/Domain/Aggregeates/ParamEngineContext/ParamRuleContext/ParamRule.cs` + `ValueObj/ConditionPattern.cs` |
| 标准族 Standard Family | `StandardFamily`，聚合若干内容大致相同的标准、公式、参数结构、共享规则 | `src/Domain/Aggregeates/ParamEngineContext/StandardFamilyContext/StandardFamily.cs` |
| 条件池完整性判断 | 参数引擎协调器判断（校验服务 `IConditionPoolValidateService`），条件池本身保持“弱类型字典” | `src/Domain/Services/Validate/ConditionPoolValidateService.cs` |
| 规则引擎调度 | `ParamGenerationEngine`：按 Priority 升序匹配，命中且 StopOnMatch 则停止 | `src/Domain/Services/ParamGenerationEngine.cs` |
| 条件取值 / 比较 | `IConditionPoolDomainService`（按字段路径取值）+ `IValueComparer`（比较/范围/truthy） | `src/Domain/Services/ConditionPoolContext/ConditionPoolDomainService.cs`、`src/Domain/Services/ValueComparer.cs` |
| 补偿机制（缺参/越界） | `IParamCompensationService` → `ParamCompensationService`：缺参补默认值、越界走结构补偿 | `src/Domain/Services/Compensation/ParamCompensationService.cs` |

### 2.2 DSL 解析管道（已实现形态，与旧文档的差异）

旧文档设计的解析管道为三层：

```
业务表达式（字符串）
  ↓ Tokenize → 词法单元流（Token Stream）
  ↓ Parse    → 抽象语法树（AST，强类型节点）
  ↓ Evaluate → 领域对象（参数值/条件对象）
```

旧文档还给出了 AST 类层级设计（`RuleNode` / `AndCondition` / `ConditionNode` / `ResultMapping`）。**实际落地做了两处调整**：

1. **AST 被替换为可持久化的 `ConditionPattern`（JSON 值对象）**。规则需要入库（`BasicParamRule`）供配置化维护，内存 AST 无法直接落库，因此改为 Token 流 → 序列化 JSON → 反序列化为强类型 `ConditionPattern`。
2. **`ParamRule.Match()` 保持声明性**：聚合根不直接访问 `ConditionPool` 内部结构，取值/比较职责委托给注入的 `IConditionPoolDomainService`（accessor）与 `IValueComparer`（comparer），保证聚合根与技术细节解耦。

实际管道：

```
业务文本 / JSON
  ↓ RuleTokenizer.Tokenize()        词法：字符串/数值/单位/运算符/标识符 → Token 流
  ↓ Parser.Parse()                  语法：Token → ConditionPatternJson + ResultValue
  ↓ 反序列化                         → ConditionPattern（强类型值对象）
  ↓ ParamRule.Match(pool, accessor, comparer)   运行时求值
```

实现位置：
- 词法：`src/Infrastructure/Service/RuleTokenizer.cs`（正则原子拆分，6 类 Token）
- 语法：`src/Domain/Services/Parser.cs`（`IParser`）
- 模式：`src/Domain/Aggregeates/ParamEngineContext/ParamRuleContext/ValueObj/ConditionPattern.cs`
- JSON 序列化：`src/Infrastructure/Service/ConditionPatternSerializer.cs`

**关于 `+` 号**：旧文档将其定义为逻辑与（AND）。落地后体现在 `ConditionPattern` 内——Equal/Comparison/In 各集合内部为逻辑与；更复杂的组合用 `CompositeMatches`（`CompositeCondition`）表达，支持 AND/OR/NOT 递归嵌套。

### 2.3 参数结构聚合根职责（与旧文档的差异）

旧文档设想 `ParamStructure.Generate(pool, ruleRepo)` 由聚合根内部协调生成（协调者模式）。实际落地把**跨聚合编排上移到应用层协调器** `ParamGenerationCoordinator`，聚合根保持声明性（只持有 Schema / 标准族 ID / 规则 ID / 公式 ID 与状态流转），更符合聚合根边界最小化原则：

- 聚合根：`ParamStructure`（`src/Domain/Aggregeates/ParamEngineContext/ParamStructureContext/ParamStructure.cs`）
- 协调器：`src/Application/Service/ParamGenerateService/ParamGenerationCoordinator.cs`

### 2.4 标准参数生成工作流（已实现的真实调用链）

```
前端
 └─ PUT /api/CheckList/{id}/generate-param
     └─ CheckListAppService.CalculateParamAsync()
         └─ ParamGenerationUseCaseService.GenerateForCheckListItemAsync(item, pool)
             ├─ 按 item.StandardIds → StandardFamily → 该族的 ParamStructure 集合
             └─ 每个 structure → ParamGenerationCoordinator.GenerateAsync(structure, pool)
                 ├─ 1. 加载 Formula（若结构关联公式）
                 ├─ 2. IConditionPoolValidateService 校验条件池（按 Schema）
                 ├─ 3. IParamRuleRepository.GetByIdsAsync 加载适用规则
                 ├─ 4. ParamGenerationEngine.Generate(pool, rules) 匹配生成 ParamSet
                 └─ 5. IParamValidateService 校验 + IParamCompensationService 补偿（缺参/默认值）
             └─ FinalGenerateAsync(testItemId, paramSet)
                 └─ 按 TestItem 的 ParamRequireDefinitions 再做补偿 + 校验
```

### 2.5 已实现的代码地图（按层）

**Domain 聚合根**（`src/Domain/Aggregeates/ParamEngineContext/`）
- `ConditionPoolContext/ConditionPool.cs`
- `ParamStructureContext/ParamStructure.cs`（`ValueObj/ParamSchema.cs`、`ParamDefinition.cs`、`ParamLimitation.cs`、`ConditionRequirement.cs`）
- `ParamRuleContext/ParamRule.cs`（`ValueObj/ConditionPattern.cs`、`CompositeCondition.cs`、`ComparisonCondition.cs`、`ParamValue.cs`、`Enums/ComparisonOperator.cs`、`Enums/LogicalOperator.cs`）
- `FormulaContext/Formula.cs`（`ValueObj/FormulaId.cs`、`Enums/SlotType.cs`）
- `StandardFamilyContext/StandardFamily.cs`

**Domain 服务**（`src/Domain/Services/`）
- `ParamGenerationEngine.cs`、`Parser.cs`、`ValueComparer.cs`
- `ConditionPoolContext/ConditionPoolDomainService.cs`、`Validate/ConditionPoolValidateService.cs`
- `Compensation/ParamCompensationService.cs`

**引擎契约**（`src/Domain/Contract/Service/Engine/`）
- `IParamGenerationEngine.cs`、`IParamCompensationService.cs`、`IParamValidateService.cs`、`IParamStructureValidateService.cs`、`IParamRuleValidateService.cs`、`IParamLimitation.cs`、`ITokenizer.cs`、`IParser.cs`、`IConditionPatternBuilder.cs`
- `Condition/`：`IConditionAccessor.cs`、`IConditionEnricher.cs`、`IConditionPoolDomainService.cs`、`IConditionPoolValidateService.cs`、`IConditionPoolComparisonService.cs`、`IGenerateRequiredConditionsService.cs`、`IGroupPoolsAsync.cs`
- `Conparison/IValueComparer.cs`

**Infrastructure**（`src/Infrastructure/`）
- `Service/RuleTokenizer.cs`、`Service/ConditionPatternSerializer.cs`
- `Data/Repository/`：`ParamStructureRepository.cs`、`ParamRuleRepository.cs`、`FormulaRepository.cs`、`StandardFamilyRepository.cs`、`ConditionPoolRepository.cs`、`TestItemRepository.cs`
- `Data/Persistence/`：`BasicParamStructure.cs`、`BasicParamRule.cs`、`BasicFormula.cs`、`BasicStandardFamily.cs`、`FormulaStandardfamily.cs`、`ParamsturctureStandardfamily.cs`、`ConditionPool.cs`、`BasicItem.cs`

**Application 服务**（`src/Application/`）
- `Service/ParamGenerateService/`：`ParamGenerationUseCaseService.cs`、`ParamGenerationCoordinator.cs`
- `Service/ParamRuleAppService/`：`ParamRuleApplicationService.cs`、`ParamRuleQueryService.cs`、`RuleTranslationService.cs`
- `Service/ParamStructureContext/`、`Service/FormulaContext/`、`Service/ConditionPoolContext/`
- `Service/CheckListContext/CheckListAppService.cs`
- `UseCase/LogicTestUseCaseService.cs`（前端试算：更新条件池 / 按公式试算参数）

**Web API**（`src/Web API/`）
- `ParamRulesController.cs`（add-json / add-naturaltext / update-json / update-naturaltext / active / deactive / get / getall / getfrom-formulaId）
- `ParamStructureController.cs`、`ParamFormulaController.cs`、`StandardController.cs`、`StandardFamilyController.cs`、`ConditionPoolController.cs`
- `CheckListController.cs`、`UseCase/LogicValidationController.cs`

> **约定**（见 `doc/guides/ddd-conventions.md`）：强类型 ID（`ParamStructureId` 等）；聚合根工厂 `Create` / 重建 `Reconstitute`；应用服务 `Add/Update/Activate`；仓储 `Save/Get`。

---

## 3. 买家参数层（🚧 待实现）—— 本文档重点

### 3.1 现状

**新架构已预留的钩子：**

| 预留项 | 现状 | 位置 |
|---|---|---|
| `Buyer` 聚合根 | 空壳，仅 `BuyerId` | `src/Domain/Aggregeates/BuyerContext/Buyer.cs` |
| 买家持久化实体 | `BasicBuyer`（BuyerCode/BuyerName/Remark/SampleStorageDate/Country/IsIndividualTraveler） | `src/Infrastructure/Data/Persistence/BasicBuyer.cs` |
| 买家仓储 | `IBuyerReposity` + `BuyerRepository`（按 BuyerCode 查询） | `src/Domain/Contract/Repository/IBuyerReposity.cs`、`src/Infrastructure/Data/Repository/BuyerRepository.cs` |
| 买家覆盖字段 | `CheckListItem.BuyerModifiedTestItemId` / `BuyerModifiedTextMethodId` | `src/Domain/Aggregeates/CheckListContext/CheckListItem.cs` |
| 覆盖阶段占位 | `ParamGenerationCoordinator.GenerateAsync` 末尾 `//买家自定义层覆盖` | `src/Application/Service/ParamGenerateService/ParamGenerationCoordinator.cs` |
| 买家作为条件字段 | `ConditionPattern` 示例已将 `Buyer` 作为可匹配字段 | `.../ParamRuleContext/ValueObj/ConditionPattern.cs` |

**旧层问题（"一个按钮背后绑定过多逻辑"的根源）：**
- `Application/Services/BuyerService/*`：13 个 `XxxBuyer → XxxService → XxxRepository` 手写链，`IBuyer.ShowItem/ShowParameter` 全部返回 `object?`，类型信息丢失。
- `Application/Services/Factory/BuyerFactory.cs` + `PrintExcelStrategyFactory.cs`：两个大 switch，按买家名（小写字符串）分发。
- `Application/Services/ExcelService/PrintExcelMethod/*`：`PrintXxxExcel` 每个 430~1853 行，合计约 1.2 万行，按买家复制粘贴式膨胀。
- 旧入口：`POST /api/buyer/confirm`、`POST /api/buyer/parameter`（`Interfaces/Controllers/BuyerController.cs`）。

### 3.2 定位：买家层不是"13 个新类"

沿用两层设计：**标准参数是底层不变式，买家层只做"覆盖 / 替换 / 追加"**。但买家层的实现方式与标准层一致——**数据驱动、配置优先、代码兜底**：

1. **能配置的**（参数覆盖）→ 复用现有 `ParamRule + Formula + ParamStructure` 机制，把 `Buyer` 作为 `ConditionPattern` 的一个条件字段（等值匹配），规则按优先级高于标准规则。
2. **不能配置的**（整段换逻辑）→ 极少数买家才写少量策略代码，且通过 DI 注册（`IScopedDependency` 自动发现）分发，**禁止**再出现 `BuyerFactory` 式的大 switch。
3. **散客**（`IsIndividualTraveler = 1`）→ 完全按标准走，直接跳过买家覆盖阶段。

### 3.3 买家覆盖的三种形态

| 形态 | 语义 | 落点 |
|---|---|---|
| 参数覆盖（Param Override） | 同测试项同标准，买家改动参数值（如洗水程序、温度） | 买家 `ParamRule`：`ConditionPattern` 加 `Buyer` 等值匹配，Priority 高于标准规则；在覆盖阶段经同一 `ParamGenerationEngine` 运行 |
| 测试方法替换（Method Replace） | 买家用自己的测试方法，`ref` 某标准 | 生成 `BuyerModifiedTextMethodId`，CheckList 生成时替换 `StandardIds` 中的方法引用 |
| 测试项目替换/追加（Item Replace） | 买家自定义测试项 | 生成 `BuyerModifiedTestItemId`，替换/追加 `TestItemId` |

> 三种形态可叠加：先按买家规则做参数覆盖，再做方法/项目替换，最后仍走统一的校验 + 补偿。

### 3.4 设计：买家聚合根（充实现有空壳）

`Buyer` 聚合根只承载**主数据与身份**，不承载计算逻辑（参照 `Menu`、`PhysicalWeightRecord` 的范式）：

```
Buyer : AggregateRoot<BuyerId, string>
  BuyerCode            // 强类型 ID 值，跨系统唯一（仓储按它查询）
  BuyerName
  Remark
  SampleStorageDate    // 留样天数（可进入 ConditionPool，供规则判断）
  Country
  IsIndividualTraveler // 散客标志：true 时跳过买家覆盖
  Create(...)          // 工厂：校验不变式
  Reconstitute(...)    // 重建：仓储专用，不校验
  // 不做：参数计算、打印、报表——那些归买家覆盖规则 / 打印引擎
```

实现要点：
- `BuyerId` 值对象已存在（`src/Domain/Aggregeates/BuyerContext/ValueObj/BuyerId.cs`），`Menu` 已通过 `BuyerId` 引用买家。
- 仓储返回类型从 `BasicBuyer`（持久化实体）改为 `Buyer`（聚合根），映射用 Mapster 配置（已有 `BasicBuyerMappingConfig`）。
- 买家主数据（如 `SampleStorageDate`、`Country`）在生成时**填充进 ConditionPool**（`Buyer.SampleStorageDate` → `pool["SampleStorageDate"]`），供买家规则的条件判断复用，不必新造机制。

### 3.5 设计：买家层生成工作流（挂在占位处）

```
标准层生成完成（标准 ParamSet 已生成）
 └─ 买家覆盖阶段（ParamGenerationCoordinator.GenerateAsync 末尾占位处，
                 或独立 BuyerOverrideCoordinator，二选一，建议独立以保持协调器单一职责）
     ├─ 1. 判定散客：buyer.IsIndividualTraveler == true → 直接结束
     ├─ 2. 加载买家覆盖规则：
     │      IParamRuleRepository.GetByBuyerIdAsync(buyerId, standardFamilyId)
     │      → 复用同一 ParamRule / ConditionPattern 结构，Pattern 含 Buyer 等值匹配
     ├─ 3. 复用 ParamGenerationEngine.Generate(pool, buyerRules) → 覆盖 ParamSet
     ├─ 4. 方法/项目替换：
     │      BuyerModifiedTextMethodId / BuyerModifiedTestItemId 落库到 CheckListItem
     └─ 5. 统一校验 + 补偿（复用 IParamValidateService / IParamCompensationService）
```

> 说明：覆盖规则的仓储查询（`GetByBuyerIdAsync`）当前 `IParamRuleRepository` 未提供，需新增；或在 `ParamStructure` 之上再挂一层"买家规则组"（`BuyerRuleGroup`），按买家+标准族聚合规则。二者不互斥，建议先用仓储查询落地最小闭环。

### 3.6 落地路线图（建议顺序）

| 阶段 | 内容 | 产出 |
|---|---|---|
| 1 | 充实 `Buyer` 聚合根（主数据 + Create/Reconstitute），仓储改为返回聚合根，补齐映射 | 买家主数据聚合化 |
| 2 | 新增买家覆盖规则的数据接口（`GetByBuyerIdAsync` / 买家规则组），条件池注入买家主数据字段 | 覆盖规则可配置 |
| 3 | 实现覆盖阶段，挂在 `ParamGenerationCoordinator` 占位处，先做**参数覆盖**形态（跑通 Tchibo 洗水温度类覆盖） | 最小闭环 |
| 4 | 实现**方法/项目替换**形态，`BuyerModified*Id` 生成与落库，接入 CheckList 生成 | 覆盖全形态 |
| 5 | 拆分旧 `PrintXxxExcel`（共享 OpenXml 模板引擎 + 每买家列映射，参照 `PhysicalWeightDocxEngine` 范式） | 打印去重 |
| 6 | 废弃旧 `BuyerService/`、`BuyerFactory`、`PrintExcelStrategyFactory`，删除 13 个 `XxxBuyer` | 旧层移除 |

### 3.7 约束与红线

- **禁止**新建类似 `BuyerFactory` 的按买家 switch/if 分发——用 DI 注册或配置驱动。
- **禁止**在 `Buyer` 聚合根里写参数计算 / 打印逻辑。
- **禁止**新增返回 `object?` 的买家接口——统一 `Result<T>` 收敛。
- 买家规则必须可配置、可版本化（沿用 `Status` + `EffectiveDate` + `Priority` 机制），不得写死进代码。
