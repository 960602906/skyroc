# 主业务流程总图

## 用途

本文档用于给后续 AI 识别整体业务，并据此推导数据库表。它把商品、客户、订单、采购、库存、配送、售后、财务、溯源、报表、系统权限串成一条主业务链路。

原则：

- 基础资料先建表，交易单据再建表。
- 所有业务单据采用“主表 + 明细表 + 状态字段 + 审计字段”。
- 商品、客户、供应商、仓库、员工等核心资料在单据明细里保留必要快照，避免历史单据受基础资料改名影响。
- 金额、数量、单位换算、批次库存、结算状态必须独立建模，不要只依赖页面展示字段。

## 主业务流程图

```mermaid
flowchart LR
  subgraph BASE["基础资料"]
    G1["商品分类 goods_type"]
    G2["商品档案 goods"]
    G3["商品单位 goods_unit"]
    C1["客户/公司 customer/company"]
    C2["客户标签 customer_tag"]
    S1["供应商 supplier"]
    P1["采购员 purchaser"]
    W1["仓库 ware"]
    Q1["报价单 quotation"]
    Q2["客户协议价 customer_protocol"]
    R1["采购规则 purchase_rule"]
  end

  subgraph ORDER["订单中心"]
    O1["客户下单/后台建单 order"]
    O2{"订单审核"}
    O3["订单商品明细 order_goods_detail"]
    O4["按商品生成采购计划"]
    O5["按订单生成销售出库"]
    O6["订单签收/回单"]
  end

  subgraph PURCHASE["采购中心"]
    PP1["采购计划 purchase_plan"]
    PP2["合并/拆分计划"]
    PO1["采购单 purchase_order"]
    PO2["采购单明细 purchase_order_detail"]
  end

  subgraph STORAGE["库存中心"]
    SI1["采购入库 stock_in_purchase"]
    SI2["其他入库 stock_in_other"]
    SI3["销售退货入库 stock_in_after_sale"]
    SO1["销售出库 stock_out_sale"]
    SO2["采购退货出库 stock_out_purchase_return"]
    SO3["其他出库 stock_out_other"]
    ST1["库存批次 stock_batch"]
    ST2["库存流水 stock_ledger"]
    ST3["库存盘点 stocktaking"]
  end

  subgraph DELIVERY["配送中心"]
    D1["配送任务 delivery_task"]
    D2["司机/承运商 driver/carrier"]
    D3["配送路线 route"]
    D4["配送异常 delivery_exception"]
  end

  subgraph AFTER["售后中心"]
    A1["售后单 after_sale"]
    A2["售后商品 after_sale_goods"]
    A3["取货任务 pickup_task"]
  end

  subgraph FINANCE["财务中心"]
    F1["客户账单 customer_bill"]
    F2["客户结款凭证 customer_settlement"]
    F3["供应商待结单据 supplier_pending"]
    F4["供应商结算 supplier_settlement"]
  end

  subgraph TRACE["溯源中心"]
    T1["检测报告 inspection_report"]
    T2["报告商品 inspection_goods"]
    T3["溯源记录 trace_record"]
    T4["二维码详情 trace_qr"]
    T5["外部报送日志 api_push_log"]
  end

  subgraph SYSTEM["系统支撑"]
    U1["用户/员工 user/employee"]
    U2["角色/菜单/权限 role/menu/permission"]
    U3["部门 dept"]
    U4["打印模板 print_template"]
    U5["操作/登录日志 system_log"]
  end

  G1 --> G2
  G2 --> G3
  G2 --> Q1
  C1 --> C2
  C1 --> Q1
  C1 --> Q2
  S1 --> R1
  G2 --> R1
  W1 --> O1
  Q1 --> O1
  Q2 --> O1
  C1 --> O1
  O1 --> O3
  O1 --> O2
  O2 -->|"驳回"| O1
  O2 -->|"通过"| O4
  O2 -->|"通过"| O5
  O4 --> PP1
  PP1 --> PP2
  PP2 --> PO1
  PO1 --> PO2
  PO1 --> SI1
  SI1 --> ST1
  SI1 --> ST2
  SI1 --> F3
  O5 --> SO1
  ST1 --> SO1
  SO1 --> ST2
  SO1 --> D1
  D2 --> D1
  D3 --> D1
  D1 --> D4
  D1 --> O6
  O6 --> F1
  F1 --> F2
  F3 --> F4
  O6 --> A1
  A1 --> A2
  A2 --> A3
  A3 --> SI3
  A1 --> F1
  SI1 --> T1
  T1 --> T2
  T2 --> T3
  O3 --> T3
  T3 --> T4
  T3 --> T5
  SI2 --> ST1
  SO2 --> ST2
  SO3 --> ST2
  ST3 --> ST2
  U1 --> O1
  U1 --> PO1
  U1 --> D1
  U2 --> U1
  U3 --> U1
  U4 --> O1
  U4 --> PO1
  U4 --> SI1
  U4 --> SO1
```

