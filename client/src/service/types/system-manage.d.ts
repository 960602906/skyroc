/**
 * 命名空间 Api.SystemManage
 *
 * 后端 API 模块：系统管理模块（兼容别名，类型定义见各模块 types 文件）
 */
declare namespace Api {
  namespace SystemManage {
    /** 通用搜索参数 */
    type CommonSearchParams = Common.CommonSearchParams;

    /** 角色 */
    type Role = Api.Role.Entity;

    /** 角色搜索参数 */
    type RoleSearchParams = Api.Role.SearchParams;

    /** 角色列表 */
    type RoleList = Api.Role.List;

    /** 所有角色（简化版） */
    type AllRole = Api.Role.AllEntity;

    /**
     * 用户性别
     *
     * - "1": 男
     * - "2": 女
     */
    type UserGender = Api.User.UserGender;

    /** 用户 */
    type User = Api.User.Entity;

    /** 用户搜索参数 */
    type UserSearchParams = Api.User.SearchParams;

    /** 用户列表 */
    type UserList = Api.User.List;

    /**
     * 菜单类型
     *
     * - "1": 目录
     * - "2": 菜单
     */
    type MenuType = Api.Menu.MenuType;

    /** 菜单按钮 */
    type MenuButton = Api.MenuButton.Entity;

    /**
     * 图标类型
     *
     * - "1": iconify 图标
     * - "2": 本地图标
     */
    type IconType = Api.Menu.IconType;

    /** 菜单的路由属性 */
    type MenuPropsOfRoute = Api.Menu.MenuPropsOfRoute;

    /** 菜单 */
    type Menu = Api.Menu.Entity;

    /** 菜单列表 */
    type MenuList = Api.Menu.List;

    /** 菜单树 */
    type MenuTree = Api.Menu.TreeNode;
  }
}
