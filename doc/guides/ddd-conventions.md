# DDD 编码规范

## 实体 ID
- 所有实体 ID 使用强类型（`实体名-Id`如`StandardId` 而非 `Guid`）

- 定义在 `Domain.Shared/StrongIds/` 目录

- 继承 `StrongId<T>` 基类

## 聚合根
- 工厂方法命名：`Create`（内存实例化）

- 应用服务方法命名：`Add` / `Update`/`Activate`（业务用例）

- 仓储方法命名：`Save` / `Get`（持久化）

## 领域事件
- 命名：过去式 + `Event` 后缀如（`StandardCreatedEvent`）

- 位置：`Domain/Contract/Events/`

- 必须包含：发生时间、相关 ID、关键数据

## 仓储
- 接口定义在 `Domain/Repositories/`

- 实现在 `Infrastructure/Repositories/`

- 查询方法：`FindByXxx`（可能返回 null）

- 获取方法：`GetByXxx`（必须完整加载聚合）