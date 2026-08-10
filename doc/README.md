# NX-lims Softlines Command System

## 项目简介

NX-lims Softlines Command System 是一个用于管理和执行软性面料测试的系统。旨在提升测试效率与准确性。

## 技术栈

- 后端：.NET 8
- 数据库：SQL Server / SQLite
- 规程引擎：[Drools](https://www.drools.org/)

## 快速开始

1. 克隆仓库

   ```bash
   git clone https://github.com/DDsang66/NX-lims-Softlines-Command-System.git
   cd "NX-lims Softlines Command System"
   ```

2. 环境要求

   - .NET 8 SDK
   - SQL Server（或使用开发时的本地容器/SQLite 替代）
   - 推荐使用 VS2022 或 JetBrains Rider

3. 配置数据库连接

   - 在 `src/Infrastructure` 或应用的 `appsettings.Development.json` 中配置 `ConnectionStrings:Default` 指向你的 SQL Server 实例。

4. 运行迁移与初始化

   ```bash
   cd src/Infrastructure
   dotnet ef database update --project ./ --startup-project ../..  # 根据实际项目结构调整
   ```

5. 启动服务

   ```bash
   cd src
   dotnet run --project YourHostProject.csproj
   ```

## 测试

- 单元测试：在 `tests/` 或 `src` 下的 test 项目中执行 `dotnet test`。
- 集成测试：建议使用 SQLite InMemory 或 Testcontainers 来近似真实 DB 场景。

## 开发约定

- 领域驱动设计（DDD）相关约定详见：`doc/guides/ddd-conventions.md`。
- 代码重建/规则引擎指导见：`doc/guides/Code Rebuild Guidance.doc`。

## 贡献与支持

- 提交前请确保通过 `dotnet format` 及单元测试。
- 新功能分支命名：`feature/<描述>`，bug 修复：`fix/<描述>`。
- 如需帮助请在仓库 Issues 创建问题并贴上最小可复现步骤及日志输出。

## License

本项目遵循 MIT 许可证。有关详细信息，请参阅 [LICENSE](LICENSE) 文件。