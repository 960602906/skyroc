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
