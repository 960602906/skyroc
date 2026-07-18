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
