# architecture/bounded-contexts.md（限界上下文）

# 限界上下文划分

## 核心域

## StandardManagement（标准管理）
-**职责**：管理检测标准、参数结构、计算规则

-**聚合根**：Standard、ParameterSchema、ParameterRule

-**领域事件**：StandardCreated、StandardActivated、RuleAdded

## BuyerManagement（买家管理）
-**职责**：客户信息、检测套餐、委托单

-**聚合根**：Buyer、BuyerMenu、CheckList（TestOrder）

-**领域事件**：BuyerRegistered、MenuConfigured、OrderSubmitted

### TestingExecution（检测执行）
-**职责**：检测任务分配、结果录入、报告生成

-**聚合根**：TestTask、TestResult、Report 

-**领域事件**：TaskAssigned、ResultEntered、ReportGenerated