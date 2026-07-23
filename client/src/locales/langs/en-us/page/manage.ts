const manage: App.I18n.Schema['translation']['page']['manage'] = {
  common: {
    status: {
      disable: 'Disable',
      enable: 'Enable'
    }
  },
  menu: {
    activeMenu: 'Active Menu',
    addChildMenu: 'Add Child Menu',
    addMenu: 'Add Menu',
    button: 'Button',
    buttonCode: 'Button Code',
    buttonDesc: 'Button Desc',
    buttons: 'Button List',
    constant: 'Constant',
    editMenu: 'Edit Menu',
    fixedIndexInTab: 'Fixed Index In Tab',
    form: {
      activeMenu: 'Please select route name of the highlighted menu',
      button: 'Please select whether it is a button',
      buttonCode: 'Please enter button code',
      buttonDesc: 'Please enter button description',
      fixedIndexInTab: 'Please enter the index fixed in the tab',
      fixedInTab: 'Please select whether to fix in the tab',
      hideInMenu: 'Please select whether to hide menu',
      home: 'Please select home',
      href: 'Please enter href',
      i18nKey: 'Please enter i18n key',
      icon: 'Please enter iconify name',
      keepAlive: 'Please select whether to cache route',
      layout: 'Please select layout component',
      localIcon: 'Please enter local icon name',
      menuName: 'Please enter menu name',
      menuStatus: 'Please select menu status',
      menuType: 'Please select menu type',
      multiTab: 'Please select whether to support multiple tabs',
      order: 'Please enter order',
      page: 'Please select page component',
      parent: 'Please select whether to parent menu',
      pathParam: 'Please enter path param',
      queryKey: 'Please enter route parameter Key',
      queryValue: 'Please enter route parameter Value',
      routeName: 'Please enter route name',
      routePath: 'Please enter route path'
    },
    hideInMenu: 'Hide In Menu',
    home: 'Home',
    href: 'Href',
    i18nKey: 'I18n Key',
    icon: 'Icon',
    iconType: {
      iconify: 'Iconify Icon',
      local: 'Local Icon'
    },
    iconTypeTitle: 'Icon Type',
    id: 'ID',
    index: 'Index',
    keepAlive: 'Keep Alive',
    layout: 'Layout Component',
    localIcon: 'Local Icon',
    menuName: 'Menu Name',
    menuStatus: 'Menu Status',
    menuType: 'Menu Type',
    multiTab: 'Multi Tab',
    order: 'Order',
    page: 'Page Component',
    parent: 'Parent Menu',
    parentId: 'Parent ID',
    pathParam: 'Path Param',
    query: 'Query Params',
    routeName: 'Route Name',
    routePath: 'Route Path',
    title: 'Menu List',
    type: {
      directory: 'Directory',
      menu: 'Menu'
    }
  },
  role: {
    addRole: 'Add Role',
    buttonAuth: 'Button Auth',
    editRole: 'Edit Role',
    form: {
      roleCode: 'Please enter role code',
      roleDesc: 'Please enter role description',
      roleName: 'Please enter role name',
      roleStatus: 'Please select role status'
    },
    menuAuth: 'Menu Auth',
    roleCode: 'Role Code',
    roleDesc: 'Role Description',
    roleName: 'Role Name',
    roleStatus: 'Role Status',
    title: 'Role List'
  },
  roleDetail: {
    content: 'This page is solely for displaying all matched multi-level dynamic routes.',
    explain:
      '[...slug] is the syntax for matching all multi-level dynamic routes. The data is random and may not match.'
  },
  user: {
    addUser: 'Add User',
    editUser: 'Edit User',
    form: {
      nickName: 'Please enter nick name',
      userEmail: 'Please enter email',
      userGender: 'Please select gender',
      userName: 'Please enter user name',
      userPhone: 'Please enter phone number',
      userRole: 'Please select user role',
      userStatus: 'Please select user status'
    },
    gender: {
      female: 'Female',
      male: 'Male'
    },
    nickName: 'Nick Name',
    title: 'User List',
    userEmail: 'Email',
    userGender: 'Gender',
    userName: 'User Name',
    userPhone: 'Phone Number',
    userRole: 'User Role',
    userStatus: 'User Status'
  },
  userDetail: {
    content: `The loader allows network requests and lazy-loaded files to be triggered almost simultaneously, enabling the lazy-loaded files to be parsed while waiting for the network request to complete. Once the network request finishes, the page is displayed all at once. Leveraging React's Fiber architecture, if users find the waiting time too long, they can switch to different pages during the wait. This is an advantage of the React framework and React Router's data loader, as it avoids the conventional sequence of: request lazy-loaded file -> parse -> mount -> send network request -> render page -> display, and eliminates the need for manually adding a loading effect.`,
    explain: `This page is solely for demonstrating the powerful capabilities of react-router-dom's loader. The data is random and may not match.`
  }
};

export default manage;
