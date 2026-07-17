import { request } from '../request';
import { MENU_BUTTON_URLS } from '../urls';

/** 根据 id 获取菜单按钮 */
export function fetchGetMenuButtonDetail(id: string) {
  return request<Api.MenuButton.Entity>({
    method: 'get',
    url: `${MENU_BUTTON_URLS.BASE}/${id}`
  });
}

/** 创建菜单按钮 */
export function fetchAddMenuButton(data: Api.MenuButton.CreateParams) {
  return request<Api.MenuButton.Entity>({
    data,
    method: 'post',
    url: MENU_BUTTON_URLS.BASE
  });
}

/** 更新菜单按钮 */
export function fetchUpdateMenuButton(data: Api.MenuButton.UpdateParams) {
  return request<Api.MenuButton.Entity>({
    data,
    method: 'put',
    url: MENU_BUTTON_URLS.BASE
  });
}

/** 删除菜单按钮 */
export function fetchDeleteMenuButton(id: string) {
  return request<Api.MenuButton.Entity>({
    method: 'delete',
    url: `${MENU_BUTTON_URLS.BASE}/${id}`
  });
}

/** 批量创建菜单按钮 */
export function fetchBatchCreateMenuButtons(data: Api.MenuButton.BatchCreateParams) {
  return request<Api.MenuButton.Entity[]>({
    data,
    method: 'post',
    url: MENU_BUTTON_URLS.BATCH
  });
}

/** 批量替换菜单按钮 */
export function fetchReplaceMenuButtons(data: Api.MenuButton.ReplaceParams) {
  return request<Api.MenuButton.Entity[]>({
    data,
    method: 'put',
    url: MENU_BUTTON_URLS.REPLACE
  });
}
