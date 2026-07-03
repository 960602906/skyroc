# SkyRoc

`SkyRoc` 是一个基于 `.NET 9` 的生鲜供应链后台项目。第一阶段已完成认证、系统权限和基础资料能力，第二阶段已完成销售订单 CRUD、审核状态机与接口联调收口。

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

- [SkyRoc](./SkyRoc/): Web API 启动项目，包含控制器、中间件、启动配置和 HTTP 联调脚本
- [Application](./Application/): 应用层，包含 DTO、业务服务、校验器、映射配置和异常定义
- [Domain](./Domain/): 领域层，包含实体和仓储接口
- [Infrastructure](./Infrastructure/): 基础设施层，包含 EF Core、仓储实现、事务、缓存、数据库种子和迁移
- [Shared](./Shared/): 共享层，包含公共常量、通用响应模型、配置模型和工具类
- [SkyRoc.Tests](./SkyRoc.Tests/): xUnit 测试，覆盖映射、缓存、授权、基础资料与订单服务、API 集成、序列化和 Swagger 契约

## 业务模块与开发进度

**P1 系统权限与基础资料已经完成，当前进入 P2-09 采购单模型。** 销售订单模型、事务化 CRUD、审核状态机、细粒度权限、Swagger、HTTP 主流程和 API 集成测试均已收口；采购计划已支持查询、生成、供应商/采购员分配、合并和按订单或商品数量拆分。

| 阶段 | 状态 | 范围 |
| --- | --- | --- |
| P1 系统权限与基础资料 | 已完成 | 认证、系统权限、基础资料、定价、回归测试与 API 契约 |
| P2 订单主链路 | 进行中 | 订单 CRUD、审核联调和采购计划操作已完成，当前执行 P2-09 采购单模型 |
| P3 售后与财务 | 待开始 | 售后、客户结算、供应商结算 |
| P4 查询与支撑 | 待开始 | 溯源、报表、驾驶舱、打印、日志 |

完整模块状态见 [业务模块与开发进度](./docs/开发进度.md)，逐项交付物和验收条件见 [自动开发任务清单](./docs/自动开发任务清单.md)。每次开发只完成清单中第一个未勾选任务，验收通过后再推进断点。

## 核心能力

- 用户登录、刷新令牌、注销
- 当前用户信息获取
- 基于角色的动态路由返回
- 基于稳定权限编码和 JWT 权限声明的授权策略
- 用户管理
- 角色管理
- 菜单管理
- 菜单按钮管理
- 部门管理
- 当前用户资料查询、更新和修改密码
- 商品分类、商品档案和商品单位
- 公司、客户、客户标签和子账号
- 供应商、采购员和仓库基础资料
- 报价单、报价商品、客户协议价和采购规则
- 销售订单分页、详情、创建、编辑、删除和审核状态流转
- 采购计划查询、生成、供应商/采购员分配、合并和拆分
- Redis 中的 AccessToken / RefreshToken 缓存
- Redis 不可用时的运行时内存降级
- 数据库迁移与默认种子初始化

## 已暴露 API

当前控制器位于 [SkyRoc/Controllers](./SkyRoc/Controllers/)，按能力分为：

- 认证与权限：`Auth`、`Users`、`Roles`、`Menus`、`MenuButtons`、`Departments`
- 商品与定价：`GoodsTypes`、`Goods`、`GoodsUnits`、`Quotations`、`QuotationGoods`、`CustomerProtocols`、`CustomerProtocolGoods`
- 客户与采购基础资料：`Companies`、`Customers`、`CustomerTags`、`CustomerSubAccounts`、`Suppliers`、`Purchasers`、`PurchaseRules`、`Wares`
- 订单：`Orders`
- 采购计划：`PurchasePlans`

采购单、库存单据、配送、售后、财务、报表和溯源 API 尚未实现。

HTTP 脚本当前共记录 192 个请求；第一阶段真实路由、响应约定和权限编码见 [第一阶段 API 契约](./docs/第一阶段API契约.md)。

