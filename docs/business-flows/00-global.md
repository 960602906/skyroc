# 全局约定

## 业务定位

当前系统是生鲜供应链后台，核心链路是：客户下单 -> 订单审核 -> 采购计划 -> 采购单 -> 入库 -> 销售出库 -> 配送 -> 签收/回单 -> 售后/结算。

```mermaid
flowchart TD
  A["客户/后台创建订单"] --> B["订单审核"]
  B --> C["生成采购计划"]
  C --> D["生成采购单据"]
  D --> E["采购入库"]
  B --> F["生成销售出库"]
  E --> F
  F --> G["配送任务"]
  G --> H["客户签收/回单"]
  H --> I["单位结算"]
  E --> J["供应商结算"]
  H --> K["售后处理"]
  K --> L["退货入库/退款/补货"]
```

## 前端重写目标

- 用 React + TypeScript 重建，不迁移 Vue 代码。
- 旧项目只作为业务、接口、字段和状态来源。
- 新项目建议按领域模块组织，而不是照搬页面目录。

推荐目录：

```text
src/
  app/
  shared/
    api/
    auth/
    components/
    constants/
    hooks/
    utils/
  features/
    dashboard/
    goods/
    customer/
    order/
    after-sales/
    purchase/
    storage/
    delivery/
    finance/
    reports/
    traceability/
    system/
```

## 请求约定

旧项目请求封装来源：`src/utils/request.js`。

| 项 | 规则 |
| --- | --- |
| baseURL | `process.env.VUE_APP_BASE_API` |
| token | `Authorization: Bearer ${token}` |
| 超时 | 3 分钟 |
| 成功码 | 通常 `code === 200` |
| 列表响应 | 通常 `rows + total` |
| 详情响应 | 通常 `data` |
| 登录失效 | `code === 401` 时清 token 并重新登录 |
| 导出 | blob 响应，需兼容 blob 中返回 JSON 错误 |

建议 React 类型：

```ts
export type ApiResult<T> = {
  code: number;
  msg?: string;
  data: T;
};

export type PageResult<T> = {
  code: number;
  msg?: string;
  rows: T[];
  total: number;
};

export type PageParams = {
  pageNum: number;
  pageSize: number;
};
```

## 登录权限流程

```mermaid
flowchart TD
  A["登录页提交账号密码"] --> B["POST /login"]
  B --> C["保存 token"]
  C --> D["GET /getInfo"]
  D --> E["保存 roles 与 permissions"]
  E --> F["GET /getRouters"]
  F --> G["生成菜单和动态路由"]
  G --> H["进入后台首页"]
  D --> I{"401 或权限失效"}
  I --> J["清 token 并跳转登录"]
```

接口：

| 动作 | 方法 | URL |
| --- | --- | --- |
| 登录 | POST | `/login` |
| 用户信息 | GET | `/getInfo` |
| 登出 | POST | `/logout` |
| 菜单路由 | GET | `/getRouters` |
| 查询本人资料 | GET | `/api/system/user/profile` |
| 更新本人资料 | PUT | `/api/system/user/profile` |
| 修改密码 | PUT | `/api/system/user/profile/updatePwd` |

权限规则：

- 用户权限来自 `getInfo.data.permissions`。
- API 权限编码采用稳定的 `module:resource:action` 三段式格式，已发布编码不得复用或改变含义。
- 用户角色关联的菜单按钮编码会在登录和刷新令牌时写入 `permission` JWT Claim；`getUserInfo` 同时返回 `permissions`，并通过 `buttons` 兼容现有前端字段。
- 独立业务动作的授权策略以权限编码为策略名，由统一的 `PermissionRequirement` 和 `PermissionAuthorizationHandler` 校验。
- 基础资料通用 CRUD 通过控制器的权限资源与 `read/create/update/delete` 操作组合成权限编码，由 `ResourcePermissionRequirement` 和处理器校验，避免继承动作漏配。
- 超级权限：`*:*:*`。
- 旧项目管理员角色：`admin`、`superadmin`、`administrator`、`pljzLk`。
- 新项目建议把菜单权限、按钮权限、接口权限拆成独立工具。

当前已固定系统管理与基础资料权限编码。商品、客户、定价、采购和仓库控制器已接入细粒度策略；报价审核使用独立的 `business:pricing:audit` 权限。