## 主表关系图

```mermaid
erDiagram
  SYS_USER ||--o{ SYS_USER_ROLE : has
  SYS_ROLE ||--o{ SYS_USER_ROLE : binds
  SYS_ROLE ||--o{ SYS_ROLE_MENU : grants
  SYS_MENU ||--o{ SYS_ROLE_MENU : belongs_to
  SYS_DEPT ||--o{ BUSINESS_EMPLOYEE : contains

  GOODS_TYPE ||--o{ GOODS : classifies
  GOODS ||--o{ GOODS_UNIT : has_units
  GOODS ||--o{ QUOTATION_GOODS : priced_by
  QUOTATION ||--o{ QUOTATION_GOODS : contains
  CUSTOMER ||--o{ CUSTOMER_QUOTATION : binds
  QUOTATION ||--o{ CUSTOMER_QUOTATION : binds
  CUSTOMER ||--o{ CUSTOMER_PROTOCOL : has
  CUSTOMER_PROTOCOL ||--o{ CUSTOMER_PROTOCOL_GOODS : contains
  GOODS ||--o{ CUSTOMER_PROTOCOL_GOODS : uses

  COMPANY ||--o{ CUSTOMER : owns
  CUSTOMER_TAG ||--o{ CUSTOMER_TAG_REL : binds
  CUSTOMER ||--o{ CUSTOMER_TAG_REL : tagged
  COMPANY ||--o{ CUSTOMER_SUB_ACCOUNT : has
  CUSTOMER ||--o{ CUSTOMER_SUB_ACCOUNT : authorizes

  CUSTOMER ||--o{ SALE_ORDER : places
  SALE_ORDER ||--o{ SALE_ORDER_DETAIL : contains
  GOODS ||--o{ SALE_ORDER_DETAIL : sold_as

  SALE_ORDER ||--o{ PURCHASE_PLAN : generates
  PURCHASE_PLAN ||--o{ PURCHASE_ORDER_DETAIL : sourced_to
  SUPPLIER ||--o{ PURCHASE_ORDER : supplies
  PURCHASER ||--o{ PURCHASE_ORDER : handles
  PURCHASE_ORDER ||--o{ PURCHASE_ORDER_DETAIL : contains
  GOODS ||--o{ PURCHASE_ORDER_DETAIL : purchased_as

  WARE ||--o{ STOCK_IN_ORDER : receives
  WARE ||--o{ STOCK_OUT_ORDER : ships
  STOCK_IN_ORDER ||--o{ STOCK_IN_DETAIL : contains
  STOCK_OUT_ORDER ||--o{ STOCK_OUT_DETAIL : contains
  GOODS ||--o{ STOCK_BATCH : batches
  WARE ||--o{ STOCK_BATCH : stores
  STOCK_BATCH ||--o{ STOCK_LEDGER : changes
  STOCK_IN_DETAIL ||--o{ STOCK_LEDGER : increases
  STOCK_OUT_DETAIL ||--o{ STOCK_LEDGER : decreases

  STOCK_OUT_ORDER ||--o{ DELIVERY_TASK : creates
  DRIVER ||--o{ DELIVERY_TASK : assigned_to
  CARRIER ||--o{ DRIVER : owns
  ROUTE ||--o{ DELIVERY_TASK : plans
  DELIVERY_TASK ||--o{ DELIVERY_EXCEPTION : reports

  SALE_ORDER ||--o{ AFTER_SALE : has
  AFTER_SALE ||--o{ AFTER_SALE_GOODS : contains
  AFTER_SALE_GOODS ||--o{ PICKUP_TASK : creates
  PICKUP_TASK ||--o{ STOCK_IN_ORDER : returns_to_stock

  CUSTOMER ||--o{ CUSTOMER_BILL : settles
  CUSTOMER_BILL ||--o{ CUSTOMER_SETTLEMENT_DETAIL : paid_by
  CUSTOMER_SETTLEMENT ||--o{ CUSTOMER_SETTLEMENT_DETAIL : contains
  SUPPLIER ||--o{ SUPPLIER_SETTLEMENT : settles
  SUPPLIER_SETTLEMENT ||--o{ SUPPLIER_SETTLEMENT_DETAIL : contains
  STOCK_IN_ORDER ||--o{ SUPPLIER_SETTLEMENT_DETAIL : source

  STOCK_IN_ORDER ||--o{ INSPECTION_REPORT : inspected_by
  INSPECTION_REPORT ||--o{ INSPECTION_GOODS : contains
  INSPECTION_GOODS ||--o{ TRACE_RECORD : creates
  SALE_ORDER_DETAIL ||--o{ TRACE_RECORD : traces_to
  TRACE_RECORD ||--o{ API_PUSH_LOG : pushed_by
```

