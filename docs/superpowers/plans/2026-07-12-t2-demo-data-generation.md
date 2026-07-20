# T2 全量联调数据生成器 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在唯一允许的 PostgreSQL 联调库中安全、可重复地补齐受稳定业务键管理的长期联调数据，并能以报告证明完整性。

**Architecture:** 生成器放在 `SkyRoc.Tests/Testing/PostgreSql`，仅由 PostgreSQL 白名单夹具构造并显式调用；它把“稳定键目录、分层写入、结果快照”分开。具备公开写入口的聚合通过真实应用服务/HTTP 建立，其余支撑关系由受控构造器精确按稳定键 upsert；不删除或模糊匹配任何既存数据。

**Tech Stack:** .NET 9、EF Core 9、Npgsql/PostgreSQL、xUnit、既有 `PostgreSqlTestFixture` 与 JSON/Markdown 报告器。

## Global Constraints

- 仅可使用经 `DatabaseSafetyGuard` 精确校验为 `skyroc` 的 `Testing` 数据库。
- 严禁 `DROP DATABASE`、重建、全表 `TRUNCATE`、删除未由生成器完整稳定键确认归属的数据。
- 长期业务键必须为 `SKYROC-DEMO-<AREA>-<NNN>`，只允许完整键精确定位；临时探针使用本轮 `SKYROC-AUTOTEST-*` 批次并精确清理。
- 任何数量、单价、成本、金额计算均使用 `Shared.Constants.NumericPrecision`；所有注释为中文。
- 本计划不提交、推送或创建 PR；只有 T2 的全部验收门禁通过才更新任务清单为 `[x]`。

---

### Task 1: 生成入口与幂等结果契约

**Files:**
- Create: `SkyRoc.Tests/Testing/PostgreSql/DemoDataGenerationResult.cs`
- Create: `SkyRoc.Tests/Testing/PostgreSql/DemoDataGenerator.cs`
- Create: `SkyRoc.Tests/PostgreSql/DemoDataGeneratorTests.cs`
- Modify: `SkyRoc.Tests/Testing/PostgreSql/PostgreSqlTestFixture.cs`

**Interfaces:**
- Produces: `Task<DemoDataGenerationResult> GenerateDemoDataAsync(CancellationToken)`。
- Consumes: `PostgreSqlTestFixture.CreateDbContext()` 和 `DemoDataStableKeyCatalog.Create(area, sequence)`。

- [x] **Step 1: 写入失败的真实 PostgreSQL 测试。** 断言第一次调用仅新增稳定键管理记录、第二次调用不新增重复记录，并且生成结果列出每层新增/更新数。
- [x] **Step 2: 运行 RED。** `dotnet test SkyRoc.Tests/SkyRoc.Tests.csproj --filter "FullyQualifiedName~DemoDataGeneratorTests"`；预期因入口不存在失败。
- [x] **Step 3: 实现最小安全入口。** 入口先调用 `DatabaseSafetyGuard.Validate`，使用独立 DbContext 和每层事务；发生异常时停止后续层，不尝试清理长期数据。
- [x] **Step 4: 运行 GREEN。** 重跑同一测试，读取两次前后受管业务键和总行数，确认第二次增量为零。

### Task 2: 权限与基础资料层

**Files:**
- Modify: `SkyRoc.Tests/Testing/PostgreSql/DemoDataGenerator.cs`
- Create: `SkyRoc.Tests/Testing/PostgreSql/DemoDataBaseCatalogBuilder.cs`
- Modify: `SkyRoc.Tests/PostgreSql/DemoDataGeneratorTests.cs`

**Interfaces:**
- Produces: 受管用户、角色、菜单、部门、公司、客户标签、商品分类、仓库、承运商、司机、采购员及其关系的稳定键索引。
- Consumes: `ApplicationDbContext`、公开基础资料服务和明确的受控支撑关系构造器。

- [ ] **Step 1: 写入 RED 用例。** 覆盖 30–50 条权限/基础资料、空白拒绝、人工同名不被修改、恢复被删除的单一受管键、二次运行关系无重复。
- [ ] **Step 2: 实现基础资料按依赖层 upsert。** 对具有 API 写入口的公司、客户、商品等调用应用服务；仅为关系/固定配置使用受控数据库构造器。
- [ ] **Step 3: 断言字段质量。** 所有适用可写字段填写非空、有业务含义文本；每个状态字段至少有语义正确的记录。
- [ ] **Step 4: 运行真实 PostgreSQL 测试。** 确认人工记录主键、更新时间和数量不变。

### Task 3: 商品、客户、定价与采购支撑层

**Files:**
- Modify: `SkyRoc.Tests/Testing/PostgreSql/DemoDataGenerator.cs`
- Create: `SkyRoc.Tests/Testing/PostgreSql/DemoDataPricingBuilder.cs`
- Modify: `SkyRoc.Tests/PostgreSql/DemoDataGeneratorTests.cs`