## 通用列表流程

```mermaid
flowchart TD
  A["进入列表页"] --> B["初始化下拉选项"]
  B --> C["合并默认查询条件"]
  C --> D["请求列表接口"]
  D --> E["展示表格 rows/total"]
  E --> F{"用户操作"}
  F --> G["搜索/重置"]
  F --> H["分页/排序"]
  F --> I["勾选批量操作"]
  F --> J["行内详情/编辑"]
  F --> K["导入/导出/打印"]
  G --> D
  H --> D
  I --> D
```

通用能力：

- 分页：`pageNum`、`pageSize`。
- 表头配置：旧项目由后端保存表格字段显示顺序；本次重写已移出需求范围，不迁移（详见下文“表头配置”）。
- 批量选择：支持当前页选择和全选查询结果。
- 导入导出：统一 multipart job 接口。
- 打印：选择模板 -> 获取打印数据 -> 预览/打印。

## 基础下拉接口

| 数据 | 方法 | URL |
| --- | --- | --- |
| 仓库 | GET | `/business/ware/dropList` |
| 部门 | GET | `/system/dept/dropList` |
| 客户 | GET | `/business/customer/list` |
| 供应商 | GET | `/business/supplier/list` |
| 采购员 | GET | `/business/purchaser/list` |
| 商品分类 | GET | `/business/goods/type/list` |
| 打印分类 | GET | `/business/goods/type/flat/list` |
| 承运商 | GET | `/business/carrier/list` |
| 司机 | GET | `/business/driver/list` |
| 客户标签树 | GET | `/business/customer/tag/tree/list` |

## 导入导出

```mermaid
flowchart TD
  A["用户点击导出"] --> B["组装 xxxExportParam"]
  B --> C["POST /business/multipart/job/exportJob/{jobType}"]
  C --> D{"响应类型"}
  D --> E["blob 文件流"]
  D --> F["blob 内 JSON 错误"]
  E --> G["解析 content-disposition 文件名并下载"]
  F --> H["提示错误 msg"]
```

SkyRoc 当前采用同步 CSV 作业：请求处理期间直接读取/生成文件，任务表只保存方向、行数、状态、时间和失败摘要，不保存文件二进制，也不复用独立文件上传能力。

| 动作 | 方法 | URL | 权限 |
| --- | --- | --- | --- |
| 下载模板 | GET | `/api/import-export/jobs/templates/{jobType}` | `business:import-export:create` |
| 导入任务 | POST multipart/form-data | `/api/import-export/jobs/import/{jobType}` | `business:import-export:create` |
| 导出文件 | GET | `/api/import-export/jobs/export/{jobType}` | `business:import-export:read` |
| 任务状态 | GET | `/api/import-export/jobs/{id}` | `business:import-export:read` |

当前 `jobType`：`1 = Goods`。商品导入模板为 UTF-8 BOM CSV，表头固定为 `Name,Code,GoodsTypeId,Spec,Brand,Origin,TaxRate,IsOnSale,Remark`；`GoodsTypeId` 必须是现存分类 UUID，税率使用 0 至 100 的百分比，`IsOnSale` 仅接受 `true` 或 `false`。服务先完成全文件格式、分类、名称和编码（含跨行及数据库既有值）校验；任一行失败则不写入任何商品行，并在任务状态中返回错误摘要。单个 CSV 限制为 2 MiB；文件存储与独立上传仍由后续文件上传能力负责。

导出响应为 `text/csv`，并在 `X-Import-Export-Job-Id` 响应头返回可查询的任务主键。CSV 字段会对逗号、双引号和换行进行标准转义。

## 文件上传

文件上传采用非公开磁盘目录和元数据表，不能通过静态路径直接访问。上传时同时验证扩展名、客户端 MIME 类型和文件签名，仅支持 PDF、PNG、JPEG，单文件不超过 10 MiB；服务端生成随机存储键，不使用用户给出的路径或文件名作为实际路径。文件下载仅限创建人，并把其他用户、丢失文件和不存在主键统一表现为未找到。

| 动作 | 方法 | URL | 权限 |
| --- | --- | --- | --- |
| 上传已验证文件 | POST multipart/form-data | `/api/files` | `business:file:create` |
| 下载本人文件 | GET | `/api/files/{id}/download` | `business:file:read` |

