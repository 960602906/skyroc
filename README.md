# SkyRoc

`SkyRoc` 是一个基于 .NET 9 和 PostgreSQL 的生鲜供应链 Web API，采用 Clean Architecture 分层。系统已覆盖认证授权、基础资料、订单、采购、库存、配送、售后、财务、溯源、报表、导入导出、安全文件、打印和运营支撑能力。

P1 至 P4 的计划任务已全部完成；业务流程、Swagger、HTTP 示例、部署配置、格式和全量测试均已完成收口核对。

## 技术栈

- .NET 9 / C# 13
- ASP.NET Core Web API / Swashbuckle
- Entity Framework Core 9 / PostgreSQL
- Redis（不可用时可在单实例内降级为内存缓存）
- JWT Bearer / 资源操作权限
- FluentValidation / AutoMapper
- xUnit / `WebApplicationFactory<Program>`

## 项目结构

- [SkyRoc](./SkyRoc/)：Web API 入口、控制器、中间件、配置和 HTTP 示例
- [Application](./Application/)：DTO、验证器、映射和应用服务
- [Domain](./Domain/)：领域实体和仓储接口
- [Infrastructure](./Infrastructure/)：EF Core、仓储、迁移、缓存和种子数据
- [Shared](./Shared/)：公共常量、响应模型、选项和工具
- [SkyRoc.Tests](./SkyRoc.Tests/)：单元、契约和真实 HTTP 集成测试
- [client](./client/)：管理端前端（React + Vite），与本仓库同仓便于前后端联调

## 业务模块与开发进度

| 阶段 | 状态 | 范围 |
| --- | --- | --- |
| P1 系统权限与基础资料 | 已完成 | 认证、权限、基础资料、定价和第一阶段契约 |
| P2 订单主链路 | 已完成 | 订单、采购、库存、配送、签收回单和完整链路联调 |
| P3 售后与财务 | 已完成 | 售后、客户账单/结款、供应商结算和完整链路联调 |
| P4 查询与支撑 | 已完成 | 溯源、报表、驾驶舱、导入导出、文件、打印、系统支撑与全项目收口 |

完整状态见 [开发进度](./docs/开发进度.md)，验收顺序见 [自动开发任务清单](./docs/自动开发任务清单.md)，业务口径见 [业务流程文档](./docs/business-flows/00-global.md)。

## 核心能力

- 登录、令牌刷新、注销、个人中心、动态路由和细粒度授权
- 用户、角色、菜单、部门以及商品、客户、供应商、采购员、仓库等基础资料
- 报价、客户协议价和采购规则
- 销售订单、审核、采购计划、采购单和来源追溯
- 采购/其他/销售退货入库，销售/采购退货/其他出库，批次、流水、盘点和库存查询
- 配送任务、路线、司机、异常、客户签收、验收和回单归档
- 售后审核、取货任务、退货入库和客户账单调整
- 客户结款、供应商待结单据和供应商结算
- 检测报告、订单商品溯源、公开二维码详情和外部报送状态
- 销售、售后、库存、采购报表和首页驾驶舱
- CSV 导入导出、安全文件上传、打印模板和业务打印快照
- 服务时段、小程序下单、分拣权重、通知公告和安全审计日志

## API 与联调资料

控制器位于 [SkyRoc/Controllers](./SkyRoc/Controllers/)，Swagger 在 Development 环境生成。接口统一返回 `ApiResponse<T>`，JSON 使用 camelCase，时间字段按既有转换器输出 `yyyy-MM-dd HH:mm:ss`。

HTTP 脚本当前共记录 378 个请求，见 [SkyRoc.http](./SkyRoc/SkyRoc.http)。脚本覆盖认证、基础资料、订单、采购、库存、配送、售后、财务、溯源、报表、驾驶舱、导入导出、文件、打印和系统支撑接口。

推荐联调顺序：

1. `Health`、`Login`、`Get User Info`、`Get Routes`
2. 创建并审核销售订单
3. 生成采购计划和采购单，完成采购入库
4. 创建并审核销售出库，生成配送任务
5. 分配司机、配送、签收和回单
6. 按需执行售后、客户结款和供应商结算

## 安全配置

