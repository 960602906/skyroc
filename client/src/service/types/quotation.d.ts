declare namespace Api {
  namespace Quotation {
    type Entity = Common.CommonRecord<{
      code: string;
      customerIds: string[] | null;
      description: string | null;
      effectiveEnd: string | null;
      effectiveStart: string | null;
      isAudited: boolean;
      name: string;
      remark: string | null;
    }>;

    type CreateParams = CommonType.RecordNullable<
      Pick<
        Entity,
        | 'code'
        | 'customerIds'
        | 'description'
        | 'effectiveEnd'
        | 'effectiveStart'
        | 'isAudited'
        | 'name'
        | 'remark'
        | 'status'
      >
    >;

    type UpdateParams = CreateParams & { id: string };

    type SearchParams = CommonType.RecordNullable<
      Api.Base.SearchParams & {
        isAudited?: boolean | null;
      }
    >;

    type List = Common.PaginatingQueryRecord<Entity>;

    type AllEntity = Pick<Entity, 'code' | 'id' | 'name'>;
  }
}
