declare namespace Api {
  namespace CustomerProtocol {
    type Entity = Common.CommonRecord<{
      code: string;
      customerIds: string[] | null;
      effectiveEnd: string | null;
      effectiveStart: string;
      name: string;
      quotationId: string | null;
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
