declare namespace Api {
  namespace CustomerTag {
    type Entity = Common.CommonRecord<{
      code: string;
      name: string;
      parentId: string | null;
      remark: string | null;
      sort: number;
    }>;

    type CreateParams = CommonType.RecordNullable<
      Pick<Entity, 'code' | 'name' | 'parentId' | 'remark' | 'sort' | 'status'>
    >;

    type UpdateParams = CreateParams & { id: string };

    type SearchParams = CommonType.RecordNullable<
      Api.Base.SearchParams & {
        parentId?: string | null;
      }
    >;

    type List = Common.PaginatingQueryRecord<Entity>;

    type AllEntity = Pick<Entity, 'code' | 'id' | 'name'>;
  }
}