## 流程节点到数据库表

| 流程节点 | 触发动作 | 建议主表 | 建议明细/关系表 | 关键外键 |
| --- | --- | --- | --- | --- |
| 商品建档 | 新增/编辑商品 | `goods` | `goods_unit`、`goods_image`、`goods_supplier_rel` | `goods_type_id` |
| 报价维护 | 新增报价、绑定商品 | `quotation` | `quotation_goods`、`customer_quotation` | `goods_id`、`customer_id` |
| 协议价 | 客户协议价配置 | `customer_protocol` | `customer_protocol_goods`、`customer_protocol_customer` | `customer_id`、`goods_id` |
| 客户资料 | 新增公司、客户、子账号 | `company`、`customer` | `customer_tag_rel`、`customer_sub_account` | `company_id`、`tag_id` |
| 采购规则 | 配置客户/商品/供应商规则 | `purchase_rule` | `purchase_rule_goods`、`purchase_rule_customer` | `goods_id`、`supplier_id`、`customer_id` |
| 销售订单 | 客户下单、后台建单 | `sale_order` | `sale_order_detail` | `customer_id`、`quotation_id`、`ware_id` |
| 订单审核 | 审核通过/驳回/重提 | `sale_order` | `order_audit_log` | `order_id`、`audit_user_id` |
| 采购计划 | 订单商品生成计划 | `purchase_plan` | `purchase_plan_order_rel` | `order_id`、`goods_id`、`supplier_id` |
| 采购单 | 计划生成或手工新增采购单 | `purchase_order` | `purchase_order_detail` | `supplier_id`、`purchaser_id` |
| 采购入库 | 采购到货入库 | `stock_in_order` | `stock_in_detail`、`stock_batch`、`stock_ledger` | `purchase_order_id`、`ware_id` |
| 销售出库 | 订单生成销售出库 | `stock_out_order` | `stock_out_detail`、`stock_ledger` | `sale_order_id`、`ware_id`、`batch_id` |
| 其他入库 | 手工库存增加 | `stock_in_order` | `stock_in_detail`、`stock_ledger` | `ware_id`、`goods_id` |
| 采购退货出库 | 供应商退货 | `stock_out_order` | `stock_out_detail`、`stock_ledger` | `supplier_id`、`batch_id` |
| 其他出库 | 手工库存扣减 | `stock_out_order` | `stock_out_detail`、`stock_ledger` | `ware_id`、`goods_id` |
| 库存盘点 | 盘盈/盘亏调整 | `stocktaking_order` | `stocktaking_detail`、`stock_ledger` | `ware_id`、`batch_id` |
| 配送任务 | 销售出库后分配配送 | `delivery_task` | `delivery_task_order_rel` | `stock_out_order_id`、`driver_id`、`route_id` |
| 签收回单 | 客户签收、验收 | `sale_order` | `order_receipt`、`order_check_detail` | `order_id` |
| 售后单 | 退款、退货、补货、换货 | `after_sale` | `after_sale_goods` | `order_id`、`customer_id` |
| 取货任务 | 售后退货取货 | `pickup_task` | `pickup_task_goods` | `after_sale_id`、`driver_id` |
| 客户账单 | 签收和售后后生成应收 | `customer_bill` | `customer_bill_detail` | `customer_id`、`order_id`、`after_sale_id` |
| 客户结款 | 客户付款/优惠/流水 | `customer_settlement` | `customer_settlement_detail` | `customer_bill_id` |
| 供应商结算 | 采购入库/采购退货结算 | `supplier_settlement` | `supplier_settlement_detail` | `supplier_id`、`stock_in_order_id` |
| 检测报告 | 入库商品关联检测 | `inspection_report` | `inspection_goods`、`inspection_attachment` | `stock_in_order_id`、`goods_id` |
| 溯源记录 | 订单商品关联报告 | `trace_record` | `trace_record_goods`、`api_push_log` | `order_detail_id`、`inspection_report_id` |
| 报表查询 | 销售、库存、采购、售后汇总 | 通常不单独建业务表 | 可建 `report_export_job` | 来源于订单、库存、财务 |
| 打印模板 | 打印配送单、采购单、出入库单 | `print_template` | `print_template_field` | `template_code` |
| 系统权限 | 菜单、角色、用户权限 | `sys_user`、`sys_role`、`sys_menu` | `sys_user_role`、`sys_role_menu` | `user_id`、`role_id`、`menu_id` |
| 操作日志 | 登录、操作审计 | `system_log` | 无或 `system_log_detail` | `user_id`、`module` |

