declare namespace Api {
  namespace CustomerProtocol {
    type Entity = Common.CommonRecord<{
      code: string;
      customerIds: string[] | null;
      effectiveEnd: string | null;
      effectiveStart: string;
      /** 协议价商品明细（详情接口返回） */
      goods?: Api.CustomerProtocolGoods.Entity[] | null;
      name: string;
      quotationId: string | null;
      /** 关联报价单名称（详情/关联查询时回填） */
      quotationName?: string | null;
      remark: string | null;
    }>;

    type CreateParams = CommonType.RecordNullable<
      Pick<
        Entity,
        'code' | 'customerIds' | 'effectiveEnd' | 'effectiveStart' | 'name' | 'quotationId' | 'remark' | 'status'
      >
    >;

    type UpdateParams = CreateParams & { id: string };

    type SearchParams = CommonType.RecordNullable<
      Api.Base.SearchParams & {
        quotationId?: string | null;
      }
    >;

    type List = Common.PaginatingQueryRecord<Entity>;

    type AllEntity = Pick<Entity, 'code' | 'id' | 'name'>;
  }
}
