declare namespace Api {
  namespace StockIn {
    /** 采购模式（复用采购领域定义） */
    type PurchasePattern = import('../enums').PurchasePatternValue;

    /** 单据业务状态 */
    type StockDocumentStatus = import('../enums').StockDocumentStatusValue;

    /** 入库业务类型 */
    type StockInOrderType = import('../enums').StockInOrderTypeValue;

    /** 打印状态 */
    type StockPrintStatus = import('../enums').StockPrintStatusValue;

    /**
     * 入库单主单 DTO
     *
     * 返回入库来源、仓库、业务方、审核状态及商品明细快照
     */
    type Entity = Common.CommonRecord<{
      /** 来源售后单主键；手工销售退货入库以及其他入库类型为空 */
      afterSaleId?: string | null;
      /** 最近一次审核通过时间（UTC） */
      auditTime?: string | null;
      /** 最近一次审核人的系统用户主键 */
      auditUserId?: string | null;
      /** 最近一次审核时的用户名称快照 */
      auditUserName?: string | null;
      /** 单据业务状态：草稿、待审核、已审核、已反审核或已删除 */
      businessStatus: StockDocumentStatus;
      /** 退货客户主键；仅销售退货入库时填写 */
      customerId?: string | null;
      /** 入库业务发生时的客户名称快照 */
      customerName?: string | null;
      /** 发起入库业务的部门主键 */
      departmentId?: string | null;
      /** 入库业务发生时的部门名称快照 */
      departmentName?: string | null;
      /** 入库商品明细集合 */
      details: StockInDetail[];
      /** 预计到货时间（UTC）；尚未确认时为空 */
      expectedArrivalTime?: string | null;
      /** 入库单业务编号 */
      inNo: string;
      /** 计划或实际入库时间（UTC） */
      inTime: string;
      /** 入库业务类型：采购、其他或销售退货 */
      orderType: StockInOrderType;
      /** 单据打印状态：0 未打印，1 已打印 */
      printStatus: StockPrintStatus;
      /** 来源采购单主键；仅采购入库时填写 */
      purchaseOrderId?: string | null;
      /** 采购入库采用的采购模式；其他入库和销售退货入库为空 */
      purchasePattern?: PurchasePattern | null;
      /** 负责采购到货的采购员主键；非采购入库可为空 */
      purchaserId?: string | null;
      /** 入库业务发生时的采购员名称快照 */
      purchaserName?: string | null;
      /** 入库单级业务备注 */
      remark?: string | null;
      /** 最近一次反审核完成时间（UTC） */
      reverseTime?: string | null;
      /** 最近一次反审核人的系统用户主键 */
      reverseUserId?: string | null;
      /** 最近一次反审核时的用户名称快照 */
      reverseUserName?: string | null;
      /** 供货供应商主键；采购入库时通常填写 */
      supplierId?: string | null;
      /** 入库业务发生时的供应商名称快照 */
      supplierName?: string | null;
      /** 入库金额合计，按系统业务币种计量 */
      totalAmount: number;
      /** 入库基础单位数量合计，仅用于展示 */
      totalBaseQuantity: number;
      /** 接收入库商品的仓库主键 */
      wareId: string;
      /** 单据创建时的仓库名称快照 */
      wareName: string;
    }>;

    /**
     * 入库商品明细 DTO
     *
     * 返回商品、单位、批次、数量和成本快照
     */
    type StockInDetail = Common.CommonRecord<{
      /** 按商品基础单位换算后的入库数量 */
      baseQuantity: number;
      /** 商品批次号；同仓库同商品下定位唯一库存批次 */
      batchNo: string;
      /** 入库单位换算为商品基础单位的比例 */
      conversionRate: number;
      /** 商品到期日期，仅记录自然日；无保质期或未知时为空（DateOnly 格式：yyyy-MM-dd） */
      expireDate?: string | null;
      /** 入库发生时的商品编码快照 */
      goodsCode: string;
      /** 入库商品主键 */
      goodsId: string;
      /** 入库发生时的商品名称快照 */
      goodsName: string;
      /** 入库计量单位主键 */
      goodsUnitId: string;
      /** 入库发生时的计量单位名称快照 */
      goodsUnitName: string;
      /** 来源售后取货任务主键；手工销售退货入库以及其他入库类型为空 */
      pickupTaskId?: string | null;
      /** 商品生产日期，仅记录自然日；未知时为空（DateOnly 格式：yyyy-MM-dd） */
      productDate?: string | null;
      /** 来源采购单商品明细主键；非采购入库时为空 */
      purchaseOrderDetailId?: string | null;
      /** 按入库单位计量的入库数量 */
      quantity: number;
      /** 当前入库商品行的业务备注 */
      remark?: string | null;
      /** 审核入库后对应的库存批次主键；未审核时为空 */
      stockBatchId?: string | null;
      /** 所属入库主单主键 */
      stockInOrderId: string;
      /** 入库金额，为入库数量乘以单价后的金额快照 */
      totalPrice: number;
      /** 入库单价，按系统业务币种和入库单位计量 */
      unitPrice: number;
    }>;

