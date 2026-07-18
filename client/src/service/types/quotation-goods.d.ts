declare namespace Api {
  namespace QuotationGoods {
    type Entity = Common.CommonRecord<{
      /** 商品编码（详情/关联查询时回填） */
      goodsCode?: string | null;
      goodsId: string;
      /** 商品名称（详情/关联查询时回填） */
      goodsName?: string | null;
      goodsUnitId: string;
      /** 报价单位名称（详情/关联查询时回填） */
      goodsUnitName?: string | null;
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
