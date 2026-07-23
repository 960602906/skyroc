const manage: App.I18n.Schema['translation']['page']['manage'] = {
  common: {
    status: {
      disable: '禁用',
      enable: '启用'
    }
  },
  menu: {
    activeMenu: '高亮的菜单',
    addChildMenu: '新增子菜单',
    addMenu: '新增菜单',
    button: '按钮',
    buttonCode: '按钮编码',
    buttonDesc: '按钮描述',
    buttons: '按钮列表',
    constant: '常量路由',
    editMenu: '编辑菜单',
    fixedIndexInTab: '固定在页签中的序号',
    form: {
      activeMenu: '请选择高亮的菜单的路由名称',
      button: '请选择是否按钮',
      buttonCode: '请输入按钮编码',
      buttonDesc: '请输入按钮描述',
      fixedIndexInTab: '请输入固定在页签中的序号',
      fixedInTab: '请选择是否固定在页签中',
      hideInMenu: '请选择是否隐藏菜单',
      home: '请选择首页',
      href: '请输入外链',
      i18nKey: '请输入国际化key',
      icon: '请输入图标',
      keepAlive: '请选择是否缓存路由',
      layout: '请选择布局组件',
      localIcon: '请选择本地图标',
      menuName: '请输入菜单名称',
      menuStatus: '请选择菜单状态',
      menuType: '请选择菜单类型',
      multiTab: '请选择是否支持多标签',
      order: '请输入排序',
      page: '请选择页面组件',
      parent: '请选择父级菜单',
      pathParam: '请输入路径参数',
      queryKey: '请输入路由参数Key',
      queryValue: '请输入路由参数Value',
      routeName: '请输入路由名称',
      routePath: '请输入路由路径'
    },
    hideInMenu: '隐藏菜单',
    home: '首页',
    href: '外链',
    i18nKey: '国际化key',
    icon: '图标',
    iconType: {
      iconify: 'iconify图标',
      local: '本地图标'
    },
    iconTypeTitle: '图标类型',
    id: 'ID',
    index: '序号',
    keepAlive: '缓存路由',
    layout: '布局',
    localIcon: '本地图标',
    menuName: '菜单名称',
    menuStatus: '菜单状态',
    menuType: '菜单类型',
    multiTab: '支持多页签',
    order: '排序',
    page: '页面组件',
    parent: '父级菜单',
    parentId: '父级菜单ID',
    pathParam: '路径参数',
    query: '路由参数',
    routeName: '路由名称',
    routePath: '路由路径',
    title: '菜单列表',
    type: {
      directory: '目录',
      menu: '菜单'
    }
  },
  role: {
    addRole: '新增角色',
    buttonAuth: '按钮权限',
    editRole: '编辑角色',
    form: {
      roleCode: '请输入角色编码',
      roleDesc: '请输入角色描述',
      roleName: '请输入角色名称',
      roleStatus: '请选择角色状态'
    },
    menuAuth: '菜单权限',
    roleCode: '角色编码',
    roleDesc: '角色描述',
    roleName: '角色名称',
    roleStatus: '角色状态',
    title: '角色列表'
  },
  roleDetail: {
    content: '这个页面仅仅是为了展示匹配到所有多级动态路由',
    explain:
      '[...slug] 是匹配所有多级动态路由的语法 以[...any]为格式,匹配到的数据会在useRoute的params中以数组的形式存在'
  },
  user: {
    addUser: '新增用户',
    editUser: '编辑用户',
    form: {
      nickName: '请输入昵称',
      userEmail: '请输入邮箱',
      userGender: '请选择性别',
      userName: '请输入用户名',
      userPhone: '请输入手机号',
      userRole: '请选择用户角色',
      userStatus: '请选择用户状态'
    },
    gender: {
      female: '女',
      male: '男'
    },
    nickName: '昵称',
    title: '用户列表',
    userEmail: '邮箱',
    userGender: '性别',
    userName: '用户名',
    userPhone: '手机号',
    userRole: '用户角色',
    userStatus: '用户状态'
  },
  userDetail: {
    content: `loader 会让网络请求跟懒加载的文件几乎一起发出请求 然后 一边解析懒加载的文件 一边去等待 网络请求
        待到网络请求完成页面 一起显示 配合react的fiber架构 可以做到 用户如果嫌弃等待时间较长 在等待期间用户可以去
        切换不同的页面 这是react 框架和react-router数据路由器的优势 而不用非得等到 页面的显现 而不是常规的
        请求懒加载的文件 - 解析 - 请求懒加载的文件 - 挂载之后去发出网络请求 - 然后渲染页面 - 渲染完成
        还要自己加loading效果`,
    explain: '这个页面仅仅是为了展示 react-router-dom 的 loader 的强大能力，数据是随机的对不上很正常'
  }
};

export default manage;
