/** Storage enums */

/**
 * Stock document status
 *
 * - -1: deleted (history only, not in stock calculation)
 * - 1: draft (editable)
 * - 2: pending audit (submitted, no edit)
 * - 3: audited (stock affected)
 * - 4: reversed (audit reversed)
 */
export enum StockDocumentStatus {
  /** Deleted (history only) */
  DELETED = -1,
  /** Draft (editable) */
  DRAFT = 1,
  /** Pending audit (submitted) */
  PENDING_AUDIT = 2,
  /** Audited (stock affected) */
  AUDITED = 3,
  /** Reversed (audit reversed) */
  REVERSED = 4
}

export type StockDocumentStatusValue =
  | StockDocumentStatus.DELETED
  | StockDocumentStatus.DRAFT
  | StockDocumentStatus.PENDING_AUDIT
  | StockDocumentStatus.AUDITED
  | StockDocumentStatus.REVERSED;

/**
 * Stock in order type
 *
 * - 1: purchase stock in
 * - 2: other stock in
 * - 3: sales return stock in
 */
export enum StockInOrderType {
  /** Purchase stock in */
  PURCHASE = 1,
  /** Other stock in */
  OTHER = 2,
  /** Sales return stock in */
  SALES_RETURN = 3
}

export type StockInOrderTypeValue = StockInOrderType.PURCHASE | StockInOrderType.OTHER | StockInOrderType.SALES_RETURN;

/**
 * Stock print status
 *
 * - 0: not printed
 * - 1: printed
 */
export enum StockPrintStatus {
  /** Not printed */
  NOT_PRINTED = 0,
  /** Printed */
  PRINTED = 1
}

export type StockPrintStatusValue = StockPrintStatus.NOT_PRINTED | StockPrintStatus.PRINTED;