## 建表优先级

```mermaid
flowchart TD
  A["第一阶段：系统权限和基础资料"] --> B["用户、角色、菜单、部门"]
  A --> C["商品、客户、供应商、仓库、报价"]
  B --> D["第二阶段：订单主链路"]
  C --> D
  D --> E["销售订单和订单明细"]
  E --> F["采购计划和采购单"]
  F --> G["库存入库、出库、批次、流水"]
  G --> H["配送任务和签收回单"]
  H --> I["第三阶段：售后和财务"]
  I --> J["售后单、取货任务"]
  I --> K["客户账单、客户结款、供应商结算"]
  K --> L["第四阶段：溯源、报表、打印和日志"]
```

## 核心状态字段

| 表 | 字段 | 建议枚举 |
| --- | --- | --- |
| `sale_order` | `order_status` | `pending_audit`、`rejected`、`sorting_pending`、`sorting`、`sorting_done`、`delivering`、`signed` |
| `sale_order` | `return_status` | `not_returned`、`returned` |
| `purchase_plan` | `purchase_status` | `unpublished`、`generated`、`part_generated` |
| `purchase_order` | `status` | `draft`、`completed`、`cancelled` |
| `stock_in_order` | `status` | `draft`、`pending_audit`、`audited`、`reversed`、`deleted` |
| `stock_out_order` | `status` | `draft`、`pending_audit`、`audited`、`reversed`、`deleted` |
| `delivery_task` | `delivery_status` | `pending_assign`、`assigned`、`delivering`、`exception`、`signed` |
| `after_sale` | `after_status` | `draft`、`pending_audit`、`return_pending`、`refund_pending`、`done` |
| `customer_settlement` | `settlement_status` | `pending`、`partial`、`paid`、`voided` |
| `supplier_settlement` | `settlement_status` | `pending`、`partial`、`paid`、`voided` |
| `inspection_report` | `status` | `draft`、`effective`、`voided` |
| `api_push_log` | `push_status` | `pending`、`success`、`failed` |

## AI 建表提示

- 单据类表统一字段：`id`、`order_no`、`status`、`remark`、`created_by`、`created_at`、`updated_by`、`updated_at`、`deleted_flag`。
- 明细类表统一字段：`id`、`parent_id`、`goods_id`、`goods_name_snapshot`、`goods_code_snapshot`、`unit_id`、`unit_name_snapshot`、`quantity`、`base_quantity`、`unit_price`、`total_price`。
- 库存不要只存当前数，必须有 `stock_batch` 当前批次数和 `stock_ledger` 流水。
- 金额字段使用 decimal，不使用 float；数量字段也使用 decimal。
- 报价、协议价、订单、采购、库存、结算都要保留价格快照。
- 反审核不要直接删除历史，建议新增反向流水或记录 `reversed_from_id`。
- 报表优先基于业务表查询生成，除非性能要求再设计汇总表。

