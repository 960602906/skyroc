/** Purchase enums */

/**
 * Purchase pattern
 *
 * - 1: supplier direct supply
 * - 2: market self purchase
 */
export enum PurchasePattern {
  /** Supplier direct supply */
  SUPPLIER_DIRECT = 1,
  /** Market self purchase */
  MARKET_SELF_PURCHASE = 2
}

export type PurchasePatternValue = PurchasePattern.SUPPLIER_DIRECT | PurchasePattern.MARKET_SELF_PURCHASE;

/** 采购计划生成采购单的进度。 */
export enum PurchasePlanStatus {
  /** 尚未生成采购单。 */
  UNPUBLISHED = 1,
  /** 已全部生成采购单。 */
  GENERATED = 2,
  /** 已部分生成采购单。 */
  PARTIALLY_GENERATED = 3
}

export type PurchasePlanStatusValue =
  | PurchasePlanStatus.UNPUBLISHED
  | PurchasePlanStatus.GENERATED
  | PurchasePlanStatus.PARTIALLY_GENERATED;

/**
 * 采购单执行状态，控制采购单是否仍可编辑以及能否被后续入库流程引用。
 *
 * - 1: draft - 未完成草稿,可继续维护
 * - 2: completed - 已完成,可供采购入库引用
 * - 3: cancelled - 已取消,不再执行
 */
export enum PurchaseOrderStatus {
  /** 未完成草稿,可继续维护采购商品、价格和预计到货时间。 */
  DRAFT = 1,
  /** 已完成,采购内容已确认并可供采购入库引用。 */
  COMPLETED = 2,
  /** 已取消,不再执行且不得生成新的采购入库单。 */
  CANCELLED = 3
}

export type PurchaseOrderStatusValue =
  | PurchaseOrderStatus.DRAFT
  | PurchaseOrderStatus.COMPLETED
  | PurchaseOrderStatus.CANCELLED;
