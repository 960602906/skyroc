declare namespace Api {
  namespace Purchaser {
    type Entity = Common.CommonRecord<{
      code: string;
      departmentId: string | null;
      name: string;
      phone: string | null;
      remark: string | null;
      userId: string | null;
    }>;

    type CreateParams = CommonType.RecordNullable<
      Pick<Entity, 'code' | 'departmentId' | 'name' | 'phone' | 'remark' | 'status' | 'userId'>
    >;

    type UpdateParams = CreateParams & { id: string };

    type SearchParams = CommonType.RecordNullable<
      Api.Base.SearchParams & {
        departmentId?: string | null;
      }
    >;

    type List = Common.PaginatingQueryRecord<Entity>;

    type AllEntity = Pick<Entity, 'code' | 'id' | 'name'>;
  }
}