    /**
     * 采购入库创建请求
     *
     * 创建结果始终为可编辑草稿
     */
    type CreatePurchasePayload = {
      /** 发起入库业务的部门主键 */
      departmentId?: string | null;
      /** 采购入库商品行，至少包含一项 */
      details: CreateStockInDetailPayload[];
      /** 预计到货时间（UTC）；尚未确认时可为空 */
      expectedArrivalTime?: string | null;
      /** 计划或实际入库时间（UTC） */
      inTime: string;
      /** 来源采购单主键；用于回填供应商、采购员和采购模式并支持追溯 */
      purchaseOrderId?: string | null;
      /** 采购模式：供应商直供或市场自采 */
      purchasePattern: PurchasePattern;
      /** 负责采购到货的采购员主键 */
      purchaserId?: string | null;
      /** 入库单级业务备注 */
      remark?: string | null;
      /** 供货供应商主键；供应商直供采购入库时必填 */
      supplierId?: string | null;
      /** 接收入库商品的仓库主键 */
      wareId: string;
    };

    /**
     * 采购入库编辑请求
     *
     * 整单替换主单字段与商品行
     */
    type UpdatePurchasePayload = {
      /** 发起入库业务的部门主键 */
      departmentId?: string | null;
      /** 采购入库商品行完整集合，至少包含一项 */
      details: UpdateStockInDetailPayload[];
      /** 预计到货时间（UTC）；尚未确认时可为空 */
      expectedArrivalTime?: string | null;
      /** 待编辑的采购入库单主键 */
      id: string;
      /** 计划或实际入库时间（UTC） */
      inTime: string;
      /** 来源采购单主键；用于回填供应商、采购员和采购模式并支持追溯 */
      purchaseOrderId?: string | null;
      /** 采购模式：供应商直供或市场自采 */
      purchasePattern: PurchasePattern;
      /** 负责采购到货的采购员主键 */
      purchaserId?: string | null;
      /** 入库单级业务备注 */
      remark?: string | null;
      /** 供货供应商主键；供应商直供采购入库时必填 */
      supplierId?: string | null;
      /** 接收入库商品的仓库主键 */
      wareId: string;
    };

    /**
     * 入库商品行创建请求
     *
     * 描述入库单位、批次、数量和价格
     */
    type CreateStockInDetailPayload = {
      /** 商品到期日期，仅记录自然日；无保质期或未知时可为空（DateOnly 格式：yyyy-MM-dd） */
      expireDate?: string | null;
      /** 入库商品主键 */
      goodsId: string;
      /** 入库计量单位主键，必须属于入库商品 */
      goodsUnitId: string;
      /** 来源售后取货任务主键；仅销售退货入库使用，任务必须已完成且最多入库一次 */
      pickupTaskId?: string | null;
      /** 商品生产日期，仅记录自然日；未知时可为空（DateOnly 格式：yyyy-MM-dd） */
      productDate?: string | null;
      /** 来源采购单商品明细主键；仅采购入库回填，用于追溯到货来源 */
      purchaseOrderDetailId?: string | null;
      /** 按入库单位计量的入库数量，必须大于零 */
      quantity: number;
      /** 当前入库商品行的业务备注 */
      remark?: string | null;
      /** 入库单价，按入库单位计量，不得为负 */
      unitPrice: number;
    };

    /**
     * 入库商品行编辑请求
     *
     * 携带明细主键以便按行更新草稿入库单
     */
    type UpdateStockInDetailPayload = CreateStockInDetailPayload & {
      /** 已存在的入库商品行主键；为空表示新增商品行 */
      id?: string | null;
    };

    /**
     * 入库单审核或反审核操作请求
     *
     * 携带可选业务说明
     */
    type AuditPayload = {
      /** 审核或反审核原因说明；写入生成的库存流水备注 */
      remark?: string | null;
    };

    /**
     * 入库单分页查询参数
     *
     * 按入库类型、仓库、业务方、时间和审核状态筛选
     */
    type SearchParams = CommonType.RecordNullable<
      Api.Base.SearchParams & {
        /** 单据业务状态筛选 */
        businessStatus?: StockDocumentStatus;
        /** 客户主键筛选 */
        customerId?: string;
        /** 商品主键筛选，命中包含该商品的入库单 */
        goodsId?: string;
        /** 入库时间截止（含），UTC */
        inTimeEnd?: string;
        /** 入库时间起始（含），UTC */
        inTimeStart?: string;
        /** 入库单编号或商品名称、编码关键字，采用包含匹配 */
        keyword?: string;
        /** 入库业务类型筛选：采购、其他或销售退货 */
        orderType?: StockInOrderType;
        /** 供应商主键筛选 */
        supplierId?: string;
        /** 仓库主键筛选 */
        wareId?: string;
      }
    >;

    /** 入库单分页结果 */
    type List = Common.PaginatingQueryRecord<Entity>;

    /** 通用 Payload 类型（兼容旧有宽泛调用）。新代码请使用具体的 Create/Update Payload。 */
    type Payload = Record<string, unknown>;

    /** 复用全部实体类型（兼容 Other/SalesReturn 入库）。 */
    type AllEntity = Pick<Entity, 'id'> & Record<string, unknown>;

    /** Result 类型，暂未使用 */
    type Result = unknown;
  }
}
