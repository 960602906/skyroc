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
dotnet test SkyRoc.Tests\SkyRoc.Tests.csproj --filter "FullyQualifiedName~PostgreSqlInfrastructureTests|FullyQualifiedName~PostgreSqlInfrastructureDocumentationTests|FullyQualifiedName~DatabaseSafety|FullyQualifiedName~BatchCleanup"
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

T1 会在这套扫描底座上补充 EF 模型与 PostgreSQL 目录逐项比对、字段适用条件、固定配置例外和更完整的业务一致性门禁。
