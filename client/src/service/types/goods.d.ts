declare namespace Api {
  namespace Goods {
    type Entity = Common.CommonRecord<{
      baseUnitId: string | null;
      brand: string | null;
      code: string;
      defaultSupplierId: string | null;
      defaultWareId: string | null;
      description: string | null;
      goodsTypeId: string;
      isOnSale: boolean;
      name: string;
      origin: string | null;
      remark: string | null;
      spec: string | null;
      supplierIds: string[] | null;
      taxRate: number | null;
    }>;

    type CreateParams = CommonType.RecordNullable<
      Pick<
        Entity,
        | 'baseUnitId'
        | 'brand'
        | 'code'
        | 'defaultSupplierId'
        | 'defaultWareId'
        | 'description'
        | 'goodsTypeId'
        | 'isOnSale'
        | 'name'
        | 'origin'
        | 'remark'
        | 'spec'
        | 'status'
        | 'supplierIds'
        | 'taxRate'
      >
    >;

    type UpdateParams = CreateParams & { id: string };

    type SearchParams = CommonType.RecordNullable<
      Api.Base.SearchParams & {
        defaultSupplierId?: string | null;
        defaultWareId?: string | null;
        goodsTypeId?: string | null;
        isOnSale?: boolean | null;
      }
    >;

    type List = Common.PaginatingQueryRecord<Entity>;

    type AllEntity = Pick<Entity, 'code' | 'id' | 'name'>;
  }
}
