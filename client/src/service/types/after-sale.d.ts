declare namespace Api {
  namespace AfterSale {
    /** 售后单业务状态 */
    type AfterStatus = import('../enums').AfterSaleStatusValue;

    /** 售后商品申请类型 */
    type AfterSaleType = import('../enums').AfterSaleTypeValue;

    /** 售后处理方式 */
    type HandleType = import('../enums').AfterSaleHandleTypeValue;

    /** 售后原因分类 */
    type ReasonType = import('../enums').AfterSaleReasonTypeValue;

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
      reasonType: ReasonType;
      refundAmount: number;
      remark: string | null;
      saleOrderDetailId: string | null;
      supplierId: string | null;
      supplierName: string | null;
      unitPrice: number;
    }>;

    /** 售后分页列表使用的商品处理摘要 */
    type ListGoods = Pick<Goods, 'afterSaleType' | 'handleType' | 'refundAmount'>;

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

    /** 售后分页列表项，仅包含列表展示和操作判断需要的数据 */
    type ListItem = Pick<
      Entity,
      | 'afterSaleNo'
      | 'afterStatus'
      | 'contactName'
      | 'contactPhone'
      | 'createTime'
      | 'customerName'
      | 'id'
      | 'orderPrice'
      | 'saleOrderId'
      | 'saleOrderNo'
      | 'settlementPrice'
      | 'totalRefundAmount'
    > & {
      goods: ListGoods[];
      hasPickupTasks: boolean;
      latestAuditAction: AuditAction | null;
    };

    type AllEntity = Pick<Entity, 'afterSaleNo' | 'customerName' | 'id'>;

    /** 售后商品编辑行，关联来源订单时由后端固定商品、单位和单价快照。 */
    type GoodsPayload = {
      actualRefundQuantity: number;
      afterSaleType: AfterSaleType;
      goodsId?: string | null;
      goodsUnitId?: string | null;
      handleType: HandleType;
      reasonType: ReasonType;
      remark?: string | null;
      saleOrderDetailId?: string | null;
      unitPrice?: number | null;
    };

    /** 基于销售订单创建的售后草稿。 */
    type CreatePayload = {
      contactName?: string | null;
      contactPhone?: string | null;
      goods: GoodsPayload[];
      pickupAddress?: string | null;
      remark?: string | null;
      saleOrderId: string;
      source: string;
    };

    /** 编辑售后草稿，来源订单和客户创建后不可变更。 */
    type UpdatePayload = Omit<CreatePayload, 'saleOrderId' | 'source'> & {
      id: string;
    };

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

    type List = Common.PaginatingQueryRecord<ListItem>;

    type Payload = ActionParams | CreatePayload | UpdatePayload | Record<string, unknown>;

    type Result = unknown;
  }
}
