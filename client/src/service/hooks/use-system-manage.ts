import { useQuery } from '@tanstack/react-query';

import {
  fetchGetBoundedRoles,
  fetchGetMenuList,
  fetchGetMenuTree,
  fetchGetRoleDetail,
  fetchGetRoleList,
  fetchGetUserList
} from '../api';
import { QUERY_KEYS } from '../keys';

/**
 * Get role list hook
 *
 * @example
 *   const { data: roleList, isLoading } = useRoleList({ current: 1, size: 10 });
 *
 * @param params - Search parameters
 */
export function useRoleList(params?: Api.SystemManage.RoleSearchParams) {
  return useQuery({
    queryFn: () => fetchGetRoleList(params),
    queryKey: QUERY_KEYS.SYSTEM_MANAGE.ROLE_LIST(params)
  });
}

/**
 * Get all roles hook
 *
 * @example
 *   const { data: allRoles, isLoading } = useAllRoles();
 */
export function useAllRoles() {
  return useQuery({
    queryFn: fetchGetBoundedRoles,
    queryKey: QUERY_KEYS.SYSTEM_MANAGE.ALL_ROLES,
    select: data => data.map(item => ({ code: item.secondaryText ?? '', id: item.id, name: item.label })),
    staleTime: 0
  });
}

/**
 * Get user list hook
 *
 * @example
 *   const { data: userList, isLoading } = useUserList({ current: 1, size: 10 });
 *
 * @param params - Search parameters
 */
export function useUserList(params?: Api.SystemManage.UserSearchParams) {
  return useQuery({
    queryFn: () => fetchGetUserList(params),
    queryKey: QUERY_KEYS.SYSTEM_MANAGE.USER_LIST(params)
  });
}

/**
 * Get menu list hook
 *
 * @example
 *   const { data: menuList, isLoading } = useMenuList();
 */
export function useMenuList(params?: Api.Menu.SearchParams) {
  return useQuery({
    queryFn: () => fetchGetMenuList(params),
    queryKey: QUERY_KEYS.SYSTEM_MANAGE.MENU_LIST
  });
}

/**
 * Get menu tree hook
 *
 * @example
 *   const { data: menuTree, isLoading } = useMenuTree();
 */
export function useMenuTree() {
  return useQuery({
    queryFn: fetchGetMenuTree,
    queryKey: QUERY_KEYS.SYSTEM_MANAGE.MENU_TREE
  });
}

/** 角色详情 hook */
export function useRoleDetail(id: string) {
  return useQuery({
    queryFn: () => fetchGetRoleDetail(id),
    queryKey: QUERY_KEYS.SYSTEM_MANAGE.ROLE_DETAIL(id)
  });
}