上传 multipart 字段固定为 `file`。成功响应的 `data.downloadUrl` 是受保护 API 地址，不是物理存储路径；后续检测报告等模块可保存该地址及文件名/大小快照。

## 打印

```mermaid
flowchart TD
  A["用户点击打印"] --> B["选择打印模板"]
  B --> C["按业务对象请求打印数据"]
  C --> D["组装模板数据"]
  D --> E["打印预览"]
  E --> F["确认打印"]
```

SkyRoc 当前以模板主表保存设计器 JSON，并以模板字段表保存可绑定字段路径；后端只提供稳定业务数据快照，不负责 HTML 或打印机渲染。预览数据接口是只读的；正式打印完成后，订单、入库单和出库单必须显式确认，才会把已有 `printStatus` 从未打印更新为已打印。

模板接口：

| 动作 | 方法 | URL |
| --- | --- | --- |
| 按编码取模板 | GET | `/api/print-templates/by-code/{templateCode}` |
| 模板分页 | GET | `/api/print-templates?pageNumber=1&pageSize=20` |
| 新增模板 | POST | `/api/print-templates` |
| 修改模板 | PUT | `/api/print-templates` |
| 删除模板 | DELETE | `/api/print-templates/{id}` |

业务打印接口：

| 对象 | 方法 | URL |
| --- | --- | --- |
| 销售订单/客户配送单 | GET | `/api/print-data/1?ids={id}` |
| 采购单据 | GET | `/api/print-data/2?ids={id}` |
| 入库单 | GET | `/api/print-data/3?ids={id}` |
| 出库单 | GET | `/api/print-data/4?ids={id}` |
| 客户结款凭证 | GET | `/api/print-data/5?ids={id}` |
| 供应商结算 | GET | `/api/print-data/6?ids={id}` |
| 确认订单/入库/出库已打印 | POST | `/api/print-data/{businessType}/confirm` |

`businessType` 固定为 `1 = 销售订单`、`2 = 采购单`、`3 = 入库单`、`4 = 出库单`、`5 = 客户结款`、`6 = 供应商结算`。单次最多请求 100 个不重复 UUID；打印数据按请求 ID 顺序返回。模板维护需要 `system:print-template:*`，业务数据读取需要 `business:print:read`，正式打印确认需要 `business:print:update`。

配送单打印分组属于旧系统辅助配置，当前未纳入 P4-08 范围；前端可按本次 API 返回的单据集合自行分组。

## 系统支撑与审计

运营管理使用 `/api/system-settings` 维护服务时段、小程序下单开关和分拣权重；通知公告使用 `/api/notices` 维护草稿、发布和撤回；系统审计使用 `/api/logs/operations` 与 `/api/logs/logins` 分页查询。服务时段不允许跨日，提前下单天数范围为 0–30，分拣权重必须为非负且最多四位小数。

系统会为通过认证与授权的关键写请求记录操作审计，日志只包含已脱敏的请求摘要、HTTP 结果、耗时和操作人；密码、令牌、密钥以及响应正文均不入库。每次登录成功或失败都会生成登录日志，便于安全排查而不暴露认证凭据。

## 表头配置

> 已移出重写需求范围：P4-07 只保留安全文件上传，不再实现用户级表头配置保存/读取。以下为旧系统参考，除非产品重新确认需求，否则不迁移对应后端接口。

旧项目通过 `src/utils/headerColumnsSetting.js` 维护表格字段配置。

| 动作 | 方法 | URL |
| --- | --- | --- |
| 当前表头配置 | GET | `/business/table/setting/current` |
| 保存表头配置 | POST | `/business/table/setting` |

旧项目 React 重写建议（仅在恢复需求时参考）：

- 每个列表定义稳定的 `tableKey`。
- 默认列配置写在前端。
- 后端配置只覆盖显示/隐藏/顺序。
- 禁止把业务字段名写死在表格组件内部。

## 后续必须补齐

- 后端 OpenAPI/Swagger 请求体和响应体。
- 导入导出 `jobType` 全量枚举。
- 状态流转的后端校验规则。
- 金额、数量、单位换算精度。
- 反审核、作废、删除是否产生冲销记录。
