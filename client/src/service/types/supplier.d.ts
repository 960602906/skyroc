declare namespace Api {
  namespace Supplier {
    type Entity = Common.CommonRecord<{
      address: string | null;
      bankAccount: string | null;
      bankName: string | null;
      code: string;
      contactName: string | null;
      contactPhone: string | null;
      name: string;
      remark: string | null;
      taxNo: string | null;
    }>;

    type CreateParams = CommonType.RecordNullable<
      Pick<
        Entity,
        | 'address'
        | 'bankAccount'
        | 'bankName'
        | 'code'
        | 'contactName'
        | 'contactPhone'
        | 'name'
        | 'remark'
        | 'status'
        | 'taxNo'
      >
    >;

    type UpdateParams = CreateParams & { id: string };

    type SearchParams = Api.Base.SearchParams;

    type List = Common.PaginatingQueryRecord<Entity>;

    type AllEntity = Pick<Entity, 'code' | 'id' | 'name'>;
  }
}
