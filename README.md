# SkyRoc

`SkyRoc` 是一个基于 `.NET 9` 的后台管理系统后端项目，围绕用户、角色、菜单、按钮、部门这些核心对象，提供认证、授权、动态路由和基础管理能力。

当前项目采用分层架构，已经具备本地启动、默认种子初始化、Redis Token 缓存、Redis 运行时内存降级、Swagger 调试、HTTP 联调脚本和最小回归测试。

## 技术栈

- `.NET 9`
- `ASP.NET Core Web API`
- `Entity Framework Core`
- `PostgreSQL`
- `Redis`
- `JWT Bearer Authentication`
- `FluentValidation`
- `AutoMapper`
- `xUnit`

## 项目结构

- [SkyRoc](/abs/path/C:/Users/mrqin/RiderProjects/skyroc/SkyRoc): Web API 启动项目，包含控制器、中间件、启动配置和 HTTP 联调脚本
- [Application](/abs/path/C:/Users/mrqin/RiderProjects/skyroc/Application): 应用层，包含 DTO、业务服务、校验器、映射配置和异常定义
- [Domain](/abs/path/C:/Users/mrqin/RiderProjects/skyroc/Domain): 领域层，包含实体和仓储接口
- [Infrastructure](/abs/path/C:/Users/mrqin/RiderProjects/skyroc/Infrastructure): 基础设施层，包含 EF Core、仓储实现、事务、缓存、数据库种子和迁移
- [Shared](/abs/path/C:/Users/mrqin/RiderProjects/skyroc/Shared): 共享层，包含公共常量、通用响应模型、配置模型和工具类
- [SkyRoc.Tests](/abs/path/C:/Users/mrqin/RiderProjects/skyroc/SkyRoc.Tests): 测试项目，当前包含最小映射回归测试

## 核心能力

- 用户登录、刷新令牌、注销
- 当前用户信息获取
- 基于角色的动态路由返回
- 用户管理
- 角色管理
- 菜单管理
- 菜单按钮管理
- 部门管理
- Redis 中的 AccessToken / RefreshToken 缓存
- Redis 不可用时的运行时内存降级
- 数据库迁移与默认种子初始化

## 已暴露 API

当前控制器位于 [SkyRoc/Controllers](/abs/path/C:/Users/mrqin/RiderProjects/skyroc/SkyRoc/Controllers)，主要包括：

- `AuthController`
- `UsersController`
- `RolesController`
- `MenusController`
- `DepartmentsController`
- `MenuButtonsController`

## 启动说明

### 1. 环境要求

- 已安装 `.NET SDK 9`
- PostgreSQL 可连接
- Redis 推荐可用，但不是本地启动的硬性前置条件

### 2. 关键配置

项目主要配置文件在 [appsettings.json](/abs/path/C:/Users/mrqin/RiderProjects/skyroc/SkyRoc/appsettings.json:1)。

需要注意：

- `ConnectionStrings:DefaultConnection` 当前在配置文件中配置
- `JwtSettings` 已直接在配置文件中提供签名密钥
- `DevSeed:Enabled=true` 时，开发种子账号密码直接从配置文件读取
- `Redis:Enabled=true` 且 `Redis:ConnectionString` 有效时，项目优先使用 Redis
- `launchSettings.json` 当前只用于声明开发环境和监听地址

### 3. 本地运行

在项目根目录执行：

```powershell
dotnet run --project .\SkyRoc\SkyRoc.csproj --launch-profile http
```

默认监听地址：

- `http://localhost:5293`

### 4. 数据库种子

应用启动时会自动执行数据库迁移和默认种子初始化，逻辑位于 [DbSeeder.cs](/abs/path/C:/Users/mrqin/RiderProjects/skyroc/Infrastructure/Data/DbSeeder.cs:1)。

当前默认会初始化：

- 角色：`Admin` / `User`
- 菜单与路由树
- 用户角色关系
- 角色菜单关系
- 默认部门：`总部`、`研发部`

## 默认账号

仅当 `Development` 环境且 `DevSeed:Enabled=true` 时，才会初始化默认种子账号。

如果数据库已经存在旧数据，种子不会覆盖已有记录。

## 联调方式

项目提供了现成的 HTTP 联调脚本 [SkyRoc.http](/abs/path/C:/Users/mrqin/RiderProjects/skyroc/SkyRoc/SkyRoc.http:1)。

推荐联调顺序：

1. `Health`
2. `Login`
3. `Get User Info`
4. `Get Routes`
5. `Menus - Get Tree`
6. `Departments - Get Tree`

