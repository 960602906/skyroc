declare namespace Api {
  namespace CustomerProtocolGoods {
    type Entity = Common.CommonRecord<{
      customerProtocolId: string;
      /** 商品编码（详情/关联查询时回填） */
      goodsCode?: string | null;
      goodsId: string;
      /** 商品名称（详情/关联查询时回填） */
      goodsName?: string | null;
      goodsUnitId: string;
      /** 协议价单位名称（详情/关联查询时回填） */
      goodsUnitName?: string | null;
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
