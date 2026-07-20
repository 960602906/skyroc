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
    CUSTOMER_TAGS: ['base', 'customerTags'] as const,
    CUSTOMERS: ['base', 'customers'] as const,
    DEPARTMENT_TREE: ['base', 'departmentTree'] as const,
    DRIVERS: ['base', 'drivers'] as const,
    GOODS: ['base', 'goods'] as const,
    GOODS_TYPE_TREE: ['base', 'goodsTypeTree'] as const,
    GOODS_TYPES: ['base', 'goodsTypes'] as const,
    GOODS_UNITS: ['base', 'allGoodsUnits'] as const,
    GOODS_UNITS_BY_GOODS: (goodsId: string) => ['base', 'goodsUnits', goodsId] as const,
    PROTOCOLS: ['base', 'protocols'] as const,
    PURCHASERS: ['base', 'purchasers'] as const,
    QUOTATIONS: ['base', 'quotations'] as const,
    SUPPLIERS: ['base', 'suppliers'] as const,
    WARES: ['base', 'wares'] as const
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
    MENU_LIST: ['systemManage', 'menuList'] as const,
    MENU_TREE: ['systemManage', 'menuTree'] as const,
    ROLE_DETAIL: (id: string) => ['systemManage', 'roleDetail', id] as const,
    ROLE_LIST: (params?: Api.SystemManage.RoleSearchParams) => ['systemManage', 'roleList', params] as const,
    USER_LIST: (params?: Api.SystemManage.UserSearchParams) => ['systemManage', 'userList', params] as const
  }
} as const;

export const MUTATION_KEYS = {
  AUTH: {
    REFRESH_TOKEN: ['auth', 'refreshToken'] as const
  }
} as const;
