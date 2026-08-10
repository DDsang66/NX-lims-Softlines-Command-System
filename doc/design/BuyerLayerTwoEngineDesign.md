# 买家参数层设计方案 —— 两套同构引擎 + 协调器适配

> 状态：🚧 待评审 / 待实现
> 关联文档：[重构指引](../guides/Code%20Rebuild%20Guidance.md)（标准层实现现状）｜ [DDD 编码规范](../guides/ddd-conventions.md)
> 适用范围：`src/` 下的 DDD 新架构；不涉及旧层 `Application/Services/BuyerService/*` 的直接改动（其迁移见第 11 节）

---

## 1. 背景与目标

### 1.1 问题

1. **标准层已完整落地**（ParamStructure / Formula / ParamRule / ConditionPool / 引擎 / 校验 / 补偿 / DSL 管道），但**买家层尚未实现**。
2. 旧层（`Application/Services/BuyerService/*`）存在 13 个 `XxxBuyer → XxxService → XxxRepository` 手写链 + 两个大 switch 工厂，逻辑全部用 `object?` 传递，无法配置化，无法维护。
3. 业务诉求：买家与标准是**两个不相关联的层级**，各自有完整的"参数结构 / 公式 / 规则"，运算时由**上层协调器适配**；最终参数 = 买家参数 ∪ 标准参数，**买家优先**，标准层补充买家未覆盖的参数。

### 1.2 目标

- **买家层复用标准层已有的整套机制**（聚合根 / 引擎 / 校验 / 补偿 / DSL），不另起炉灶。
- 买家层与标准层**同构**：`Buyer → BuyerParamStructure → BuyerFormula → BuyerParamRule`。
- 由**协调器**完成"买家 pass → 标准 pass → 并集（买家优先）"的编排。
- 对外 API（`PUT /api/CheckList/{id}/generate-param`）**保持不变**。

---

## 2. 现状盘点

### 2.1 已实现（可直接复用）

| 组件 | 位置 | 复用性 |
|---|---|---|
| `ParamRule` 聚合根 | `src/Domain/Aggregeates/ParamEngineContext/ParamRuleContext/ParamRule.cs` | ✅ `FormulaId`/`StructureId` 可空，层无关 |
| `ParamStructure` 聚合根 | `.../ParamStructureContext/ParamStructure.cs` | ✅ 归属扩展后可复用 |
| `Formula` 聚合根 | `.../FormulaContext/Formula.cs` | ✅ 归属扩展后可复用 |
| `ParamSet` 值对象（含 `Merge(Ignore)` 并集原语） | `.../CheckListContext/ValueObj/ParamSet.cs` | ✅ 零改动 |
| `ConditionPool` 聚合根 | `.../ConditionPoolContext/ConditionPool.cs` | ✅ 建池注入买家字段后复用 |
| `ParamGenerationEngine` 引擎 | `src/Domain/Services/ParamGenerationEngine.cs` | ✅ 层无关调度器，零改动 |
| 校验 `ParamValidateService` | `src/Domain/Services/Validate/ParamValidateService.cs` | ✅ 层无关 |
| 补偿 `ParamCompensationService` | `src/Domain/Services/Compensation/ParamCompensationService.cs` | ✅ 层无关 |
| DSL 管道（Tokenize/Parse/序列化） | `src/Infrastructure/Service/RuleTokenizer.cs`、`src/Domain/Services/Parser.cs`、`src/Infrastructure/Service/ConditionPatternSerializer.cs` | ✅ 层无关 |
| accessor/comparer | `IConditionPoolDomainService` / `IValueComparer` | ✅ 零改动 |
| 协调器（标准层现有） | `src/Application/Service/ParamGenerateService/ParamGenerationCoordinator.cs` | ✅ 买家 pass 是新增协调器，标准逻辑保留 |

### 2.2 已预留的买家钩子

