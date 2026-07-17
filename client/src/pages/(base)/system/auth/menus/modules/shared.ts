const LAYOUT_PREFIX = 'layout.';
const VIEW_PREFIX = 'page.';
const FIRST_LEVEL_ROUTE_COMPONENT_SPLIT = '$';

export function createDefaultModel(): Model {
  return {
    activeMenu: null,
    buttons: [],
    component: '',
    constant: false,
    fixedIndexInTab: null,
    hideInMenu: false,
    href: null,
    i18nKey: null,
    icon: '',
    iconType: 1,
    keepAlive: false,
    layout: null,
    menuType: 1,
    multiTab: false,
    name: '',
    order: 0,
    parentId: undefined,
    path: '',
    pathParam: '',
    query: [],
    status: 1,
    title: ''
  };
}

export function getLayoutAndPage(component?: string | null) {
  let layout = '';
  let page = '';

  const [layoutOrPage = '', pageItem = ''] = component?.split(FIRST_LEVEL_ROUTE_COMPONENT_SPLIT) || [];

  layout = getLayout(layoutOrPage);
  page = getPage(pageItem || layoutOrPage);

  return { layout, page };
}

function getLayout(layout: string) {
  return layout.startsWith(LAYOUT_PREFIX) ? layout.replace(LAYOUT_PREFIX, '') : '';
}

function getPage(page: string) {
  return page.startsWith(VIEW_PREFIX) ? page.replace(VIEW_PREFIX, '') : '';
}

/**
 * Get path param from route path
 *
 * @param routePath route path
 */
export function getPathParamFromRoutePath(routePath: string) {
  const [path, param = ''] = routePath.split('/:');

  return {
    param,
    path
  };
}

export function flattenMenu(menuList: Api.SystemManage.Menu[]) {
  const result: CommonType.Option<string>[] = [];

  function flatten(item: Api.SystemManage.Menu) {
    const label = item.title;

    result.push({ label, value: item.id }); // 将当前元素加入结果数组，并移除 children 属性

    if (item.children && Array.isArray(item.children)) item.children.forEach(flatten); // 递归处理 children
  }

  menuList.forEach(flatten); // 对初始数组中的每一个元素进行展开

  return result;
}

export type Model = Pick<
  Api.SystemManage.Menu,
  | 'activeMenu'
  | 'component'
  | 'constant'
  | 'fixedIndexInTab'
  | 'hideInMenu'
  | 'href'
  | 'i18nKey'
  | 'icon'
  | 'iconType'
  | 'keepAlive'
  | 'menuType'
  | 'multiTab'
  | 'name'
  | 'order'
  | 'parentId'
  | 'path'
  | 'status'
  | 'title'
> & {
  buttons: NonNullable<Api.SystemManage.Menu['buttons']>;
  layout: string | null;
  pathParam: string;
  query: NonNullable<Api.SystemManage.Menu['query']>;
};

export type OperateType = AntDesign.TableOperateType | 'addChild';

export type Props = Omit<Page.OperateDrawerProps, ' operateType'> & {
  allPages: string[];
  menuList: CommonType.Option<string>[];
  operateType: OperateType;
};

export type RuleKey = Extract<keyof Model, 'name' | 'path' | 'status' | 'title'>;
