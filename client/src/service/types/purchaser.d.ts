declare namespace Api {
  namespace Purchaser {
    type Entity = Common.CommonRecord<{
      code: string;
      departmentId: string | null;
      /** 所属部门名称（列表/详情回填） */
      departmentName: string | null;
      name: string;
      phone: string | null;
      remark: string | null;
      userId: string | null;
      /** 关联系统用户名（列表/详情回填） */
      userName: string | null;
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
