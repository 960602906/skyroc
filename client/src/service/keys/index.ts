/**
 * React Query Keys
 *
 * Global unique keys for React Query cache management
 */

export const QUERY_KEYS = {
  // Auth
  AUTH: {
    USER_INFO: ['auth', 'userInfo'] as const
  },
  BASE: {
    COMPANIES: ['base', 'companies'] as const,
    CUSTOMERS: ['base', 'customers'] as const,
    GOODS: ['base', 'goods'] as const,
    GOODS_TYPES: ['base', 'goodsTypes'] as const,
    GOODS_UNITS_BY_GOODS: (goodsId: string) => ['base', 'goodsUnits', goodsId] as const,
    PROTOCOLS: ['base', 'protocols'] as const,
    PURCHASERS: ['base', 'purchasers'] as const,
    QUOTATIONS: ['base', 'quotations'] as const,
    SUPPLIERS: ['base', 'suppliers'] as const,
    WARES: ['base', 'wares'] as const
  },
  CUSTOMER_TAG: {
    TREE: ['customerTag', 'tree'] as const
  },
  // Route
  ROUTE: {
    CONSTANT_ROUTES: ['route', 'constantRoutes'] as const,
    IS_ROUTE_EXIST: (routeName: string) => ['route', 'isRouteExist', routeName] as const,
    USER_ROUTES: ['route', 'userRoutes'] as const
  },
  // System Manage
  SYSTEM_MANAGE: {
    ALL_ROLES: ['systemManage', 'allRoles'] as const,
    DETAILS_ROLE: (id: string) => ['systemManage', 'detailsRole', id] as const,
    MENU_LIST: ['systemManage', 'menuList'] as const,
    MENU_TREE: ['systemManage', 'menuTree'] as const,
    ROLE_LIST: (params?: Api.SystemManage.RoleSearchParams) => ['systemManage', 'roleList', params] as const,
    USER_LIST: (params?: Api.SystemManage.UserSearchParams) => ['systemManage', 'userList', params] as const
  }
} as const;

export const MUTATION_KEYS = {
  AUTH: {
    LOGIN: ['auth', 'login'] as const,
    REFRESH_TOKEN: ['auth', 'refreshToken'] as const
  }
} as const;
