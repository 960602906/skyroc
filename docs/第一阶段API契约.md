# 第一阶段 API 契约

> 核对日期：2026-07-02  
> 契约来源：当前 ASP.NET Core 控制器、Swagger 和 `SkyRoc/SkyRoc.http`。本文记录当前后端真实路由；`docs/business-flows/` 中的 `/business/*`、`/system/*` 旧 Vue 路由仅用于业务溯源，不代表本仓库已暴露同名接口。

## 访问与响应约定

- 开发地址：`http://localhost:5293`；Swagger UI：`http://localhost:5293/`；Swagger JSON：`/swagger/v1/swagger.json`。
- 除健康检查、登录和刷新令牌外，接口均要求 `Authorization: Bearer <AccessToken>`。
- 统一响应为 `{ "code": 200, "msg": "操作成功", "data": ... }`；业务响应码使用 `ResponseCode`，HTTP 认证失败和权限不足分别为 401、403。
- 分页请求使用 `current`、`size`，分页数据位于 `data.records`，并返回 `data.total`、`data.current`、`data.size`。
- 时间字段按 `yyyy-MM-dd HH:mm:ss` 传输；状态参数 `Enable=1`、`Disable=2`。

## 公共基础资料动作

下表动作由 15 个基础资料控制器统一提供。`{resource}` 替换为各资源控制器名，`{id}` 为 GUID。

| 动作 | 方法与路由 | 权限动作 |
| --- | --- | --- |
| 分页 | `GET /api/{resource}/list` | `read` |
| 全部 | `GET /api/{resource}` | `read` |
| 详情 | `GET /api/{resource}/{id}` | `read` |
| 新增 | `POST /api/{resource}` | `create` |
| 修改 | `PUT /api/{resource}` | `update` |
| 删除 | `DELETE /api/{resource}/{id}` | `delete` |
| 批量删除 | `DELETE /api/{resource}/batchDelete` | `delete` |
| 启用/禁用 | `PATCH /api/{resource}/{id}/status?status=Enable|Disable` | `update` |

## 第一阶段接口清单

| 模块 | 控制器/路由 | 数量 | 补充动作 |
| --- | --- | ---: | --- |
| 健康检查 | `GET /health` | 1 | 匿名 |
| 认证 | `/api/auth` | 5 | `login`、`getUserInfo`、`getRoutes`、`refresh-token`、`logout`；登录和刷新匿名 |
| 个人中心 | `/api/system/user/profile` | 3 | 查询、更新、`updatePwd` |
| 用户 | `/api/users` | 9 | 分页、全部、详情、新增、修改、删除、批量删除、`assignRoles`、`unassignRoles` |
| 角色 | `/api/roles` | 9 | 分页、全部、详情、新增、修改、删除、批量删除、`assignMenus`、`unassignRoles`（当前路由名，实际为解绑菜单） |
| 菜单 | `/api/menus` | 8 | 分页、全部、树、详情、新增、修改、删除、批量删除 |
| 菜单按钮 | `/api/menu-buttons` | 6 | 详情、新增、`batch`、修改、`replace`、删除 |
| 部门 | `/api/departments` | 8 | 树、详情、新增、修改、删除、批量删除、状态、部门用户 |
| 商品分类 | `/api/goods-types` | 9 | 公共 8 项 + `GET tree` |
| 商品档案 | `/api/goods` | 9 | 公共 8 项 + `PATCH {id}/sale-status` |
| 商品单位 | `/api/goods-units` | 9 | 公共 8 项 + `GET by-goods/{goodsId}` |
| 报价单 | `/api/quotations` | 9 | 公共 8 项 + `PATCH {id}/audit` |
| 报价商品 | `/api/quotation-goods` | 8 | 公共 8 项 |
| 客户协议价 | `/api/customer-protocols` | 8 | 公共 8 项 |
| 协议价商品 | `/api/customer-protocol-goods` | 8 | 公共 8 项 |
| 公司 | `/api/companies` | 8 | 公共 8 项 |
| 客户 | `/api/customers` | 8 | 公共 8 项 |
| 客户标签 | `/api/customer-tags` | 9 | 公共 8 项 + `GET tree` |
| 客户子账号 | `/api/customer-sub-accounts` | 8 | 公共 8 项 |
| 供应商 | `/api/suppliers` | 8 | 公共 8 项 |
| 采购员 | `/api/purchasers` | 8 | 公共 8 项 |
| 采购规则 | `/api/purchase-rules` | 8 | 公共 8 项 |
| 仓库 | `/api/wares` | 8 | 公共 8 项 |

第一阶段共记录 174 个可调用请求，均已在 `SkyRoc/SkyRoc.http` 提供示例。

## 权限编码

| 资源 | 权限前缀/独立权限 |
| --- | --- |
| 用户 | `system:user:read/create/update/delete`、`system:user:assign-roles` |
| 角色 | `system:role:read/create/update/delete`、`system:role:assign-menus` |
| 菜单 | `system:menu:read/create/update/delete` |
| 菜单按钮 | `system:menu-button:read/create/update/delete` |
| 部门 | `system:department:read/create/update/delete` |
| 商品分类、档案、单位 | `business:goods:read/create/update/delete` |
| 公司、客户、标签、子账号 | `business:customer:read/create/update/delete` |
| 报价、报价商品、协议价及商品 | `business:pricing:read/create/update/delete`；审核为 `business:pricing:audit` |
| 供应商、采购员、采购规则 | `business:purchase:read/create/update/delete` |
| 仓库 | `business:storage:read/create/update/delete` |

管理员超级权限为 `*:*:*`。Swagger 仅在受保护操作上显示 Bearer 要求，并在细粒度授权操作描述中展示所需权限码。

## 联调入口

完整请求体、查询参数和变量顺序见 [`SkyRoc.http`](../SkyRoc/SkyRoc.http)。建议按“健康检查 -> 登录 -> 用户信息 -> 路由 -> 基础资料查询 -> 新增/修改/状态/删除”的顺序执行；写操作前必须把顶部 GUID 占位变量替换为当前数据库真实值。