使用方式：

- 先执行 `Login`
- 将返回的 `token` 和 `refreshToken` 填回顶部变量
- 将业务实体真实 ID 替换顶部占位变量后再调用更新、删除类接口

## Swagger

开发环境下会自动启用 Swagger。

默认访问入口：

- [http://localhost:5293/](http://localhost:5293/)

## 认证与授权说明

- 登录成功后会生成 `AccessToken` 和 `RefreshToken`
- `AccessToken` 中包含：
  - 用户 ID
  - 用户名
  - 邮箱
  - 角色编码 `ClaimTypes.Role`
  - 当前角色 ID `current_role_id`
- `getRoutes` 接口基于当前角色 ID 查询菜单并返回前端路由树

相关实现可参考：

- [AuthService.cs](/abs/path/C:/Users/mrqin/RiderProjects/skyroc/Application/Services/AuthService.cs:1)
- [JwtService.cs](/abs/path/C:/Users/mrqin/RiderProjects/skyroc/Application/Services/JwtService.cs:1)
- [AuthExtensions.cs](/abs/path/C:/Users/mrqin/RiderProjects/skyroc/SkyRoc/Extensions/AuthExtensions.cs:1)

## Redis 缓存与降级说明

缓存抽象位于 [ICacheService.cs](/abs/path/C:/Users/mrqin/RiderProjects/skyroc/Shared/Common/ICacheService.cs:1)，当前会在应用启动时根据 Redis 是否可用，选择 [RedisCacheService.cs](/abs/path/C:/Users/mrqin/RiderProjects/skyroc/Infrastructure/Caching/RedisCacheService.cs:1) 或 [MemoryCacheService.cs](/abs/path/C:/Users/mrqin/RiderProjects/skyroc/Infrastructure/Caching/MemoryCacheService.cs:1) 作为实现。

当前行为如下：

- 如果 `Redis:Enabled=true` 且启动时能建立连接，项目会直接使用 Redis 缓存
- 如果 Redis 在启动时不可达，或者显式配置 `Redis:Enabled=false`，项目会直接以纯内存模式启动
- 启动完成后不会在每个缓存请求上再次探测 Redis，也不会在运行时自动切换缓存实现

需要特别注意：

- 这里的启动期切换是为了保证登录、刷新令牌、注销等依赖 Token 缓存的流程尽量不中断
- 内存缓存仅在当前进程内生效，不跨实例、不跨进程共享
- 如果应用是以内存模式启动，后续即使 Redis 恢复，也不会自动切回 Redis，需重启应用
- 因此在多实例部署场景下，内存模式只能作为兜底，不适合作为长期运行形态

相关实现可参考：

- [RedisServiceCollectionExtensions.cs](/abs/path/C:/Users/mrqin/RiderProjects/skyroc/Infrastructure/Caching/RedisServiceCollectionExtensions.cs:1)
- [RedisCacheService.cs](/abs/path/C:/Users/mrqin/RiderProjects/skyroc/Infrastructure/Caching/RedisCacheService.cs:1)
- [MemoryCacheService.cs](/abs/path/C:/Users/mrqin/RiderProjects/skyroc/Infrastructure/Caching/MemoryCacheService.cs:1)

## 测试

当前测试项目位于 [SkyRoc.Tests](/abs/path/C:/Users/mrqin/RiderProjects/skyroc/SkyRoc.Tests:1)。

已包含：

- 部门树映射测试
- 菜单树映射测试
- Redis 启动时缓存选择测试

运行测试：

```powershell
dotnet test .\SkyRoc.Tests\SkyRoc.Tests.csproj
```

## 当前已知情况

- 如果当前实例是以内存模式启动的，内存缓存数据不会自动同步回 Redis，多实例场景下仍需谨慎评估一致性
- `appsettings.json` 当前保留数据库连接串、JWT 密钥和开发种子密码，如需上线建议再迁移到安全配置源
- 项目已有 `OperationLog` 实体与仓储，但还没有完整操作审计链路
- 缓存拦截器基础设施已经存在，但业务层尚未大规模接入 `Cacheable` / `CacheEvict`

## 建议下一步

- 增加服务层和集成测试，锁住 `login -> getUserInfo -> getRoutes` 主链路
- 完善 `OperationLog` 采集与查询能力
- 按部署环境决定是否将敏感配置迁移到环境变量或 Secret Manager
- 为 `SkyRoc.http` 增加更多真实业务场景示例
