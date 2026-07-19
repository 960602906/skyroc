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
