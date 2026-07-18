/** 销售订单相关枚举 */

/**
 * 销售订单业务状态
 *
 * - -1: 待审核
 * - 1: 待分拣
 * - 2: 分拣中
 * - 3: 分拣完成
 * - 4: 配送中
 * - 5: 已签收
 * - 6: 审核已驳回
 */
export enum SaleOrderStatus {
  /** 待审核 */
  PENDING_AUDIT = -1,
  /** 待分拣 */
  SORTING_PENDING = 1,
  /** 分拣中 */
  SORTING = 2,
  /** 分拣完成 */
  SORTING_COMPLETED = 3,
  /** 配送中 */
  DELIVERING = 4,
  /** 已签收 */
  SIGNED = 5,
  /** 审核已驳回 */
  REJECTED = 6
}

export type SaleOrderStatusValue =
  | SaleOrderStatus.PENDING_AUDIT
  | SaleOrderStatus.SORTING_PENDING
  | SaleOrderStatus.SORTING
  | SaleOrderStatus.SORTING_COMPLETED
  | SaleOrderStatus.DELIVERING
  | SaleOrderStatus.SIGNED
  | SaleOrderStatus.REJECTED;

/**
 * 回单状态
 *
 * - 0: 未回单
 * - 1: 已回单
 */
export enum OrderReturnStatus {
  /** 未回单 */
  NOT_RETURNED = 0,
  /** 已回单 */
  RETURNED = 1
}

export type OrderReturnStatusValue = OrderReturnStatus.NOT_RETURNED | OrderReturnStatus.RETURNED;

/**
 * 打印状态
 *
 * - 0: 未打印
 * - 1: 已打印
 */
export enum OrderPrintStatus {
  /** 未打印 */
  NOT_PRINTED = 0,
  /** 已打印 */
  PRINTED = 1
}

export type OrderPrintStatusValue = OrderPrintStatus.NOT_PRINTED | OrderPrintStatus.PRINTED;

/**
 * 出库生成状态
 *
 * - 0: 未生成
 * - 1: 部分生成
 * - 2: 已生成
 */
export enum OrderOutStorageStatus {
  /** 未生成 */
  NOT_GENERATED = 0,
  /** 部分生成 */
  PARTIALLY_GENERATED = 1,
  /** 已生成 */
  GENERATED = 2
}

export type OrderOutStorageStatusValue =
  | OrderOutStorageStatus.NOT_GENERATED
  | OrderOutStorageStatus.PARTIALLY_GENERATED
  | OrderOutStorageStatus.GENERATED;

/**
 * 列表日期筛选字段
 *
 * - 0: 下单日期
 * - 1: 收货日期
 * - 2: 出库日期
 */
export enum OrderDateType {
  /** 下单日期 */
  ORDER_DATE = 0,
  /** 收货日期 */
  RECEIVE_DATE = 1,
  /** 出库日期 */
  OUT_DATE = 2
}

export type OrderDateTypeValue = OrderDateType.ORDER_DATE | OrderDateType.RECEIVE_DATE | OrderDateType.OUT_DATE;

/**
 * 订单商品客户验收状态
 *
 * - 0: 待验收
 * - 1: 验收通过
 * - 2: 验收拒绝
 */
export enum OrderCustomerCheckStatus {
  /** 待验收 */
  PENDING = 0,
  /** 验收通过 */
  ACCEPTED = 1,
  /** 验收拒绝 */
  REJECTED = 2
}

export type OrderCustomerCheckStatusValue =
  | OrderCustomerCheckStatus.PENDING
  | OrderCustomerCheckStatus.ACCEPTED
  | OrderCustomerCheckStatus.REJECTED;

/**
 * 订单审核轨迹动作
 *
 * - 0: 提交
 * - 1: 通过
 * - 2: 驳回
 * - 3: 重新提交
 */
export enum OrderAuditAction {
  /** 提交 */
  SUBMIT = 0,
  /** 通过 */
  APPROVE = 1,
  /** 驳回 */
  REJECT = 2,
  /** 重新提交 */
  RESUBMIT = 3
}

export type OrderAuditActionValue =
  | OrderAuditAction.SUBMIT
  | OrderAuditAction.APPROVE
  | OrderAuditAction.REJECT
  | OrderAuditAction.RESUBMIT;
