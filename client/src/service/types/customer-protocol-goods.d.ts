declare namespace Api {
  namespace CustomerProtocolGoods {
    type Entity = Common.CommonRecord<{
      customerProtocolId: string;
      goodsId: string;
      goodsUnitId: string;
      minOrderQuantity: number | null;
      protocolPrice: number;
      remark: string | null;
    }>;

    type CreateParams = CommonType.RecordNullable<
      Pick<Entity, 'customerProtocolId' | 'goodsId' | 'goodsUnitId' | 'minOrderQuantity' | 'protocolPrice' | 'remark'>
    >;

    type UpdateParams = CreateParams & { id: string };

    type SearchParams = CommonType.RecordNullable<
      Pick<Common.CommonRecord, 'status'> &
        Common.CommonSearchParams & {
          customerProtocolId?: string | null;
          goodsId?: string | null;
        }
    >;

    type List = Common.PaginatingQueryRecord<Entity>;

    type AllEntity = Pick<Entity, 'customerProtocolId' | 'goodsId' | 'id'>;
  }
}
