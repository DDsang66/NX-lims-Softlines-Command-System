# DDD 编码规范（项目约定）

本文档总结并规范本仓库（.NET 8、DDD 分层）中常用的领域驱动设计约定，旨在保证一致性、可测试性与可维护性。内容基于 `src` 目录代码实现（示例：`src\Domain\Share`、`src\Application`、`src\Infrastructure` 等）整理。

## 总体分层与目录约定
- 按责任分层：`Domain`（领域模型）、`Application`（应用服务 / DTO）、`Infrastructure`（持久化、外部服务）、`Doc`（设计/指南）。
- 代码路径示例：
  - 领域契约 / 接口：`src/Domain/Contract/...` 或 `src/Domain/Contract/Repository`
  - 聚合与实体：`src/Domain/Aggregeates/...`
  - 基础共享：`src/Domain/Share/...`
  - 仓储实现：`src/Infrastructure/Data/Repository/...`
  - 应用服务：`src/Application/Service/...`

## 命名与类型约定
- 强类型 ID：所有实体/聚合根的标识应使用强类型 Id（如 `MenuId`、`StandardId`），避免直接在领域中使用 `Guid`/`string`。
  - 强类型 id 常继承自 `AggregateRootId<T>` 或项目提供的 `StrongId<T>` 基类，并实现隐式转换（便于映射）。
- 聚合根 id 类型命名：`<Aggregate>NameId`（例如 `MenuId`）。
- 实体命名：领域实体使用描述性名词（`Menu`、`MenuItem`），VO（值对象）使用 `...Value` / `...VO` 视情况。

## 实体（Entity）与聚合根（AggregateRoot）
- `Entity` 基类责任：
  - 提供统一 `Id` 字段（`Guid` 或强类型 Id 的包装），实现 `Equals`、`GetHashCode`。
  - 支持领域事件集合（Add/Clear DomainEvents）。
  - 可选：`RowVersion` 用于乐观并发。
  - 提供 `ReconstructId(Guid id)`（或相应强类型重建方法）以支持从 PO 恢复实体 Id。
- `AggregateRoot<TId,TValue>`：
  - 聚合根应封装业务规则与不变量（invariants）。
  - 提供受控的构造（静态工厂 `Create`/`Reconstitute`）用于新建与重建（从持久化恢复）。
  - 聚合外部只能通过聚合根的公开方法改变内部实体状态（避免外部直接改集合）。

推荐约定：
- 工厂方法命名：`Create(...)`（用于新建）；重建从持久化时使用 `Reconstitute(...)`。
- 聚合内变更方法以业务动词命名：`AddMenuItem(...)`、`UpdateRequirement(...)`、`RemoveMenuItem(...)` 等。

## 值对象（Value Object）
- 不变性（immutable）优先。若必须可变，需在文档中明确并限定边界。
- 值对象实现 `Equals`/`GetHashCode` 基于其字段。

## 领域事件
- 命名：使用过去式并以 `Event` 结尾（例如 `MenuCreatedEvent`）。
- 事件类应包含：事件唯一标识、发生时间、触发事件的聚合根 ID（强类型）以及必要的关键数据。
- 事件收集/派发：聚合或实体通过 `AddDomainEvent(...)` 添加事件；仓储或统一的领域事件调度器负责在事务提交后派发并 `ClearDomainEvents()`。

## 领域服务（Domain Service）
- 当业务逻辑无法自然归属到某个聚合根时，使用领域服务。
- 接口与实现放置：`src/Domain/Services`（接口） & `src/Domain/Services/Impl`（实现），并仅包含领域逻辑（不依赖基础设施）。

## 应用层（Application）
- 只负责协调用例：组装 DTO、调用领域操作、事务边界管理、错误到 `Result` 的转换。
- 命名与约定：
  - AppService 公有方法应以业务含义命名（`CreateMenuAsync`、`AddMenuItemAsync`）。
  - 异步方法必须以 `Async` 结尾并且接受 `CancellationToken`（例如 `Task<Result> CreateMenuAsync(AddMenuDto dto, CancellationToken ct)`）。
  - 应用层应将所有输入做基础校验（空、基本范围）。复杂业务约束由领域负责。

## 返回与错误处理
- 统一使用 `Result` / `Result<T>` 码型表示成功或失败，避免抛出用于普通业务流控制。
- 对异常进行捕捉并在上层转换为 `Result.Fail`，保留内层异常信息（便于定位），但避免泄露敏感信息到终端用户。

## 仓储（Repository）
- 接口定义位置：`src/Domain/Contract/Repository`（例如 `IMenuRepository`）。
- 实现位置：`src/Infrastructure/Data/Repository`（例如 `MenuRepository`）。
- 接口职责：
  - `AddAsync`、`UpdateAsync`、`DeleteAsync`、`GetByIdAsync`、`GetAllAsync`、按需的查询方法（`GetMenusByBuyerAsync`）。