仓库内 [appsettings.json](./SkyRoc/appsettings.json) 按项目维护者决定保留默认数据库连接；该连接的访问控制、轮换与历史处理由项目维护者自行管理。JWT 签名密钥和开发种子密码不提供默认值，启动前必须通过环境变量或外部密钥管理服务注入。

PowerShell 示例：

```powershell
$env:ConnectionStrings__DefaultConnection = "<PostgreSQL connection string>"
$env:JwtSettings__SecretKey = "<high-entropy signing key>"
$env:JwtSettings__Issuer = "skyroc"
$env:JwtSettings__Audience = "skyroc"
$env:Redis__Enabled = "false"
$env:RustFS__AccessKey = "<rustfs-access-key>"
$env:RustFS__SecretKey = "<rustfs-secret-key>"
```

生产部署要求：

- 生产环境优先使用部署平台的密钥管理能力覆盖数据库连接串，并注入 JWT 密钥和外部 API Token。
- JWT 签名密钥应为独立生成的高熵随机值，并建立轮换流程。
- 多实例必须使用共享 Redis；内存降级只适合本地或单实例临时兜底。
- 文件二进制存放在 RustFS（S3 兼容）；通过 `RustFS__Endpoint`、`RustFS__BucketName`、`RustFS__AccessKey`、`RustFS__SecretKey` 注入，且不得把 Bucket 配成公开匿名可读。
- 反向代理终止 TLS 时，应正确转发协议和客户端地址，并限制可信代理范围。
- 发布前运行数据库迁移、健康检查、构建和全量测试。

## 本地启动

环境要求：.NET SDK 9 和可连接的 PostgreSQL。Redis 可通过 `Redis__Enabled=false` 关闭。

```powershell
dotnet restore SkyRoc.sln
dotnet build SkyRoc.sln --no-restore
dotnet ef database update --project Infrastructure --startup-project SkyRoc
dotnet run --project SkyRoc\SkyRoc.csproj --launch-profile http
```

默认地址为 `http://localhost:5293`，Development 环境的 Swagger UI 位于根路径。应用不会在启动时自动迁移数据库或写入开发种子，部署流程必须显式执行迁移；如需种子数据，应由受控初始化流程调用 [DbSeeder.cs](./Infrastructure/Data/DbSeeder.cs)，不得在生产环境启用开发账号。

## Redis 缓存与降级

缓存实现位于 [Infrastructure/Caching](./Infrastructure/Caching/)。启动时 Redis 可用则使用 Redis；Redis 被禁用或不可达则选择进程内缓存。启动完成后不会自动在两种实现间切换，内存缓存也不会跨实例共享，因此多实例部署不能把内存模式作为长期运行形态。

## 数据库迁移

设计时工厂位于 [DbContextFactory.cs](./Infrastructure/Data/DbContextFactory.cs)。

```powershell
dotnet ef migrations add <Name> --project Infrastructure --startup-project SkyRoc
dotnet ef database update --project Infrastructure --startup-project SkyRoc
dotnet ef migrations has-pending-model-changes --project Infrastructure --startup-project SkyRoc
```

每个持久化实体和字段都必须具有 PostgreSQL 注释；生成迁移后需检查 `Up` 和 `Down` 的表、字段、约束、索引及注释变化。

## 验证

```powershell
dotnet build SkyRoc.sln --no-restore
dotnet test SkyRoc.Tests\SkyRoc.Tests.csproj --no-build
dotnet format SkyRoc.sln --verify-no-changes
dotnet ef migrations has-pending-model-changes --project Infrastructure --startup-project SkyRoc
```

测试覆盖领域服务、状态机、权限、序列化、数据库结构与注释、Swagger 请求/响应契约以及三阶段真实 HTTP 主链路。部署配置和项目收口文档也有回归测试，防止敏感默认值或已清理断点重新进入仓库。

## 已知边界

- Redis 内存降级不提供跨实例一致性。
- 外部平台报送当前只维护状态和日志，实际网络报送需按目标平台协议另行接入。
- 当前导入导出作业只支持商品 CSV；新增 `jobType` 必须同步验证、权限、Swagger、HTTP 示例和业务文档。
- 独立期初库存维护、采购协议价、配送单分组打印等仍属于明确记录的后续产品范围，不是 P4-10 收口缺陷。
