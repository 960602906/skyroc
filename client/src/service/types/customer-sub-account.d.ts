declare namespace Api {
  namespace CustomerSubAccount {
    type Entity = Common.CommonRecord<{
      companyId: string;
      customerId: string | null;
      email: string | null;
      nickName: string | null;
      passwordHash: string | null;
      phone: string | null;
      remark: string | null;
      username: string | null;
    }>;

    type CreateParams = CommonType.RecordNullable<
      Pick<
        Entity,
        'companyId' | 'customerId' | 'email' | 'nickName' | 'passwordHash' | 'phone' | 'remark' | 'status' | 'username'
      >
    >;

    type UpdateParams = CreateParams & { id: string };

    type SearchParams = CommonType.RecordNullable<
      Pick<Common.CommonRecord, 'status'> &
        Common.CommonSearchParams & {
          companyId?: string | null;
          customerId?: string | null;
          nickName?: string | null;
          username?: string | null;
        }
    >;

    type List = Common.PaginatingQueryRecord<Entity>;

    type AllEntity = Pick<Entity, 'companyId' | 'id' | 'username'>;
  }
}