- 命名约定：
  - `FindByXxx`：可能返回 null 或空集合，不保证完整加载（用于轻量查询）。
  - `GetByXxx`：返回完整聚合（若不存在可返回 null 或抛出，需文档约定一致）。
- 持久化实践（EF Core）：
  - 读操作使用 `AsNoTracking()`（只读场景）。
  - 更新策略：先查询现有 PO，再使用 `_dbContext.Entry(existing).CurrentValues.SetValues(adaptedPo)` 更新属性，注意保持主键/外键不变。
  - 对聚合内集合（如 Menu -> MenuItems）采用差集（计算新增/删除/更新）策略同步数据库（`SyncMenuItemsAsync` 的实现是推荐做法）。
  - 批量插入/删除考虑分批与事务控制。
  - 避免在 DB 中用逗号拼接集合字段（例如 `StandardIds`）：建议用关联表或 JSON 列存储。

## 持久化模型（PO / POCO）与映射
- 持久化对象名前缀建议使用 `Basic*` 或明确 PO 后缀（例如 `BasicBuyerMenu`、`BasicMenuItem`），以便与领域对象区分。
- 映射工具（Mapster）：
  - 将映射配置集中管理（Mapster 配置类/启动时注册）。
  - 尤其注意 id/外键/时间戳的映射与保留（不要直接把完整领域对象传给 EF，也不要直接把 EF 实体当作领域对象返回）。
  - 对复杂字段（如 `StandardIds`）写入/读取处统一负责序列化与反序列化逻辑（优先考虑关系表）。

## 并发与事务
- 聚合级别使用乐观并发（`RowVersion` 字段）优先，避免悲观锁。
- 所有跨仓储或影响多个表的写操作应在同一事务（UnitOfWork）内完成。
- `IUnitOfWork` 的 `SaveChangesAsync` 应由应用层在业务用例结束时调用并由上层捕获异常。

## 异步与 Cancel
- 所有 I/O 操作使用异步 API（`async/await`）。
- 方法签名必须包含 `CancellationToken` 参数并在内部传递给 EF/外部调用。

## 规则引擎 / 参数引擎（项目特有）
- 按职责划分管线：Tokenizer → Parser → Compiler → Executor → Compensation。
- 接口契约示例：
  - `IParamRule`：`bool Matches(ConditionPool pool)`、`ParamValue Evaluate(ConditionPool pool)`。
  - 引擎输出：`ParamSet`（按 Schema 组织的参数集合）。
- 可运维性：
  - 规则、Schema、ConditionPool 等应支持版本化（JSON 存储）与热加载/回滚。
  - 将编译产物可序列化以便缓存（避免每次请求重编译）。
- 测试：为 Tokenizer/Parser/Compiler/Executor 分别编写单元测试，并提供示例规则与期望输出作为回归测试集。

## 测试建议
- 单元测试（高覆盖）：
  - 领域模型（不变量、边界条件）。
  - 规则引擎各个阶段（Tokenizer/Parser/Compiler/Executor）。
- 集成测试：
  - 使用 SQLite InMemory 或容器（Testcontainers）对仓储进行近真实 DB 测试（事务、SetValues 行为、导航加载等）。
- E2E：
  - 验证规则变更对最终 `ParamSet`/业务流程的影响。

## 可观察性（日志、监控、审计）
- 在关键路径（仓储、引擎、应用服务）添加结构化日志（traceId、输入摘要、耗时）。
- 写操作保留审计信息（谁/何时/何种变更），尤其是对规则/Schema 的变更。
- 集成指标（处理耗时、错误率）并配置告警。

## 代码风格与质量
- 保持单一职责，避免跨层职责侵入（领域不依赖基础设施）。
- 异步方法以 `Async` 后缀并接受 `CancellationToken`。
- 使用 `Result`/`Result<T>` 返回业务结果而不是异常控制流。
- 使用 Mapster 等工具时统一配置，避免散落的 `Adapt<...>()` 导致映射不一致。
- 添加静态分析、格式化规则到 CI（`dotnet format`、Roslyn 分析器 等）。

## 版本与迁移注意
- 若要修改持久化模型（例如把逗号拼接的 `StandardIds` 拆成关联表）：
  - 先在代码中支持旧格式的读入转换（兼容层），
  - 提供迁移脚本以批量迁移历史数据，
  - 回归测试覆盖迁移结果。
- 规则/Schema 的版本化应兼容历史规则的回放与再计算。

## 参考（项目内示例）
- 实体基类与聚合根实现示例：`src/Domain/Share/Entity.cs`、`src/Domain/Share/AggregateRoot.cs`、`src/Domain/Share/AggregateRootId.cs`
- 统一结果类型：`src/Domain/Share/Result.cs`
- 聚合与仓储示例：`src/Application/Service/MenuContext/MenuAppService.cs`、`src/Infrastructure/Data/Repository/MenuRepository.cs`
- 领域事件：`src/Domain/Events/DomainEvents.cs`

---