| 预留项 | 现状 | 位置 |
|---|---|---|
| `Buyer` 聚合根 | 空壳，仅 `BuyerId` 值对象 | `src/Domain/Aggregeates/BuyerContext/Buyer.cs` |
| 买家持久化实体 | `BasicBuyer`（BuyerCode/BuyerName/Remark/SampleStorageDate/Country/IsIndividualTraveler） | `src/Infrastructure/Data/Persistence/BasicBuyer.cs` |
| 买家仓储 | `IBuyerReposity` + `BuyerRepository`（按 BuyerCode 查） | `src/Domain/Contract/Repository/IBuyerReposity.cs` |
| 买家覆盖字段 | `CheckListItem.BuyerModifiedTestItemId` / `BuyerModifiedTextMethodId` | `src/Domain/Aggregeates/CheckListContext/CheckListItem.cs:28,38` |
| 覆盖阶段占位 | `ParamGenerationCoordinator.GenerateAsync` 末尾 `//买家自定义层覆盖` | `src/Application/Service/ParamGenerateService/ParamGenerationCoordinator.cs:132` |
| 买家作为条件字段 | `ConditionPattern` 注释示例已将 `Buyer` 列为可匹配字段 | `.../ParamRuleContext/ValueObj/ConditionPattern.cs` |

### 2.3 缺口

1. **归属维度**：ParamStructure / Formula 只能挂 `StandardFamilyId`，不能挂买家。
2. **仓储查询维度**：规则/结构/公式只能按公式、标准族查，不能按买家查。
3. **条件池**：初始条件由标准 schema 推导，不含 `Buyer`/`SampleStorageDate`/`Country` 等买家主数据字段。
4. **买家链路**：CheckList 目前只挂 `OrderId`，未打通 Order→Buyer（或 CheckList→Buyer）。
5. **`ParamRule.Active()` 不变式**：要求 `FormulaId`+`StructureId` 都非空，买家规则需放宽。

---

## 3. 核心设计：两套同构引擎 + 协调器适配

```
                     ┌──────────────────────────────────────────────────┐
                     │       BuyerOverrideCoordinator（协调器，应用层）     │
                     └──────────────────────────────────────────────────┘
          ┌──────────────────────────┐        ┌──────────────────────────┐
          │   买家层（同构复用）🚧     │        │   标准层（已实现）✅        │
          │  BuyerParamStructure     │        │  ParamStructure          │
          │  BuyerFormula            │        │  Formula                 │
          │  BuyerParamRule          │        │  ParamRule               │
          │  ConditionPool(买家字段)   │  同套   │  ConditionPool(标准字段)   │
          │  引擎/校验/补偿/DSL        │───────▶│  引擎/校验/补偿/DSL         │
          └──────────────────────────┘        └──────────────────────────┘
                           └── 并集：buyerSet.Merge(standardSet, Ignore) 买家优先 ──┘
```

**核心结论**：买家层不是"标准结构 + 买家特例"，而是与标准层**同构**的完整层级，复用同一套 ParamRule / ParamStructure / Formula / ParamSet / ConditionPool / 引擎 / 校验 / 补偿 / DSL，区别只在**归属**与**协调顺序**。

### 3.1 处理流水线（五步）

```
① 方法/项目替换（先于参数生成）
   └ 买家替换规则命中 → 换 StandardIds / TestItemId
     → 落库 BuyerModifiedTextMethodId / BuyerModifiedTestItemId
② 买家 pass：生成买家全量参数集
   └ Buyer → BuyerParamStructure → BuyerParamRule → 引擎.Generate(pool, buyerRules)
③ 标准 pass：生成标准全量参数集（现有 GenerateForCheckListItemAsync 逻辑，原样保留）
④ 并集：buyerSet.Merge(standardSet, MergeConflictStrategy.Ignore)   // 买家优先，标准补缺
⑤ 兜底：FinalGenerateAsync 按 TestItem 的 ParamRequireDefinitions 补默认
   └ 现有，零改动（只在 existingValue == null 时补）
```

---

## 4. 复用清单与零改动声明

| 组件 | 买家层使用方式 | 需要改动 |
|---|---|---|
| `ParamRule` | `FormulaId`/`StructureId` 传 **null** | 仅 `Active()` 归属检查放宽（§6） |
| `ParamStructure` / `Formula` | 原样类型，归属挂买家 | 加 `BuyerId?`（§5） |
| `ParamSet` | `Merge(Ignore)` = 并集原语 | **零改动** |
| `ConditionPool` | 同类型，建池注入买家字段 | 建池服务改（§8） |
| `ParamGenerationEngine` | `Generate(pool, rules)` 层无关 | **零改动** |
| `ParamValidateService` / `ParamCompensationService` | 复用 | **零改动** |
| DSL 管道（Tokenize/Parser/Serializer） | 复用 | **零改动** |
| `IConditionPoolDomainService` / `IValueComparer` | 复用 | **零改动** |
| `CheckList` / `CheckListItem` | 消费端，结果写入 `TestPointParams` | **零改动** |
| `IBuyerReposity` / `BuyerRepository` | 复用 | 充实聚合根（§10） |

