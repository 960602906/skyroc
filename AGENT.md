# SkyRoc 统一编码代理规范

本文件供不自动识别 `AGENTS.md` 的编码代理使用。**唯一权威规范是仓库根目录的 [`AGENTS.md`](./AGENTS.md)**；开始任务前必须完整阅读该文件，并以它为准。

`CLAUDE.md`、本文件和 `client/CLAUDE.md` 不维护平行的规则副本：它们只负责不同工具或子项目的导航，避免规范发生漂移。

## 开始工作前

1. 阅读 `AGENTS.md`、本文件及任务目录中更近层的说明文件。
2. 后端任务同时阅读 `CLAUDE.md`；前端任务同时阅读 `client/CLAUDE.md` 和适用的 `.cursor/skills/*/SKILL.md`。
3. 阅读 `docs/开发进度.md`、`docs/自动开发任务清单.md` 与对应的 `docs/business-flows/`；自动开发只处理第一个未勾选任务。
4. 先检查 `git status`，保留用户已有改动。

## 共享硬性约定摘要

- 保持 Clean Architecture 依赖方向：`SkyRoc → Application → Domain`，基础设施实现 Domain 接口；代码放在最窄的层。
- .NET 业务 API 进入受理后 HTTP 一律返回 `200`，成功或失败由响应体 `code` 表示；控制器使用 `ActionResult<ApiResponse<T>>`，不得用 `IActionResult` 或 HTTP 4xx/5xx 表示业务失败。
- 数量、价格、成本和金额计算必须使用 `Shared.Constants.NumericPrecision`；涉及业务决定的逻辑不得隐藏在 AutoMapper 配置中。
- 新增或修改公共后端类型、DTO、服务、仓储、控制器和操作时，补充中文 XML 文档；修改持久化实体时同步 EF Core `HasComment` 和可回滚迁移。
- 前端编辑/详情路由依赖服务端数据时使用 React Router `loader` + `useLoaderData`；业务枚举的 Badge/Select 按 `.cursor/skills/enum-badge/SKILL.md` 统一封装。
- 不提交生产连接串、JWT 密钥、种子密码或本地 Connector/MCP 凭据。

## 常用验证命令

后端在仓库根目录执行：

```powershell
dotnet restore SkyRoc.sln
dotnet build SkyRoc.sln --no-restore
dotnet test SkyRoc.Tests\SkyRoc.Tests.csproj --no-build
dotnet format SkyRoc.sln --verify-no-changes
```

前端在 `client/` 执行：

```powershell
pnpm typecheck
pnpm build
```

交付前按任务范围运行相关测试，更新 `docs/开发进度.md` 的断点和测试基线，并保留 `git diff --check` 为零错误。
