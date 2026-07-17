import { request } from '../request';
import { SYSTEM_MANAGE_URLS } from '../urls';

/** 分页查询角色 */
export function fetchGetRoleList(params?: Api.SystemManage.RoleSearchParams) {
  return request<Api.SystemManage.RoleList>({
    method: 'get',
    params,
    url: SYSTEM_MANAGE_URLS.GET_ROLE_LIST
  });
}

/** 更新角色 */
export function fetchUpdateRole(data: Api.Role.UpdateParams) {
  return request<Api.SystemManage.Role>({
    data,
    method: 'put',
    url: SYSTEM_MANAGE_URLS.BASE_ROLE_URL
  });
}

/** 创建角色 */
export function fetchAddRole(data: Api.Role.CreateParams) {
  return request<Api.SystemManage.Role>({
    data,
    method: 'post',
    url: SYSTEM_MANAGE_URLS.BASE_ROLE_URL
  });
}

/** 批量删除角色 */
export function fetchBatchDeleteRole(ids: string[]) {
  return request<Api.SystemManage.Role>({
    data: ids,
    method: 'delete',
    url: SYSTEM_MANAGE_URLS.BATCH_DELETE_ROLE_URL
  });
}

/** 删除角色 */
export function fetchDeleteRole(id: string) {
  return request<Api.SystemManage.Role>({
    method: 'delete',
    url: `${SYSTEM_MANAGE_URLS.BASE_ROLE_URL}/${id}`
  });
}

/** 查询所有角色 */
export function fetchGetAllRoles() {
  return request<Api.SystemManage.AllRole[]>({
    method: 'get',
    url: SYSTEM_MANAGE_URLS.GET_ALL_ROLES
  });
}

/** 根据id获取角色 */
export function fetchGetDetailRole(id: string) {
  return request<Api.SystemManage.Role>({
    method: 'get',
    url: `${SYSTEM_MANAGE_URLS.BASE_ROLE_URL}/${id}`
  });
}

/** 为角色分配菜单 */
export function fetchAssignMenu(roleId: string, menuIds: string[]) {
  return request<null>({
    data: {
      menuIds,
      roleId
    } satisfies Api.Role.AssignMenusParams,
    method: 'post',
    url: SYSTEM_MANAGE_URLS.ASSIGN_MENU_URL
  });
}

/** 为角色删除菜单 */
export function fetchUnassignMenu(roleId: string, menuIds: string[]) {
  return request<null>({
    data: {
      menuIds,
      roleId
    } satisfies Api.Role.UnassignMenusParams,
    method: 'post',
    url: SYSTEM_MANAGE_URLS.UNASSIGN_MENU_URL
  });
}

/** 分页查询用户 */
export function fetchGetUserList(params?: Api.SystemManage.UserSearchParams) {
  return request<Api.SystemManage.UserList>({
    method: 'get',
    params,
    url: SYSTEM_MANAGE_URLS.GET_USER_LIST
  });
}

/** 创建用户 */
export function fetchAddUser(data: Api.User.CreateParams) {
  return request<Api.SystemManage.User>({
    data,
    method: 'post',
    url: SYSTEM_MANAGE_URLS.BASE_USER_URL
  });
}

/** 更新用户 */
export function fetchUpdateUser(data: Api.User.UpdateParams) {
  return request<Api.SystemManage.User>({
    data,
    method: 'put',
    url: SYSTEM_MANAGE_URLS.BASE_USER_URL
  });
}

/** 删除用户 */
export function fetchDeleteUser(id: string) {
  return request<Api.SystemManage.User>({
    method: 'delete',
    url: `${SYSTEM_MANAGE_URLS.BASE_USER_URL}/${id}`
  });
}

/** 批量删除用户 */
export function fetchBatchDeleteUser(ids: string[]) {
  return request<Api.SystemManage.User>({
    data: ids,
    method: 'delete',
    url: SYSTEM_MANAGE_URLS.BATCH_DELETE_USER_URL
  });
}

/** 根据id获取用户 */
export function fetchGetDetailUser(id: string) {
  return request<Api.SystemManage.User>({
    method: 'get',
    url: `${SYSTEM_MANAGE_URLS.BASE_USER_URL}/${id}`
  });
}

/** 查询所有用户 */
export function fetchGetAllUsers() {
  return request<Api.SystemManage.User[]>({
    method: 'get',
    url: SYSTEM_MANAGE_URLS.GET_ALL_USERS
  });
}

/** 为用户分配角色 */
export function fetchAssignRoles(userId: string, roleIds: string[]) {
  return request<null>({
    data: {
      roleIds,
      userId
    } satisfies Api.User.AssignRolesParams,
    method: 'post',
    url: SYSTEM_MANAGE_URLS.ASSIGN_ROLES_URL
  });
}

/** 为用户移除角色 */
export function fetchUnassignRoles(userId: string, roleIds: string[]) {
  return request<null>({
    data: {
      roleIds,
      userId
    } satisfies Api.User.UnassignRolesParams,
    method: 'delete',
    url: SYSTEM_MANAGE_URLS.UNASSIGN_ROLES_URL
  });
}

/** 分页查询菜单 */
export function fetchGetMenuList(params?: Api.Menu.SearchParams) {
  return request<Api.SystemManage.MenuList>({
    method: 'get',
    params,
    url: SYSTEM_MANAGE_URLS.GET_MENU_LIST
  });
}

/** 查询所有菜单 */
export function fetchGetAllMenus() {
  return request<Api.SystemManage.Menu[]>({
    method: 'get',
    url: SYSTEM_MANAGE_URLS.BASE_MENU_URL
  });
}

/** 更新菜单 */
export function fetchUpdateMenu(data: Api.Menu.UpdateParams) {
  return request<Api.SystemManage.Menu>({
    data,
    method: 'put',
    url: SYSTEM_MANAGE_URLS.BASE_MENU_URL
  });
}

/** 创建菜单 */
export function fetchAddMenu(data: Api.Menu.CreateParams) {
  return request<Api.SystemManage.Menu>({
    data,
    method: 'post',
    url: SYSTEM_MANAGE_URLS.BASE_MENU_URL
  });
}

/** 删除菜单 */
export function fetchDeleteMenu(id: string) {
  return request<Api.SystemManage.Menu>({
    method: 'delete',
    url: `${SYSTEM_MANAGE_URLS.BASE_MENU_URL}/${id}`
  });
}

/** 批量删除菜单 */
export function fetchBatchDeleteMenu(ids: string[]) {
  return request<Api.SystemManage.Menu>({
    data: ids,
    method: 'delete',
    url: SYSTEM_MANAGE_URLS.BATCH_DELETE_MENU_URL
  });
}

/** 根据id 获取菜单 */
export function fetchGetMenuDetail(id: string) {
  return request<Api.SystemManage.Menu>({
    method: 'get',
    url: `${SYSTEM_MANAGE_URLS.BASE_MENU_URL}/${id}`
  });
}

/** 查询菜单树 */
export function fetchGetMenuTree() {
  return request<Api.SystemManage.MenuTree[]>({
    method: 'get',
    url: SYSTEM_MANAGE_URLS.GET_MENU_TREE
  });
}
