declare namespace Api {
  namespace Menu {
    type MenuType = import('../enums').MenuTypeValue;

    type IconType = import('../enums').IconTypeValue;

    type Button = Api.MenuButton.Entity;

    type MenuPropsOfRoute = Pick<
      Router.RouteHandle,
      | 'activeMenu'
      | 'constant'
      | 'fixedIndexInTab'
      | 'hideInMenu'
      | 'href'
      | 'i18nKey'
      | 'keepAlive'
      | 'multiTab'
      | 'order'
      | 'query'
    >;

    type Entity = Common.CommonRecord<{
      activeMenu: string | null;
      buttons: Button[] | null;
      children?: Entity[] | null;
      component: string | null;
      constant: boolean;
      fixedIndexInTab: number | null;
      hideInMenu: boolean;
      href: string | null;
      i18NKey: string | null;
      icon: string;
      iconType: IconType;
      keepAlive: boolean;
      layout: string | null;
      localIcon: string | null;
      menuType: MenuType;
      multiTab: boolean | null;
      name: string;
      order: number | null;
      parentId?: string | null;
      path: string;
      title: string;
    }> &
      MenuPropsOfRoute;

    type CreateParams = CommonType.RecordNullable<
      Pick<
        Entity,
        | 'activeMenu'
        | 'buttons'
        | 'component'
        | 'constant'
        | 'fixedIndexInTab'
        | 'hideInMenu'
        | 'href'
        | 'icon'
        | 'iconType'
        | 'keepAlive'
        | 'layout'
        | 'localIcon'
        | 'menuType'
        | 'multiTab'
        | 'name'
        | 'order'
        | 'parentId'
        | 'path'
        | 'status'
        | 'title'
      > & { i18NKey?: string | null }
    >;

    type UpdateParams = CreateParams & { id: string };

    type SearchParams = CommonType.RecordNullable<
      Pick<Entity, 'menuType' | 'name' | 'status'> &
        Common.CommonSearchParams & {
          isHidden?: boolean | null;
        }
    >;

    type List = Common.PaginatingQueryRecord<Entity>;

    type TreeNode = {
      children?: TreeNode[];
      id: string;
      label: string;
      parentId: string;
    };

    type Tree = TreeNode[];
  }
}
