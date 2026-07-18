declare namespace Api {
  namespace PurchaseRule {
    /**
     * 采购模式
     *
     * - 1: 供应商直供
     * - 2: 市场自采
     */
    type PurchasePattern = import('../enums').PurchasePatternValue;

    type Entity = Common.CommonRecord<{
      code: string;
      customerIds: string[] | null;
      goodsIds: string[] | null;
      goodsTypeId: string | null;
      name: string;
      purchasePattern: PurchasePattern;
      purchaserId: string | null;
      remark: string | null;
      supplierId: string | null;
      wareId: string | null;
    }>;

    type CreateParams = CommonType.RecordNullable<
      Pick<
        Entity,
        | 'code'
        | 'customerIds'
        | 'goodsIds'
        | 'goodsTypeId'
        | 'name'
        | 'purchasePattern'
        | 'purchaserId'
        | 'remark'
        | 'status'
        | 'supplierId'
        | 'wareId'
      >
    >;

    type UpdateParams = CreateParams & { id: string };

    type SearchParams = CommonType.RecordNullable<
      Api.Base.SearchParams & {
        goodsTypeId?: string | null;
        purchasePattern?: PurchasePattern | null;
        purchaserId?: string | null;
        supplierId?: string | null;
        wareId?: string | null;
      }
    >;

    type List = Common.PaginatingQueryRecord<Entity>;

    type AllEntity = Pick<Entity, 'code' | 'id' | 'name'>;
  }
}
