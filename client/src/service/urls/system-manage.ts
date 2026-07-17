/** System manage module URLs */

export const SYSTEM_MANAGE_URLS = {
  ASSIGN_MENU_URL: '/roles/assignMenus',
  ASSIGN_ROLES_URL: '/users/assignRoles',
  BASE_MENU_URL: '/menus',
  BASE_ROLE_URL: '/roles',
  BASE_USER_URL: '/users',
  BATCH_DELETE_MENU_URL: '/menus/batchDelete',
  BATCH_DELETE_ROLE_URL: '/roles/batchDelete',
  BATCH_DELETE_USER_URL: '/users/batchDelete',
  GET_ALL_ROLES: '/roles',
  GET_ALL_USERS: '/users',
  GET_MENU_LIST: '/menus/list',
  GET_MENU_TREE: '/menus/tree',
  GET_ROLE_LIST: '/roles/list',
  GET_USER_LIST: '/users/list',
  UNASSIGN_MENU_URL: '/roles/unassignRoles',
  UNASSIGN_ROLES_URL: '/users/unassignRoles'
} as const;
