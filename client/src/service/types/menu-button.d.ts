declare namespace Api {
  namespace MenuButton {
    type Entity = {
      code: string;
      desc: string;
      id: string;
      menuId: string;
    };

    type CreateParams = CommonType.RecordNullable<Pick<Entity, 'code' | 'desc' | 'menuId'>>;

    type UpdateParams = CreateParams & { id: string };

    type BatchCreateParams = CreateParams[];

    type ReplaceParams = {
      buttons: CreateParams[];
      menuId: string;
    };
  }
}