---

## 5. 归属扩展（唯一动聚合根结构的点）

### 5.1 变更

给 `ParamStructure` 与 `Formula` 各加一个**可空 `BuyerId`**，**不变式：归属二选一**。

```csharp
// ParamStructure.cs
public BuyerId? BuyerId { get; private set; }   // null = 标准层；非空 = 买家层

public static ParamStructure Create(
    ParamStructureId id,
    IEnumerable<StandardFamilyId?> standardFamilyIds,   // 标准归属
    BuyerId? buyerId,                                    // 买家归属（新增）
    ...)
{
    // 不变式：StandardFamilyIds 非空 与 BuyerId 非空 互斥
    if ((standardFamilyIds?.Any(f => f != null) == true) == (buyerId != null))
        throw new InvalidOperationException("归属二选一：标准族或买家，不能同时/都不存在");
    ...
}
```

- 标准结构：`StandardFamilyIds` 非空，`BuyerId = null`（现状，无回归）
- 买家结构：`BuyerId` 非空，`StandardFamilyIds` 空

`Formula` 做同样的变更。`Reconstitute` 同步增加参数。

### 5.2 为什么加字段而不是建独立表

| 维度 | 加 `BuyerId?` 字段 | 独立 BuyerParamStructure 表 |
|---|---|---|
| 聚合根复用 | ✅ 同一聚合根 | ❌ 复制一套聚合根/值对象/映射 |
| 激活/校验复用 | ✅ 同一套流程 | ❌ 复制 |
| 仓储基类复用 | ✅ | ❌ |
| 隔离性 | 由不变式保证 | 更强 |
| 侵入度 | 最小 | 大 |

**结论**：加字段，隔离由不变式保证。

---

## 6. ParamRule 激活放宽

### 6.1 现状