- [ ] **Step 1: 写入 RED 用例。** 覆盖 30–80 条商品、客户、供应商、采购员、仓库、报价和协议价，以及 100–300 条单位、标签、客户绑定、采购规则关系。
- [ ] **Step 2: 通过服务构建主数据与价格来源。** 覆盖有效/过期协议、多个单位换算、启用/停用资料和跨区域地址；金额和数量使用全局精度。
- [ ] **Step 3: 数据库断言。** 逐键检查唯一性、外键、快照来源、价格优先级及不修改人工数据。
- [ ] **Step 4: 运行 GREEN。** 连续运行两次，要求受管键数量稳定。

### Task 4: 订单、采购与库存链路层

**Files:**
- Modify: `SkyRoc.Tests/Testing/PostgreSql/DemoDataGenerator.cs`
- Create: `SkyRoc.Tests/Testing/PostgreSql/DemoDataOrderInventoryBuilder.cs`
- Modify: `SkyRoc.Tests/PostgreSql/DemoDataGeneratorTests.cs`

- [ ] **Step 1: 写入 RED 用例。** 覆盖 50–100 张订单、计划、采购、入出库、盘点主单与 100–300 条明细、批次、流水。
- [ ] **Step 2: 用真实服务/HTTP 形成状态流。** 创建草稿、处理中、成功终态和允许的失败/取消状态；不直接伪造服务应产生的快照、批次或流水。
- [ ] **Step 3: 写入数据库不变量。** 按 `NumericPrecision` 核对主明细金额，批次可用量非负且等于有效流水汇总。
- [ ] **Step 4: 注入失败并重试。** 失败层回滚自身写入，重试仅补齐缺失稳定键。

### Task 5: 配送、售后、财务与溯源链路层

**Files:**
- Modify: `SkyRoc.Tests/Testing/PostgreSql/DemoDataGenerator.cs`
- Create: `SkyRoc.Tests/Testing/PostgreSql/DemoDataDownstreamBuilder.cs`
- Modify: `SkyRoc.Tests/PostgreSql/DemoDataGeneratorTests.cs`

- [ ] **Step 1: 写入 RED 用例。** 覆盖 50–100 张配送、售后、账单、结算、检测报告主表及 100–300 条明细、验收、结算和追溯关系。
- [ ] **Step 2: 形成端到端正逆向链路。** 销售出库→配送→签收→应收/结款，以及售后→取货→退货入库→冲减，采购入库→应付/结算与检测→溯源。
- [ ] **Step 3: 数据库断言。** 账单余额独立重算，来源明细幂等唯一，售后/库存/结算状态符合来源状态。
- [ ] **Step 4: 连续两次运行验证。** 第二次不新增业务事实、不修改人工记录。

### Task 6: 系统支撑、命令与联调说明

**Files:**
- Modify: `scripts/Invoke-PostgreSqlBusinessTests.ps1`
- Create: `docs/testing/前端联调数据说明.md`
- Modify: `SkyRoc.Tests/PostgreSql/PostgreSqlInfrastructureDocumentationTests.cs`

- [ ] **Step 1: 写入 RED 文档/命令测试。** 要求脚本显式调用 T2 生成、连续两次质量验收并保留 JSON/Markdown 路径。
- [ ] **Step 2: 实现受保护命令。** 复用现有连接白名单，先迁移、再运行生成测试两次、最后报告；禁止任何宽范围删除命令。
- [ ] **Step 3: 编写联调说明。** 只记录非敏感账号来源、稳定编码前缀、场景索引、幂等重试和数据安全边界，不写密码或连接串。
- [ ] **Step 4: 运行 GREEN。** 验证错误库名、空连接串、人工同名记录均 fail-closed 或保持不变。

### Task 7: 全量验收与断点更新

**Files:**
- Modify: `docs/自动测试任务清单.md`
- Modify: `docs/开发进度.md`

- [ ] **Step 1: 连续运行两次真实 PostgreSQL 生成命令。** 记录逐表增量与第二次零重复增量。
- [ ] **Step 2: 生成最终质量报告。** 验收每表数量、适用字段 100%、状态覆盖、孤儿外键、重复编码、临时残留、库存和结算一致性。
- [ ] **Step 3: 运行项目门禁。** `dotnet build SkyRoc.sln --no-restore`；`dotnet test SkyRoc.Tests/SkyRoc.Tests.csproj --no-build`；`dotnet format SkyRoc.sln --verify-no-changes`。
- [ ] **Step 4: 仅在所有门禁通过后勾选 T2。** 更新测试数量、数据库基线、验收日期和下一断点；任一失败则保留 T2 `[ ]` 并记录报告路径与阻塞原因。
