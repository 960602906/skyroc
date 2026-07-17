declare namespace Api {
  namespace Ware {
    type Entity = Common.CommonRecord<{
      address: string | null;
      code: string;
      contactName: string | null;
      contactPhone: string | null;
      name: string;
      remark: string | null;
      sort: number;
    }>;

    type CreateParams = CommonType.RecordNullable<
      Pick<Entity, 'address' | 'code' | 'contactName' | 'contactPhone' | 'name' | 'remark' | 'sort' | 'status'>
    >;

    type UpdateParams = CreateParams & { id: string };

    type SearchParams = Api.Base.SearchParams;

    type List = Common.PaginatingQueryRecord<Entity>;

    type AllEntity = Pick<Entity, 'code' | 'id' | 'name'>;
  }
}