`Active()` 要求 `FormulaId != null && StructureId != null`（[ParamRule.cs:380-408](src/Domain/Aggregeates/ParamEngineContext/ParamRuleContext/ParamRule.cs#L380-L408)）。这是标准层"公式→结构→规则"推导链的产物。

### 6.2 变更

把"归属校验"从 ParamRule 内部**上移到 `IParamRuleValidateService.Validate()`**（已在应用服务激活时调用，见 `ParamRuleApplicationService.ActiveParamRuleAsync`）：

- **标准规则**：保持原检查（必须挂 Formula + Structure）。
- **买家规则**：允许只挂买家 `ParamStructure`（`BuyerId` 非空）即可激活；不强制挂买家 Formula。

`ParamRule` 本身**不感知层级**——它只声明"规则归属标识"（可空的 FormulaId/StructureId），归属合法性由协调器 / 校验服务判定。这样 `Match()`、`Update()`、`Reconstitute` 全部零改动。

---

## 7. BuyerOverrideCoordinator（协调器）详细设计

### 7.1 位置

`src/Application/Service/BuyerContext/BuyerOverrideCoordinator.cs`（应用层，`IScopedDependency`）。

### 7.2 职责

跨聚合加载、执行两个 pass、并集合并、落库；不直接持久化（沿用 `IUnitOfWork` 事务边界）。

### 7.3 依赖注入

```csharp
public class BuyerOverrideCoordinator : IScopedDependency
{
    // 买家层
    private readonly IBuyerReposity _buyerRepo;
    private readonly IParamStructureRepository _structureRepo;   // 复用，含买家结构
    private readonly IFormulaRepository _formulaRepo;            // 复用
    private readonly IParamRuleRepository _ruleRepo;             // 复用，新增按买家查
    // 标准层（现有）
    private readonly ParamGenerationCoordinator _standardCoordinator;
    // 引擎与兜底
    private readonly IParamGenerationEngine _engine;
    private readonly IParamCompensationService _compensation;
    private readonly IParamValidateService _paramValidate;
    private readonly IUnitOfWork _unitOfWork;
}
```

### 7.4 流水线伪代码

```csharp
public async Task<Result<ParamSet>> GenerateForItemAsync(CheckListItem item, ConditionPool pool, CancellationToken ct)
{
    // ① 方法/项目替换（先于参数生成）
    //    buyerRule 命中方法替换 → item.StandardIds 替换；命中项目替换 → item.TestItemId 替换
    //    落库 BuyerModifiedTextMethodId / BuyerModifiedTestItemId

    // ② 买家 pass：买家全量参数集
    var buyerSet = new ParamSet();
    var buyerStructures = await _structureRepo.GetByBuyerIdAsync(buyerId, ct);
    foreach (var structure in buyerStructures)
    {
        var rules = await _ruleRepo.GetByStructureIdAsync(structure.Id, ct);
        var set = _engine.Generate(pool, rules);              // 复用同一引擎
        buyerSet.Merge(set);                                  // 买家内部合并（Overwrite，后定义优先）
    }

    // ③ 标准 pass：标准全量参数集（现有逻辑原样）
    var standardSet = await GenerateStandardSetAsync(item, pool, ct);

    // ④ 并集：买家优先，标准补缺
    buyerSet.Merge(standardSet, MergeConflictStrategy.Ignore);

    // ⑤ 兜底：按 TestItem 定义补默认（现有，零改动）
    return await _standardCoordinator.FinalGenerateAsync(item.TestItemId!, buyerSet, ct);
}
```

### 7.5 标准 pass 封装

`ParamGenerationUseCaseService.GenerateForCheckListItemAsync` 现有的 `family → structures → coordinator.GenerateAsync` 循环**原样保留**，抽成标准 pass 方法复用；其中"每次 structure 都 Validate + Compensate"的现有行为不变（因为并集用 Ignore，买家已覆盖的值不会被覆盖，见 §7.6 说明）。

### 7.6 为什么并集用 Ignore 就能避免误伤

- `ParamSet.Merge(Ignore)` 保留已存在的键（[ParamSet.cs:55-57](src/Domain/Aggregeates/CheckListContext/ValueObj/ParamSet.cs#L55-L57)），买家值天然优先。
- 标准 pass 内部的 `Validate`+`Compensate` 只作用于**标准自己生成**的 ParamSet，不触碰买家值。
- 最终兜底 `CompensateWithItemDefinitions` 只在 `existingValue == null` 时补默认（[ParamCompensationService.cs:44-48](src/Domain/Services/Compensation/ParamCompensationService.cs#L44-L48)），不冲掉买家非空值。
- 需要注意的边界：`ParamValidateService.ValidateParamValue` 对越界会 throw（[ParamValidateService.cs:90-97](src/Domain/Services/Validate/ParamValidateService.cs#L90-L97)）。标准 pass 内该校验只针对标准结构的主参数，并集后买家值已覆盖、不会再走该校验，因此**不受影响**。

---

## 8. 条件池与买家字段

### 8.1 现状

- 初始条件 = `ParamRequireConditionGenerateService.GenerateRequiredConditionsAsync` 从标准 ParamStructure schema 推导（[ParamRequireConditionGenerateService.cs:78](src/Application/Service/ConditionPoolContext/ParamRequireConditionGenerateService.cs#L78)），**不含 Buyer**。
- `ConditionPool.Create` 的 initial 字典接受任意键、无白名单（[ConditionPool.cs:61-68](src/Domain/Aggregeates/ParamEngineContext/ConditionPoolContext/ConditionPool.cs#L61-L68)）。
- `Update()` 拒绝未知字段，且仅 Draft 态可更新（[ConditionPool.cs:155-184](src/Domain/Aggregeates/ParamEngineContext/ConditionPoolContext/ConditionPool.cs#L155-L184)）。

### 8.2 变更

- **建池时注入买家主数据字段**：`Buyer`、`SampleStorageDate`、`Country` 作为 initial 字典的一部分传入 `ConditionPool.Create`，供买家规则 `ConditionPattern` 匹配（等值 / 比较 / In / 复合）。
- **买家 pass 对池只读**：买家 pass 之后标准 pass 不得再 `Update()` 池（否则因池已是 Validated 而 throw）；任何想写池的动作必须放在建池阶段。

### 8.3 前置链路

CheckList 目前只挂 `OrderId`，需打通 **Order→Buyer**（在 `ParamRequireConditionGenerateService` 或建池处按 Order 关联买家）或直接给 CheckList 加 `BuyerId`。这是数据管线改动，不是聚合根结构改动。

---

## 9. 仓储与数据模型

### 9.1 仓储接口扩展

| 接口 | 新增方法 | 语义 |
|---|---|---|
| `IParamStructureRepository` | `GetByBuyerIdAsync(BuyerId)` | 买家结构列表 |
| `IFormulaRepository` | `GetByBuyerIdAsync(BuyerId)` | 买家公式列表 |
| `IParamRuleRepository` | `GetByStructureIdAsync(ParamStructureId)` | 按结构查规则（含买家结构） |

### 9.2 数据模型

| 表 | 改动 |
|---|---|
| `BasicParamStructure` | 加 `BuyerId` 列（可空，与 `StandardFamilyIds` 归属互斥） |
| `BasicFormula` | 加 `BuyerId` 列（可空，与 `StandardFamilyIds` 归属互斥） |
| `BasicParamRule` | **不变**（`FormulaId`/`StructureId` 可空已支持买家规则挂买家结构） |
| `BasicBuyer` / `ConditionPool` | 不变 |

---

## 10. 聚合根改动清单

| 聚合根 / 组件 | 改动 | 位置 |
|---|---|---|
| `ParamStructure` | 加 `BuyerId?` + 归属二选一不变式 | `.../ParamStructureContext/ParamStructure.cs` |
| `Formula` | 加 `BuyerId?` + 归属二选一不变式 | `.../FormulaContext/Formula.cs` |
| `ParamRule` | `Active()` 归属检查上移到校验服务 | `.../ParamRuleContext/ParamRule.cs` |
| `Buyer` | 充实主数据聚合根（Create / Reconstitute / 状态） | `src/Domain/Aggregeates/BuyerContext/Buyer.cs` |
| `ParamGenerationEngine` | **零改动** | — |
| `ParamValidateService` / `ParamCompensationService` | **零改动** | — |
| `ParamSet` / `ConditionPool` / `CheckList` / `CheckListItem` | **零改动** | — |

### 10.1 Buyer 聚合根充实（参照 `Menu` / `PhysicalWeightRecord` 范式）

```csharp
public sealed class Buyer : AggregateRoot<BuyerId, string>
{
    public string BuyerName { get; private set; }
    public string? Remark { get; private set; }
    public int? SampleStorageDate { get; private set; }
    public string? Country { get; private set; }
    public bool IsIndividualTraveler { get; private set; }   // 散客：true 跳过买家 pass

    public static Buyer Create(...) { /* 校验不变式 */ }
    public static Buyer Reconstitute(...) { /* 仓储重建，不校验 */ }
    // 不做：参数计算 / 打印 / 报表
}
```

- 仓储 `IBuyerReposity` 返回类型从 `BasicBuyer`（持久化实体）改为 `Buyer`（聚合根），映射用 Mapster（已有 `BasicBuyerMappingConfig`）。
- 散客（`IsIndividualTraveler == true`）在协调器 ① 直接跳过买家 pass，全走标准。

---

## 11. 落地路线图

| 阶段 | 内容 | 产出 | 验收 |
|---|---|---|---|
| 1 | 充实 `Buyer` 聚合根；仓储改返回聚合根；补齐映射 | 买家主数据聚合化 | 买家列表接口不回归 |
| 2 | `ParamStructure`/`Formula` 加 `BuyerId?` + 归属不变式；表加列 + SQL 脚本 | 归属维度就绪 | 标准层结构回归通过 |
| 3 | 仓储扩展三个 `GetByBuyerIdAsync`/`GetByStructureIdAsync` | 按买家查询就绪 | 单测 |
| 4 | 建池注入买家字段 + 打通 Order→Buyer | 买家条件可匹配 | 条件池含 Buyer 字段 |
| 5 | `ParamRule.Active()` 归属检查上移 | 买家规则可激活 | 买家规则激活单测 |
| 6 | 新建 `BuyerOverrideCoordinator` 五步流水线，接入 `GenerateForCheckListItemAsync` | 买家覆盖闭环 | 端到端：买家覆盖 + 标准补缺 |
| 7 | 旧 `BuyerService/`、`BuyerFactory`、`PrintExcelStrategyFactory` 迁移/删除（见重构指引 §3.6） | 旧层移除 | 路由无回归 |

---

## 12. 备选方案与取舍

### 12.1 方案 A（本文档）：两套同构引擎 + 协调器

- 优点：买家层完整表达力最强，可配置化；复用最大化；归属隔离清晰。
- 缺点：需动 ParamStructure/Formula 归属、ParamRule 激活检查；双 pass 双跑引擎。

### 12.2 方案 B：单套引擎，买家规则高 Priority 混跑

- 买家规则与标准规则混进同一个 `ParamGenerationEngine.Generate`，靠 `Priority` + `StopOnMatch` 覆盖。
- 优点：一次跑完，无并集；改动最小。
- 缺点：买家规则与标准规则同表同归属，隔离差；"买家优先"依赖排序而非显式合并，语义不够强；方法/项目替换仍需单独处理。

### 12.3 方案 C：反序执行 + Merge(Ignore)

- 买家先生成，标准层"补缺"。
- 本质与本方案④并集等价，但需把标准 pass 改为"补缺感知"（跳过已覆盖结构的校验/补偿），改动集中在协调器，且不如"两套全量各自生成"清晰。

### 12.4 取舍结论

选方案 A。它把"买家与标准是两个独立层级"表达得最直接，复用最大化，且为后续打印层（`PrintXxxExcel` 拆分）铺路——打印同样可按"买家/标准两级模板"复用。

---

## 13. 风险与红线

### 13.1 红线（禁止）

- **禁止**新建类似 `BuyerFactory` 的按买家 switch/if 分发——用 DI 注册或配置驱动。
- **禁止**在 `Buyer` 聚合根里写参数计算 / 打印逻辑。
- **禁止**新增返回 `object?` 的买家接口——统一 `Result<T>`。
- **禁止**买家 pass 在标准 pass 后修改 ConditionPool（状态 Validated 会 throw）。
- 买家规则必须可配置、可版本化（沿用 `Status` + `EffectiveDate` + `Priority` 机制）。

### 13.2 风险

| 风险 | 等级 | 缓解 |
|---|---|---|
| `ParamRule.Active()` 放宽引入归属校验漏洞 | 中 | 校验上移后补单测；买家结构激活必须 `BuyerId` 非空 |
| 归属二选一不变式破坏标准层回归 | 中 | Create/Reconstitute 双端校验 + 阶段 2 回归 |
| 双 pass 双跑引擎的性能 | 低 | 散客/无买家规则短路跳过买家 pass；并集后用 `Merge(Ignore)` 无冗余写入 |
| 买家参数全集少于标准，缺参依赖兜底 | 低 | `FinalGenerateAsync` 兜底，行为与现有一致 |

---

## 附录：相关代码地图

### 标准层（复用基准）
- 聚合根：`src/Domain/Aggregeates/ParamEngineContext/{ParamStructureContext,FormulaContext,ParamRuleContext,ConditionPoolContext,StandardFamilyContext}/*`
- 引擎/服务：`src/Domain/Services/ParamGenerationEngine.cs`、`Parser.cs`、`ValueComparer.cs`、`Validate/ParamValidateService.cs`、`Compensation/ParamCompensationService.cs`
- DSL：`src/Infrastructure/Service/RuleTokenizer.cs`、`ConditionPatternSerializer.cs`
- 协调/用例：`src/Application/Service/ParamGenerateService/{ParamGenerationCoordinator,ParamGenerationUseCaseService}.cs`
- 控制器：`src/Web API/ParamRulesController.cs`、`ParamStructureController.cs`、`CheckListController.cs`

### 买家层（待实现）
- `src/Domain/Aggregeates/BuyerContext/Buyer.cs`、`ValueObj/BuyerId.cs`
- `src/Infrastructure/Data/Persistence/BasicBuyer.cs`、`src/Infrastructure/Data/Repository/BuyerRepository.cs`
- `src/Application/Service/BuyerContext/{BuyerAppService,BuyerQueryAppService}.cs`、`src/Web API/Buyer/*`

### 旧层（待迁移）
- `Application/Services/BuyerService/*`、`Application/Services/Factory/{BuyerFactory,PrintExcelStrategyFactory}.cs`、`Application/Services/ExcelService/PrintExcelMethod/*`
