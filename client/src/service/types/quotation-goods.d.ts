declare namespace Api {
  namespace QuotationGoods {
    type Entity = Common.CommonRecord<{
      goodsId: string;
      goodsUnitId: string;
      isOnSale: boolean;
      minOrderQuantity: number | null;
      quotationId: string;
      remark: string | null;
      unitPrice: number;
    }>;

    type CreateParams = CommonType.RecordNullable<
      Pick<Entity, 'goodsId' | 'goodsUnitId' | 'isOnSale' | 'minOrderQuantity' | 'quotationId' | 'remark' | 'unitPrice'>
    >;

    type UpdateParams = CreateParams & { id: string };

    type SearchParams = CommonType.RecordNullable<
      Pick<Common.CommonRecord, 'status'> &
        Common.CommonSearchParams & {
          goodsId?: string | null;
          isOnSale?: boolean | null;
          quotationId?: string | null;
        }
    >;

    type List = Common.PaginatingQueryRecord<Entity>;

    type AllEntity = Pick<Entity, 'goodsId' | 'id' | 'quotationId'>;
  }
}
