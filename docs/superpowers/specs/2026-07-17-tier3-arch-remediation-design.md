# 三档架构整改设计（#10/#11/#12/#14，跳过 #13）

> 日期：2026-07-17  
> 分支：`fix/backend-arch-remediation`  
> 决策：全部方案 A；密钥明文（#13）不改；启动迁移（#14）改文档不恢复自动迁移。

## 目标

在不改变业务行为与事务边界的前提下：

1. 收敛 `StockInService` 的批次/成本职责（#10）
2. 解除库存服务对供应商账单的直接依赖（#11）
3. 接入 Serilog + OpenTelemetry（#12）
4. 文档与 README「启动不自动迁移/种子」对齐（#14）

非目标：异步 outbox、改账单算法、轮换/迁出密钥、恢复启动 `MigrateAsync`。

## #14 文档对齐

### 现状

- `Program.cs` 中 `DbSeeder.SeedAsync`（内含 `MigrateAsync`）已注释
- `README.md` 已写明启动不自动迁移/种子
- `CLAUDE.md` 仍写 “auto-runs migrations and dev seeding on startup”

### 改法

- 修改 `CLAUDE.md`：改为与 README 一致——须显式 `dotnet ef database update`；种子仅受控调用 `DbSeeder`，默认 `DevSeed:Enabled=false`
- 若 `AGENTS.md` / `docs/后端架构评审与整改.md` 有同类过时表述，一并修正
- **不**恢复 `Program.cs` 注释块

## #12 可观测性（Serilog + OpenTelemetry）

### 组件

| 组件 | 用途 |
| --- | --- |
| Serilog | 结构化日志；控制台 + 滚动文件（Development 可仅控制台） |
| OpenTelemetry | ASP.NET Core + HttpClient + EF Core tracing |
| OTLP exporter | 可选；未配置 endpoint 时不导出，仅保留 Activity 源 |

### 接入点

- `SkyRoc/Program.cs`：`builder.Host.UseSerilog(...)`；注册 OTel
- 新扩展：`SkyRoc/Extensions/ObservabilityExtensions.cs`（或拆 `SerilogExtensions` / `OpenTelemetryExtensions`）
- `appsettings.json` / `appsettings.Development.json`：`Serilog` 与 `OpenTelemetry` 配置段（**不含密钥**；OTLP endpoint 用空或占位）
- `SkyRoc.csproj`：Serilog / OTel 相关包引用
- 请求管道：`UseSerilogRequestLogging()` 放在异常中间件之后、路由附近（不改变业务 HTTP 200 约定）

### 约束

- 不替换现有 `ILogger<T>` 调用点（Serilog 作 provider）
- 健康检查 `/health` 可不进请求日志采样（可选过滤）
- 集成测试宿主若受 Serilog 影响，保持可关闭或使用默认配置不破坏现有测试

## #10 拆分库存成本/批次服务

### 边界

新建应用服务：

- `Application/Interfaces/Storage/IInventoryCostingService.cs`
- `Application/Services/Storage/InventoryCostingService.cs`

职责从 `StockInService`（及对称需要时从 `StockOutService` 只迁「可复用」部分；**本次以入库为主**，出库若方法签名差异大则仅抽共用纯计算，出库批次逻辑可留待后续）迁出：

- `ResolveBatchForInboundAsync`
- `ApplyInboundToBatch`（移动加权）
- `ApplyReversalToBatch`
- `CreateInboundLedger` / 反审核相关 ledger 创建
- `UnitCostPerBase` 等纯成本计算

`StockInService` 保留：单据 CRUD、校验、审核/反审核编排、状态流转；审核循环内改为调用 `IInventoryCostingService`。

### 行为不变

- 仍在同一 `unitOfWork.ExecuteInTransactionAsync` 内执行
- `NumericPrecision` 舍入规则不变
- 审计字段仍由调用方或 costing 服务通过既有 `Apply*Audit` 扩展写入（与现逻辑一致）

### DI

Scrutor 约定：`InventoryCostingService` → `IInventoryCostingService`，无需手写注册。

## #11 库存↔财务解耦（同事务应用事件）

### 原则

- **不**引入最终一致 / 外部消息总线
- 事件在**当前事务提交前**同步处理，失败则整单回滚（与现直调账单一致）

### 最小事件总线

Application 层：

- `IApplicationEvent` 标记接口（或无成员）
- `IApplicationEventPublisher`：`PublishAsync(IApplicationEvent, CancellationToken)`
- `IApplicationEventHandler<TEvent>`：`HandleAsync(TEvent, CancellationToken)`
- `ApplicationEventPublisher`：从 DI 解析对应 handler 并顺序执行

注册：在 `Application/DependencyInjection.cs` 扫描 `IApplicationEventHandler<>` 并注册为 scoped。

### 事件

| 事件 | 何时发布 | Handler 行为 |
| --- | --- | --- |
| `PurchaseStockInAudited` | 采购入库审核成功写库后 | `SyncPurchaseStockInAsync` |
| `PurchaseStockInReversalRequested` | 反审核前校验点 | `EnsureCanReverseSourceDocumentAsync` |
| `PurchaseStockInReversed` | 采购入库反审核完成写库后 | `RemoveBySourceDocumentAsync` |
| 出库对称事件（采购退货出库） | `StockOutService` 对应点 | 现有 `SyncPurchaseReturnOutAsync` / Ensure / Remove |

载荷携带必要实体引用或 Id（优先传已加载的 `StockInOrder`/`StockOutOrder` 以免重复查询，与现直调一致）。

### 调用方改动

- `StockInService` / `StockOutService`：**删除** `ISupplierBillService` 构造注入
- 改为注入 `IApplicationEventPublisher`，在原调用点 `PublishAsync(...)`
- Handler 内注入 `ISupplierBillService`

### 测试

- 现有 `StockInServiceTests` / `StockOutServiceTests` / 财务相关测试应继续通过（行为不变）
- 可补 1 个轻量测试：publisher 能调度 handler（可选）

## 实施顺序与提交

1. `#14` 文档（独立小提交）
2. `#12` 可观测性（独立提交）
3. `#10` InventoryCosting 抽取（独立提交）
4. `#11` 事件解耦入库+出库账单（独立提交）

每步：`dotnet build` + 聚焦 `StockIn`/`StockOut`/Finance 相关测试。

## 风险

| 风险 | 缓解 |
| --- | --- |
| Handler 漏注册导致静默不入账 | Publisher 在无 handler 时对关键财务事件抛明确异常；或启动时校验 |
| OTel 包拖慢 CI | Development 默认不强制 OTLP；测试可不启 exporter |
| 出库与入库 costing 不对称 | #10 明确入库优先；出库只做事件解耦 |

## 验收

- [ ] CLAUDE.md 与 README 关于启动迁移描述一致；`Program.cs` 仍不自动 Seed
- [ ] 日志为 Serilog；配置可调；OTLP 可选
- [ ] `StockInService` 不再包含批次加权私有实现（迁至 costing）
- [ ] `StockInService`/`StockOutService` 不再直接依赖 `ISupplierBillService`
- [ ] 审核/反审核后供应商账单行为与改前一致（测试绿）
