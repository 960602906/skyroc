declare namespace Api {
  namespace PurchaseRule {
    type Entity = Common.CommonRecord<{
      code: string;
      customerIds: string[] | null;
      goodsIds: string[] | null;
      goodsTypeId: string | null;
      name: string;
      purchasePattern: number;
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
        purchasePattern?: number | null;
        purchaserId?: string | null;
        supplierId?: string | null;
        wareId?: string | null;
      }
    >;

    type List = Common.PaginatingQueryRecord<Entity>;

    type AllEntity = Pick<Entity, 'code' | 'id' | 'name'>;
  }
}
