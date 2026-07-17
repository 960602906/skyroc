declare namespace Api {
  namespace Role {
    type Entity = Common.CommonRecord<{
      code: string;
      desc: string;
      menu?: Api.Menu.Entity[] | null;
      name: string;
    }>;

    type CreateParams = CommonType.RecordNullable<Pick<Entity, 'code' | 'desc' | 'name' | 'status'>>;

    type UpdateParams = CreateParams & { id: string };

    type SearchParams = Api.Base.SearchParams;

    type List = Common.PaginatingQueryRecord<Entity>;

    type AllEntity = Pick<Entity, 'code' | 'id' | 'name'>;

    type AssignMenusParams = {
      menuIds: string[];
      roleId: string;
    };

    type UnassignMenusParams = AssignMenusParams;
  }
}