## 启动说明

### 1. 环境要求

- 已安装 `.NET SDK 9`
- PostgreSQL 可连接
- Redis 推荐可用，但不是本地启动的硬性前置条件

### 2. 关键配置

项目主要配置文件在 [appsettings.json](./SkyRoc/appsettings.json)。

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

应用启动时会自动执行数据库迁移和默认种子初始化，逻辑位于 [DbSeeder.cs](./Infrastructure/Data/DbSeeder.cs)。

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

项目提供了覆盖第一阶段全部接口及销售订单主流程的 HTTP 联调脚本 [SkyRoc.http](./SkyRoc/SkyRoc.http)。

推荐联调顺序：

1. `Health`
2. `Login`
3. `Get User Info`
4. `Get Routes`
5. `Menus - Get Tree`
6. `Departments - Get Tree`
7. `Orders - Create`，回填返回的订单及明细 ID
8. `Orders - Update` → `Reject` → `Resubmit` → `Approve`

使用方式：

- 先执行 `Login`
- 将返回的 `token` 和 `refreshToken` 填回顶部变量
- 将业务实体真实 ID 替换顶部占位变量后再调用更新、删除类接口

## Swagger

开发环境下会自动启用 Swagger。

默认访问入口：

- [http://localhost:5293/](http://localhost:5293/)

受保护操作会显示 Bearer 认证要求；细粒度授权操作会同时显示所需权限码。登录和刷新令牌不会误标为需要 Bearer。

## 认证与授权说明

- 登录成功后会生成 `AccessToken` 和 `RefreshToken`
- `AccessToken` 中包含：
  - 用户 ID
  - 用户名
  - 邮箱
  - 角色编码 `ClaimTypes.Role`
  - 当前角色 ID `current_role_id`
- `permission` Claim 包含角色菜单对应的按钮权限编码，管理员使用超级权限 `*:*:*`
- API 权限策略采用 `module:resource:action` 编码，由统一授权要求和处理器校验
- `getRoutes` 接口基于当前角色 ID 查询菜单并返回前端路由树

相关实现可参考：

- [AuthService.cs](./Application/Services/AuthService.cs)
- [JwtService.cs](./Application/Services/JwtService.cs)
- [AuthExtensions.cs](./SkyRoc/Extensions/AuthExtensions.cs)

## Redis 缓存与降级说明

缓存抽象位于 [ICacheService.cs](./Shared/Common/ICacheService.cs)，当前会在应用启动时根据 Redis 是否可用，选择 [RedisCacheService.cs](./Infrastructure/Caching/RedisCacheService.cs) 或 [MemoryCacheService.cs](./Infrastructure/Caching/MemoryCacheService.cs) 作为实现。

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

- [RedisServiceCollectionExtensions.cs](./Infrastructure/Caching/RedisServiceCollectionExtensions.cs)
- [RedisCacheService.cs](./Infrastructure/Caching/RedisCacheService.cs)
- [MemoryCacheService.cs](./Infrastructure/Caching/MemoryCacheService.cs)

## 测试

当前测试项目位于 [SkyRoc.Tests](./SkyRoc.Tests/)。

已包含：

- 部门树映射测试
- 菜单树映射测试
- Redis 启动时缓存选择测试
- Redis 健康检查测试
- 客户资料与天眼查补充逻辑测试
- 固定日期格式序列化测试
- 权限编码、JWT 权限声明和授权处理器允许/拒绝测试
- 系统管理控制器权限策略映射回归测试
- 商品定价及客户采购基础资料服务回归测试
- Swagger 匿名/受保护操作认证契约测试
- 订单模型、映射、验证、仓储、事务化服务和审核状态机测试
- 订单认证授权、Swagger 权限说明和 HTTP API 主流程集成测试

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

按 [自动开发任务清单](./docs/自动开发任务清单.md) 顺序执行，不跳项。当前任务是 **P2-09 采购单模型**；完成并验收后再领取 P2-10。
