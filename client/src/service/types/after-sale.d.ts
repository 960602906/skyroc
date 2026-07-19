declare namespace Api {
  namespace AfterSale {
    /** 售后单业务状态 */
    type AfterStatus = import('../enums').AfterSaleStatusValue;

    /** 售后商品申请类型 */
    type AfterSaleType = import('../enums').AfterSaleTypeValue;

    /** 售后处理方式 */
    type HandleType = import('../enums').AfterSaleHandleTypeValue;

    /** 售后审核轨迹动作 */
    type AuditAction = import('../enums').AfterSaleAuditActionValue;

    /** 售后审核轨迹 */
    type AuditLog = Common.CommonRecord<{
      action: AuditAction;
      auditTime: string;
      auditUserId: string | null;
      auditUserName: string;
      currentStatus: AfterStatus;
      previousStatus: AfterStatus;
      remark: string | null;
    }>;

    /** 售后商品明细 */
    type Goods = Common.CommonRecord<{
      actualRefundQuantity: number;
      afterSaleType: AfterSaleType;
      baseRefundQuantity: number;
      baseUnitId: string | null;
      baseUnitName: string | null;
      conversionRate: number;
      departmentId: string | null;
      departmentName: string | null;
      goodsCode: string;
      goodsId: string;
      goodsName: string;
      goodsTypeName: string | null;
      goodsUnitId: string;
      goodsUnitName: string;
      handleType: HandleType;
      reasonType: number;
      refundAmount: number;
      remark: string | null;
      saleOrderDetailId: string | null;
      supplierId: string | null;
      supplierName: string | null;
      unitPrice: number;
    }>;

    /** 售后单实体 */
    type Entity = Common.CommonRecord<{
      afterSaleNo: string;
      afterStatus: AfterStatus;
      auditLogs?: AuditLog[] | null;
      contactName: string | null;
      contactPhone: string | null;
      customerId: string;
      customerName: string;
      goods: Goods[];
      orderPrice: number;
      pickupAddress: string | null;
      pickupTasks?: Common.CommonRecord<Record<string, unknown>>[] | null;
      remark: string | null;
      saleOrderId: string | null;
      saleOrderNo: string | null;
      settlementPrice: number;
      source: string;
      totalRefundAmount: number;
    }>;

    type AllEntity = Pick<Entity, 'afterSaleNo' | 'customerName' | 'id'>;

    /** 审核及状态流转操作请求体 */
    type ActionParams = {
      remark?: string | null;
    };

    /** 列表查询参数（含表单专用 dateRange） */
    type SearchParams = CommonType.RecordNullable<
      Api.Base.SearchParams & {
        afterSaleType?: AfterSaleType | null;
        afterStatus?: AfterStatus | null;
        customerId?: string | null;
        dateEnd?: string | null;
        /** 表单专用日期范围，请求前会拆成 dateStart/dateEnd */
        dateRange?: [unknown, unknown] | null;
        dateStart?: string | null;
        handleType?: HandleType | null;
        keyword?: string | null;
        saleOrderId?: string | null;
      }
    >;

    type List = Common.PaginatingQueryRecord<Entity>;

    type Payload = ActionParams & Record<string, unknown>;

    type Result = unknown;
  }
}
