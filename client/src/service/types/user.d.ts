declare namespace Api {
  namespace User {
    type UserGender = import('../enums').UserGenderValue;

    type Entity = Common.CommonRecord<{
      email: string;
      gender: UserGender | null;
      nickName: string;
      phone: string;
      roleId: string | null;
      username: string;
    }>;

    type CreateParams = CommonType.RecordNullable<
      Pick<Entity, 'email' | 'gender' | 'nickName' | 'phone' | 'roleId' | 'status' | 'username'> & {
        password?: string | null;
      }
    >;

    type UpdateParams = CreateParams & { id: string };

    type SearchParams = CommonType.RecordNullable<
      Pick<Entity, 'email' | 'gender' | 'nickName' | 'phone' | 'status' | 'username'> & Common.CommonSearchParams
    >;

    type List = Common.PaginatingQueryRecord<Entity>;

    type AssignRolesParams = {
      roleIds: string[];
      userId: string;
    };

    type UnassignRolesParams = AssignRolesParams;
  }
}
