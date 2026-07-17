declare namespace Api {
  namespace Company {
    type Entity = Common.CommonRecord<{
      address: string | null;
      code: string;
      contactName: string | null;
      contactPhone: string | null;
      name: string;
      remark: string | null;
    }>;

    type CreateParams = CommonType.RecordNullable<
      Pick<Entity, 'address' | 'code' | 'contactName' | 'contactPhone' | 'name' | 'remark' | 'status'>
    >;

    type UpdateParams = CreateParams & { id: string };

    type SearchParams = Api.Base.SearchParams;

    type List = Common.PaginatingQueryRecord<Entity>;

    type AllEntity = Pick<Entity, 'code' | 'id' | 'name'>;
  }
}
