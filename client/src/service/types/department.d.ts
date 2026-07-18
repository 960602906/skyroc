declare namespace Api {
  namespace Department {
    type Entity = Common.CommonRecord<{
      children?: Entity[] | null;
      code: string;
      email: string | null;
      leaderId: string | null;
      leaderName: string | null;
      name: string;
      parentId: string | null;
      phone: string | null;
      remark: string | null;
      sort: number | null;
    }>;

    type CreateParams = CommonType.RecordNullable<
      Pick<
        Entity,
        'code' | 'email' | 'leaderId' | 'leaderName' | 'name' | 'parentId' | 'phone' | 'remark' | 'sort' | 'status'
      >
    >;

    type UpdateParams = CreateParams & { id: string };
  }
}
