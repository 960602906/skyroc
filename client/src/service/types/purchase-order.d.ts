declare namespace Api {
  namespace PurchaseOrder {
    /** 采购单执行状态 */
    type BusinessStatus = import('../enums').PurchaseOrderStatusValue;

    /** 采购模式 */
    type PurchasePattern = import('../enums').PurchasePatternValue;

    /** 采购单商品行对采购计划明细的数量占用请求 */
    type PlanAllocation = {
      /** 从来源计划占用的采购数量，按采购单商品行的采购单位计量且必须大于零 */
      allocatedQuantity: number;
      /** 来源采购计划商品明细主键；商品和采购单位必须与采购单商品行一致 */
      purchasePlanDetailId: string;
    };

    /** 采购单商品行的采购计划来源，记录被占用的计划数量 */
    type PlanRelation = Common.CommonRecord<{
      /** 从来源计划占用的数量，按采购商品行的采购单位计量 */
      allocatedQuantity: number;
      /** 所属采购单商品行主键 */
      purchaseOrderDetailId: string;
      /** 来源采购计划商品明细主键 */
      purchasePlanDetailId: string;
      /** 来源采购计划主键 */
      purchasePlanId: string;
      /** 来源采购计划业务编号 */
      purchasePlanNo: string;
    }>;

    /** 采购单商品明细 DTO，返回商品、单位、数量、金额和计划来源快照 */
    type Detail = Common.CommonRecord<{
      /** 采购发生时的商品编码快照 */
      goodsCode: string;
      /** 采购商品主键 */
      goodsId: string;
      /** 商品规格、品牌和产地等历史详情快照 */
      goodsInfo?: string | null;
      /** 采购发生时的商品名称快照 */
      goodsName: string;
      /** 当前商品行占用的采购计划来源集合 */
      planRelations: PlanRelation[];
      /** 商品生产日期，仅记录自然日；未知时为空 */
      productDate?: string | null;
      /** 所属采购单主键 */
      purchaseOrderId: string;
      /** 采购单价，币种沿用系统业务币种 */
      purchasePrice: number;
      /** 本单采购数量，按采购单位计量 */
      purchaseQuantity: number;
      /** 采购金额，为采购数量乘以单价后的金额快照 */
      purchaseTotalPrice: number;
      /** 采购单位主键 */
      purchaseUnitId: string;
      /** 采购发生时的采购单位名称快照 */
      purchaseUnitName: string;
      /** 当前采购商品行的业务备注 */
      remark?: string | null;
      /** 业务需求数量，按采购单位计量 */
      requiredQuantity: number;
    }>;

    /** 采购单 DTO，返回供货责任快照、执行状态及采购商品明细 */
    type Entity = Common.CommonRecord<{
      /** 采购单执行状态：草稿、已完成或已取消 */
      businessStatus: BusinessStatus;
      /** 采购商品明细集合 */
      details: Detail[];
      /** 采购单业务编号 */
      purchaseNo: string;
      /** 采购模式：供应商直供或市场自采 */
      purchasePattern: PurchasePattern;
      /** 执行采购的采购员主键；未分配时为空 */
      purchaserId?: string | null;
      /** 采购单保存时的采购员名称快照 */
      purchaserName?: string | null;
      /** 预计到货时间（UTC）；尚未确认时为空 */
      receiveTime?: string | null;
      /** 采购单级业务备注 */
      remark?: string | null;
      /** 业务发生时的供应商联系人姓名快照 */
      supplierContactName?: string | null;
      /** 业务发生时的供应商联系人电话快照 */
      supplierContactPhone?: string | null;
      /** 供应商主键；市场自采且未指定供货方时为空 */
      supplierId?: string | null;
      /** 采购单保存时的供应商名称快照 */
      supplierName?: string | null;
    }>;

    /** 手工创建采购单时的商品行请求，不接受采购计划来源占用 */
    type CreateDetailPayload = {
      /** 采购商品主键 */
      goodsId: string;
      /** 商品生产日期，仅记录自然日；未知时可为空 */
      productDate?: string | null;
      /** 采购单价，币种沿用系统业务币种且不得为负数 */
      purchasePrice: number;
      /** 本单采购数量，按采购单位计量且必须大于零 */
      purchaseQuantity: number;
      /** 采购单位主键，必须属于所选商品 */
      purchaseUnitId: string;
      /** 仅针对当前采购商品行的备注 */
      remark?: string | null;
      /** 业务需求数量，按采购单位计量；省略时等于采购数量 */
      requiredQuantity?: number | null;
    };

    /** 手工创建采购单请求，创建结果始终为可编辑草稿 */
    type CreatePayload = {
      /** 手工采购商品行，至少包含一项 */
      details: CreateDetailPayload[];
      /** 采购模式：供应商直供或市场自采 */
      purchasePattern: PurchasePattern;
      /** 执行采购的采购员主键；草稿阶段可暂不分配 */
      purchaserId?: string | null;
      /** 预计到货时间（UTC）；尚未确认时可为空 */
      receiveTime?: string | null;
      /** 采购单级备注 */
      remark?: string | null;
      /** 供应商联系人姓名快照；未传时使用供应商档案当前值 */
      supplierContactName?: string | null;
      /** 供应商联系人电话快照；未传时使用供应商档案当前值 */
      supplierContactPhone?: string | null;
      /** 供应商主键；供应商直供模式必填，市场自采模式可为空 */
      supplierId?: string | null;
    };

    /** 编辑采购单时的商品行请求，可重新声明该行占用的采购计划数量 */
    type UpdateDetailPayload = {
      /** 采购商品主键 */
      goodsId: string;
      /** 原商品行主键；新增商品行时为空，非空时必须属于当前采购单 */
      id?: string | null;
      /** 当前商品行的采购计划来源；有来源时占用数量合计必须等于采购数量 */
      planAllocations: PlanAllocation[];
      /** 商品生产日期，仅记录自然日；未知时可为空 */
      productDate?: string | null;
      /** 采购单价，币种沿用系统业务币种且不得为负数 */
      purchasePrice: number;
      /** 本单采购数量，按采购单位计量且必须大于零 */
      purchaseQuantity: number;
      /** 采购单位主键，必须属于所选商品 */
      purchaseUnitId: string;
      /** 仅针对当前采购商品行的备注 */
      remark?: string | null;
      /** 业务需求数量，按采购单位计量；省略时等于采购数量 */
      requiredQuantity?: number | null;
    };

    /** 编辑采购单及其全部商品行的请求，仅草稿采购单可执行 */
    type UpdatePayload = {
      /** 替换后的完整商品行集合，至少包含一项 */
      details: UpdateDetailPayload[];
      /** 待编辑采购单主键 */
      id: string;
      /** 采购模式：供应商直供或市场自采 */
      purchasePattern: PurchasePattern;
      /** 执行采购的采购员主键；完成采购单前必须分配 */
      purchaserId?: string | null;
      /** 预计到货时间（UTC）；尚未确认时可为空 */
      receiveTime?: string | null;
      /** 采购单级备注 */
      remark?: string | null;
      /** 供应商联系人姓名快照；未传时使用供应商档案当前值 */
      supplierContactName?: string | null;
      /** 供应商联系人电话快照；未传时使用供应商档案当前值 */
      supplierContactPhone?: string | null;
      /** 供应商主键；供应商直供模式必填，市场自采模式可为空 */
      supplierId?: string | null;
    };

    /** 从采购计划剩余数量批量生成采购单的请求 */
    type GenerateFromPlansPayload = {
      /** 待生成采购单的采购计划主键集合；重复主键会被去重 */
      planIds: string[];
      /** 预计到货时间（UTC）；省略时每组采购单取来源计划最早交期 */
      receiveTime?: string | null;
      /** 写入本次生成采购单的统一业务备注 */
      remark?: string | null;
    };

    /** 采购单分页查询参数 */
    type SearchParams = CommonType.RecordNullable<
      Api.Base.SearchParams & {
        /** 采购单执行状态筛选 */
        businessStatus?: BusinessStatus;
        /** 商品主键筛选，命中包含该商品的采购单 */
        goodsId?: string;
        /** 采购单编号或商品名称、编码关键字，采用包含匹配 */
        keyword?: string;
        /** 采购模式筛选 */
        purchasePattern?: PurchasePattern;
        /** 采购员主键筛选 */
        purchaserId?: string;
        /** 预计到货截止时间（含），UTC */
        receiveTimeEnd?: string;
        /** 预计到货起始时间（含），UTC */
        receiveTimeStart?: string;
        /** 供应商主键筛选 */
        supplierId?: string;
      }
    >;

    /** 分页查询结果 */
    type List = Common.PaginatingQueryRecord<Entity>;
  }
}
