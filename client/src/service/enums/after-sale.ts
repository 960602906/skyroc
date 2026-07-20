/** 售后业务相关枚举 */

/** 售后单业务状态 */
export enum AfterSaleStatus {
  /** 待提交 */
  DRAFT = 1,
  /** 待审核 */
  PENDING_AUDIT = 2,
  /** 待退货 */
  RETURN_PENDING = 3,
  /** 待退款 */
  REFUND_PENDING = 4,
  /** 已完成 */
  COMPLETED = 5
}

export type AfterSaleStatusValue =
  | AfterSaleStatus.DRAFT
  | AfterSaleStatus.PENDING_AUDIT
  | AfterSaleStatus.RETURN_PENDING
  | AfterSaleStatus.REFUND_PENDING
  | AfterSaleStatus.COMPLETED;

/** 售后商品申请类型 */
export enum AfterSaleType {
  /** 仅退款 */
  REFUND_ONLY = 1,
  /** 退货退款 */
  RETURN_AND_REFUND = 2
}

export type AfterSaleTypeValue = AfterSaleType.REFUND_ONLY | AfterSaleType.RETURN_AND_REFUND;

/** 售后原因分类。 */
export enum AfterSaleReasonType {
  LATE_DELIVERY = 1,
  MISSING_ITEM = 2,
  WRONG_ITEM = 3,
  ORDERING_ERROR = 4,
  QUANTITY_MISMATCH = 5,
  QUALITY_ISSUE = 6,
  SPECIFICATION_MISMATCH = 7,
  DRIVER_LOSS_OR_DAMAGE = 8,
  MARKET_OUT_OF_STOCK = 9,
  SYSTEM_ISSUE = 10,
  PURCHASE_ISSUE = 11,
  UNABLE_TO_DELIVER = 12,
  OTHER = 13
}

export type AfterSaleReasonTypeValue =
  | AfterSaleReasonType.LATE_DELIVERY
  | AfterSaleReasonType.MISSING_ITEM
  | AfterSaleReasonType.WRONG_ITEM
  | AfterSaleReasonType.ORDERING_ERROR
  | AfterSaleReasonType.QUANTITY_MISMATCH
  | AfterSaleReasonType.QUALITY_ISSUE
  | AfterSaleReasonType.SPECIFICATION_MISMATCH
  | AfterSaleReasonType.DRIVER_LOSS_OR_DAMAGE
  | AfterSaleReasonType.MARKET_OUT_OF_STOCK
  | AfterSaleReasonType.SYSTEM_ISSUE
  | AfterSaleReasonType.PURCHASE_ISSUE
  | AfterSaleReasonType.UNABLE_TO_DELIVER
  | AfterSaleReasonType.OTHER;

/** 售后处理方式 */
export enum AfterSaleHandleType {
  /** 商品减免 */
  GOODS_DISCOUNT = 1,
  /** 补货 */
  REPLENISHMENT = 2,
  /** 换货 */
  EXCHANGE = 3,
  /** 账单调整 */
  BILL_ADJUSTMENT = 4,
  /** 客户沟通 */
  CUSTOMER_COMMUNICATION = 5,
  /** 其他 */
  OTHER = 6
}

export type AfterSaleHandleTypeValue =
  | AfterSaleHandleType.GOODS_DISCOUNT
  | AfterSaleHandleType.REPLENISHMENT
  | AfterSaleHandleType.EXCHANGE
  | AfterSaleHandleType.BILL_ADJUSTMENT
  | AfterSaleHandleType.CUSTOMER_COMMUNICATION
  | AfterSaleHandleType.OTHER;

/** 售后审核轨迹动作 */
export enum AfterSaleAuditAction {
  /** 首次提交 */
  SUBMIT = 1,
  /** 审核通过 */
  APPROVE = 2,
  /** 审核驳回 */
  REJECT = 3,
  /** 重新提交 */
  RESUBMIT = 4,
  /** 反审核 */
  REVERSE = 5
}

export type AfterSaleAuditActionValue =
  | AfterSaleAuditAction.SUBMIT
  | AfterSaleAuditAction.APPROVE
  | AfterSaleAuditAction.REJECT
  | AfterSaleAuditAction.RESUBMIT
  | AfterSaleAuditAction.REVERSE;
