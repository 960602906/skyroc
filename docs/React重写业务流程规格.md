# React 重写业务流程规格索引

这套文档用于把当前 Vue 后台项目拆成面向 React 重写的业务蓝图。每个业务模块一个 Markdown 文件，包含业务目标、业务流程图、页面清单、接口清单、关键字段、状态逻辑和重写提示。

## 文档目录

| 模块 | 文档 |
| --- | --- |
| 主业务流程总图 | [99-main-business-flow.md](./business-flows/99-main-business-flow.md) |
| 全局约定 | [00-global.md](./business-flows/00-global.md) |
| 首页驾驶舱 | [01-dashboard.md](./business-flows/01-dashboard.md) |
| 商品模块 | [02-goods.md](./business-flows/02-goods.md) |
| 客户模块 | [03-customer.md](./business-flows/03-customer.md) |
| 订单模块 | [04-order.md](./business-flows/04-order.md) |
| 售后模块 | [05-after-sales.md](./business-flows/05-after-sales.md) |
| 采购模块 | [06-purchase.md](./business-flows/06-purchase.md) |
| 库存模块 | [07-storage.md](./business-flows/07-storage.md) |
| 配送模块 | [08-delivery.md](./business-flows/08-delivery.md) |
| 财务模块 | [09-finance.md](./business-flows/09-finance.md) |
| 报表模块 | [10-reports.md](./business-flows/10-reports.md) |
| 溯源模块 | [11-traceability.md](./business-flows/11-traceability.md) |
| 系统模块 | [12-system.md](./business-flows/12-system.md) |
| 登录、个人中心与遗留页面 | [13-auth-personal-legacy.md](./business-flows/13-auth-personal-legacy.md) |

## 建议使用方式

1. 先读全局约定，确定登录、权限、分页、导入导出、打印、表头配置这些共用能力。
2. 按重写顺序阅读模块文档：基础资料 -> 订单 -> 采购 -> 库存 -> 配送 -> 财务 -> 报表/溯源/系统。
3. 每个模块落地前，把对应文档里的接口请求体、响应体再用后端 OpenAPI/Swagger 校准。
4. React 项目中每个 `features/*` 目录建议保留一份同名业务说明，便于后续 AI 和开发者继续迭代。
