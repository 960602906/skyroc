declare namespace Api {
  namespace Order {
    /** 销售订单业务状态 */
    type OrderStatus = import('../enums').SaleOrderStatusValue;

    /** 回单状态 */
    type ReturnStatus = import('../enums').OrderReturnStatusValue;

    /** 打印状态 */
    type PrintStatus = import('../enums').OrderPrintStatusValue;

    /** 出库生成状态 */
    type OutStorageStatus = import('../enums').OrderOutStorageStatusValue;

    /** 列表日期筛选字段 */
    type DateType = import('../enums').OrderDateTypeValue;

    /** 订单商品客户验收状态 */
    type CustomerCheckStatus = import('../enums').OrderCustomerCheckStatusValue;

    /** 订单审核轨迹动作 */
    type AuditAction = import('../enums').OrderAuditActionValue;

    /** 销售订单商品明细 */
    type Detail = Common.CommonRecord<{
      baseQuantity: number;
      baseUnitId: string | null;
      baseUnitName: string | null;
      customerCheckBaseQuantity: number | null;
      customerCheckPrice: number | null;
      customerCheckStatus: CustomerCheckStatus;
      fixedGoodsUnitId: string | null;
      fixedGoodsUnitName: string | null;
      fixedPrice: number;
      goodsCode: string;
      goodsDescription: string | null;
      goodsId: string;
      goodsImage: string | null;
      goodsName: string;
      goodsTypeName: string | null;
      goodsUnitId: string;
      goodsUnitName: string;
      hasPurchasePlan: boolean;
      innerRemark: string | null;
      quantity: number;
      remark: string | null;
      saleOrderId: string;
      totalPrice: number;
      unitConversion: number;
    }>;

    /** 订单审核记录 */
    type AuditLog = Common.CommonRecord<{
      action: AuditAction;
      auditTime: string;
      auditUserId: string | null;
      auditUserName: string;
      currentStatus: OrderStatus;
      previousStatus: OrderStatus;
      remark: string | null;
      saleOrderId: string;
    }>;

    /** 销售订单实体 */
    type Entity = Common.CommonRecord<{
      auditLogs?: AuditLog[] | null;
      contactName: string | null;
      contactPhone: string | null;
      customerCode: string;
      customerId: string;
      customerName: string;
      deliveryAddress: string | null;
      details?: Detail[] | null;
      hasOutSale: boolean;
      hasPurchasePlan: boolean;
      innerRemark: string | null;
      orderDate: string;
      orderNo: string;
      orderPrice: number;
      orderStatus: OrderStatus;
      outDate: string | null;
      outStorageStatus: OutStorageStatus;
      printStatus: PrintStatus;
      quotationId: string | null;
      receiveDate: string | null;
      remark: string | null;
      returnStatus: ReturnStatus;
      settlementPrice: number;
      updateStatus: boolean;
      wareId: string | null;
      wareName: string | null;
    }>;

    type AllEntity = Pick<Entity, 'customerName' | 'id' | 'orderNo'>;

    /** 审核操作请求体 */
    type AuditParams = {
      remark?: string | null;
    };

    /** 列表查询参数（含表单专用 dateRange） */
    type SearchParams = CommonType.RecordNullable<
      Api.Common.CommonSearchParams & {
        customerId?: string | null;
        customerTagIds?: string[] | null;
        dateEnd?: string | null;
        /** 表单专用日期范围，请求前会拆成 dateStart/dateEnd */
        dateRange?: [unknown, unknown] | null;
        dateStart?: string | null;
        dateType?: DateType | null;
        goodsIds?: string[] | null;
        goodsKey?: string | null;
        goodsTypeIds?: string[] | null;
        hasOutSale?: boolean | null;
        hasPurchasePlan?: boolean | null;
        keyword?: string | null;
        orderStatus?: OrderStatus | null;
        returnStatus?: ReturnStatus | null;
        status?: Api.Common.EnableStatus | null;
        supplierId?: string | null;
        updateStatus?: boolean | null;
      }
    >;

    type List = Common.PaginatingQueryRecord<Entity>;
  }
}
