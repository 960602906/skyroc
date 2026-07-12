# PostgreSQL 自动业务测试

## 用途与数据库边界

SkyRoc 自动业务测试与前端联调共用一个项目专用 PostgreSQL 数据库。项目维护者已在 2026-07-12 明确确认 `neondb` 是该专用测试库，因此非敏感配置 `SkyRoc.Tests/postgresql-testsettings.json` 将它登记为唯一精确白名单。测试宿主固定使用 `Testing` 环境；环境不是 `Testing`、连接串为空、实际数据库名不是 `neondb` 时，都会在建立连接或执行迁移前失败。

连接串默认读取项目的 `SkyRoc/appsettings.json`。需要在本机或 CI 覆盖时，使用 `SKYROC_TEST_CONNECTION_STRING`，不要把新的密码写入测试代码、脚本或报告。EF Core 设计时工厂只接受 `ConnectionStrings__DefaultConnection` 环境变量，不再内嵌远端账号或密码。

## 长期数据保护规则

- 不删除或重建数据库，不执行全表清空，也不修改无法确认归属的数据。
- 长期联调数据保持原主键、更新时间和记录数量；测试只能只读使用，不能拿人工数据做删除、作废或反审核探针。
- 单连接测试使用显式事务，测试结束时无论成功或异常都回滚。
- 跨连接或真实 HTTP 测试使用唯一 `SKYROC-AUTOTEST-*` 批次。每条临时记录必须同时登记实体类型、精确主键、归属字段和完整归属值。
- 清理器按创建登记的逆序删除，确保子记录先于父记录；删除条件同时包含主键与归属值。记录不存在时允许幂等重试，归属不匹配或仍有外键引用时立即失败并回滚本次清理。
- 测试异常时保留日志和质量报告；只重试能够确认属于本轮批次的记录。

## 运行方式

从仓库根目录运行：

```powershell
.\scripts\Invoke-PostgreSqlBusinessTests.ps1
```

如需用环境变量覆盖项目连接：

```powershell
$env:SKYROC_TEST_CONNECTION_STRING = '<专用测试库连接串>'
.\scripts\Invoke-PostgreSqlBusinessTests.ps1
```

脚本先解析连接串并核对 `Testing` 环境和 `neondb` 精确白名单，然后执行：

```powershell
dotnet ef database update --project Infrastructure --startup-project SkyRoc
dotnet test SkyRoc.Tests\SkyRoc.Tests.csproj --filter "FullyQualifiedName~PostgreSqlInfrastructureTests|FullyQualifiedName~PostgreSqlInfrastructureDocumentationTests|FullyQualifiedName~DatabaseMetadataInventoryTests|FullyQualifiedName~DataQualityReportWriterTests|FullyQualifiedName~DatabaseSafety|FullyQualifiedName~BatchCleanup"
```

测试夹具也会在建立真实测试连接后调用 `MigrateAsync`，保证直接运行测试时不会绕过待执行迁移。

## 基础质量报告

T0 报告写入忽略版本控制的 `artifacts/business-test-reports/`，每轮同时生成 JSON 和 Markdown 文件。报告至少包含：

- 每张业务表记录数；
- 每个持久化字段的非空、非空白填充率；
- 所有 `status` 和 `*_status` 字段的状态分布；
- 未验证的 PostgreSQL 外键约束；
- `code`、`*_code`、`*_no`、`username` 等业务编码的重复组数量；
- 所有文本字段中的 `SKYROC-AUTOTEST-*` 临时数据残留；
- 迁移历史、外键验证、库存非负和临时残留等基础业务一致性结果。

## T1 元数据盘点与质量规则

质量报告的 `metadataInventory` 是机器可读的全表清单，Markdown 同时提供“元数据盘点”表格。它以 EF Core 设计时模型和 `pg_catalog`/`information_schema` 双向核对业务表、持久化列、可空性、数值精度、表列注释、外键和唯一索引；框架表 `__EFMigrationsHistory` 不计入业务表。

每个业务表都会登记数量分类及验收目标，字段则登记为“始终必填”“业务条件适用”或“状态条件适用”。目前 `sys_setting` 被明确登记为单例/唯一键受限配置表；它按全部合法设置键验收，不以凑数方式满足普通表数量。其余表按用户权限与基础资料、主数据、业务主单、明细/关系/日志或普通表分类。新增表或字段会使对应的元数据门禁失败，不能通过扩大忽略范围绕过。

`qualityRuleExceptions` 是唯一允许的质量规则例外目录。当前仅登记 `sys_login_log.username`：登录审计是追加事件，同一账号多次登录是合法历史，不能误报为重复业务编码；其他 `username`、`code`、`*_code` 和 `*_no` 字段仍必须参与重复检测。

报告中的 `metadataFindings` 必须为空，以下一致性门禁必须为 `true`：

- `efModelMatchesPostgreSqlCatalog`
- `allBusinessTablesHaveQualityRules`
- `allPersistedColumnsHaveApplicabilityRules`
- `databaseCommentsMatchModel`
- `foreignKeysMatchModel`
- `uniqueConstraintsMatchModel`
