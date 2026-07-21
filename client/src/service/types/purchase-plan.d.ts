declare namespace Api {
  namespace PurchasePlan {
    /** 采购模式，1 为供应商直供，2 为市场自采。 */
    type PurchasePattern = import('../enums').PurchasePatternValue;

    /** 采购单生成进度。 */
    type PurchaseStatus = import('../enums').PurchasePlanStatusValue;

    /** 采购计划商品对应的销售订单需求。 */
    type OrderRelation = Common.CommonRecord<{
      purchasePlanDetailId: string;
      requiredQuantity: number;
      saleOrderDetailId: string;
      saleOrderId: string;
      saleOrderNo: string | null;
    }>;

    /** 采购计划商品明细。 */
    type Detail = Common.CommonRecord<{
      goodsCode: string;
      goodsId: string;
      goodsName: string;
      orderRelations: OrderRelation[];
      plannedQuantity: number;
      purchasedQuantity: number;
      purchasePlanId: string;
      purchaseUnitId: string;
      purchaseUnitName: string;
      remark: string | null;
      requiredQuantity: number;
    }>;

    /** 采购计划详情。 */
    type Entity = Common.CommonRecord<{
      details: Detail[];
      planDate: string;
      planNo: string;
      purchasePattern: PurchasePattern;
      purchaserId: string | null;
      purchaserName: string | null;
      purchaseStatus: PurchaseStatus;
      remark: string | null;
      supplierId: string | null;
      supplierName: string | null;
    }>;

    /** 手工创建计划的商品行。 */
    type CreateDetailParams = {
      goodsId: string;
      plannedQuantity: number;
      purchaseUnitId: string;
      remark?: string | null;
      requiredQuantity?: number | null;
    };

    type CreateParams = {
      details: CreateDetailParams[];
      planDate: string;
      purchasePattern?: PurchasePattern;
      purchaserId?: string | null;
      remark?: string | null;
      supplierId?: string | null;
    };

    type GenerateParams = { orderIds: string[]; remark?: string | null };
    type AssignSupplierParams = { planIds: string[]; supplierId?: string | null };
    type AssignPurchaserParams = { planIds: string[]; purchaserId?: string | null };
    type MergeParams = { planIds: string[]; remark?: string | null };
    type SplitOrdersParams = { planId: string; remark?: string | null; saleOrderIds: string[] };
    type SplitQuantityParams = {
      details: Array<{ detailId: string; quantity: number }>;
      planId: string;
      remark?: string | null;
    };

    /** 可按来源订单拆分的订单摘要。 */
    type SplittableOrder = { requiredQuantity: number; saleOrderId: string; saleOrderNo: string };

    /** 列表筛选条件。 */
    type SearchParams = CommonType.RecordNullable<
      Api.Common.CommonSearchParams & {
        goodsId?: string | null;
        keyword?: string | null;
        planDateEnd?: string | null;
        planDateStart?: string | null;
        purchasePattern?: PurchasePattern | null;
        purchaserId?: string | null;
        purchaseStatus?: PurchaseStatus | null;
        status?: Api.Common.EnableStatus | null;
        supplierId?: string | null;
      }
    >;

    type List = Common.PaginatingQueryRecord<Entity>;
  }
}
