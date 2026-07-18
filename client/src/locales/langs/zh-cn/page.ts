const page: App.I18n.Schema['translation']['page'] = {
  about: {
    devDep: '开发依赖',
    introduction: `SkyrocAdmin 是一个优雅且功能强大的后台管理模板，基于最新的前端技术栈，包括 React19.0, Vite6, TypeScript,ReactRouter7,Redux/toolkit 和 UnoCSS。它内置了丰富的主题配置和组件，代码规范严谨，实现了自动化的文件路由系统。此外，它还采用了基于 ApiFox 的在线Mock数据方案。SkyrocAdmin 为您提供了一站式的后台管理解决方案，无需额外配置，开箱即用。同样是一个快速学习前沿技术的最佳实践。`,
    prdDep: '生产依赖',
    projectInfo: {
      githubLink: 'Github 地址',
      latestBuildTime: '最新构建时间',
      previewLink: '预览地址',
      title: '项目信息',
      version: '版本'
    },
    title: '关于'
  },
  customer: {
    company: {
      addCompany: '新增公司',
      address: '地址',
      code: '公司编码',
      contactName: '联系人',
      contactPhone: '联系电话',
      createTime: '创建时间',
      detail: {
        back: '返回列表',
        createTime: '创建时间',
        title: '公司详情',
        updateTime: '更新时间'
      },
      editCompany: '编辑公司',
      form: {
        address: '请输入地址',
        code: '请输入公司编码',
        contactName: '请输入联系人',
        contactPhone: '请输入联系电话',
        name: '请输入公司名称',
        remark: '请输入备注',
        status: '请选择状态'
      },
      name: '公司名称',
      remark: '备注',
      sectionBasic: '基础信息',
      sectionStatus: '状态与备注',
      status: '状态',
      title: '公司资料'
    },
    detail: {
      back: '返回列表',
      title: '客户详情',
      updateTime: '更新时间'
    },
    list: {
      addCustomer: '新增客户',
      address: '地址',
      code: '客户编码',
      companyId: '所属公司',
      contactName: '联系人',
      contactPhone: '联系电话',
      createTime: '创建时间',
      editCustomer: '编辑客户',
      form: {
        address: '请输入地址',
        code: '请输入客户编码',
        companyId: '请选择所属公司',
        contactName: '请输入联系人',
        contactPhone: '请输入联系电话',
        name: '请输入客户名称',
        status: '请选择状态'
      },
      name: '客户名称',
      status: '状态',
      title: '客户列表'
    },
    operate: {
      addTitle: '新增客户',
      bankAccount: '银行账号',
      bankName: '开户银行',
      businessScope: '经营范围',
      businessTerm: '营业期限',
      defaultWareId: '默认仓库',
      editTitle: '编辑客户',
      establishDate: '成立日期',
      form: {
        bankAccount: '请输入银行账号',
        bankName: '请输入开户银行',
        businessScope: '请输入经营范围',
        businessTerm: '请输入营业期限',
        defaultWareId: '请选择默认仓库',
        establishDate: '请选择成立日期',
        invoiceAddress: '请输入开票地址',
        invoiceEmail: '请输入开票邮箱',
        invoicePhone: '请输入开票电话',
        invoiceReceiverAddress: '请输入收票地址',
        invoiceReceiverName: '请输入收票人',
        invoiceReceiverPhone: '请输入收票电话',
        invoiceTitle: '请输入发票抬头',
        legalRepresentative: '请输入法定代表人',
        quotationId: '请选择报价单',
        registeredAddress: '请输入注册地址',
        registeredCapital: '请输入注册资本',
        registrationAuthority: '请输入登记机关',
        registrationStatus: '请输入登记状态',
        remark: '请输入备注',
        tagIds: '请选择客户标签',
        taxpayerIdentificationNumber: '请输入纳税人识别号',
        unifiedSocialCreditCode: '请输入统一社会信用代码'
      },
      invoiceAddress: '开票地址',
      invoiceEmail: '开票邮箱',
      invoicePhone: '开票电话',
      invoiceReceiverAddress: '收票地址',
      invoiceReceiverName: '收票人',
      invoiceReceiverPhone: '收票电话',
      invoiceTitle: '发票抬头',
      legalRepresentative: '法定代表人',
      quotationId: '报价单',
      registeredAddress: '注册地址',
      registeredCapital: '注册资本',
      registrationAuthority: '登记机关',
      registrationStatus: '登记状态',
      remark: '备注',
      sectionBasic: '基础信息',
      sectionBusiness: '工商信息',
      sectionInvoice: '开票信息',
      tagIds: '客户标签',
      taxpayerIdentificationNumber: '纳税人识别号',
      unifiedSocialCreditCode: '统一社会信用代码'
    },
    protocol: {
      addProtocol: '新增协议',
      code: '协议编码',
      createTime: '创建时间',
      customerIds: '关联客户',
      detail: {
        back: '返回列表',
        createTime: '创建时间',
        title: '协议详情',
        updateTime: '更新时间'
      },
      editProtocol: '编辑协议',
      effectiveEnd: '失效日期',
      effectiveStart: '生效日期',
      form: {
        code: '请输入协议编码',
        customerIds: '请选择关联客户',
        name: '请输入协议名称',
        quotationId: '请选择报价单',
        remark: '请输入备注',
        status: '请选择状态'
      },
      manageGoods: '维护商品',
      name: '协议名称',
      quotationId: '报价单',
      remark: '备注',
      sectionBasic: '基础信息',
      sectionGoods: '协议商品',
      sectionStatus: '状态与备注',
      status: '状态',
      title: '客户协议'
    },
    protocolGoods: {
      add: '新增协议商品',
      addProtocolGoods: '新增协议商品',
      createTime: '创建时间',
      customerProtocolId: '客户协议',
      editProtocolGoods: '编辑协议商品',
      form: {
        customerProtocolId: '请选择客户协议',
        goodsId: '请选择商品',
        goodsUnitId: '请选择商品单位',
        minOrderQuantity: '请输入最小起订量',
        protocolPrice: '请输入协议价',
        remark: '请输入备注'
      },
      goodsId: '商品',
      goodsUnitId: '商品单位',
      minOrderQuantity: '最小起订量',
      protocolPrice: '协议价',
      remark: '备注',
      status: '状态',
      title: '协议商品'
    },
    subAccount: {
      addSubAccount: '新增子账号',
      companyId: '所属公司',
      createTime: '创建时间',
      customerId: '关联客户',
      detail: {
        back: '返回列表',
        createTime: '创建时间',
        title: '子账号详情',
        updateTime: '更新时间'
      },
      editSubAccount: '编辑子账号',
      email: '邮箱',
      form: {
        companyId: '请选择所属公司',
        customerId: '请选择关联客户',
        email: '请输入邮箱',
        nickName: '请输入昵称',
        phone: '请输入手机号',
        remark: '请输入备注',
        status: '请选择状态',
        username: '请输入用户名'
      },
      nickName: '昵称',
      phone: '手机号',
      remark: '备注',
      sectionBasic: '基础信息',
      sectionStatus: '状态与备注',
      status: '状态',
      title: '子账号管理',
      username: '用户名'
    },
    tag: {
      addTag: '新增标签',
      code: '标签编码',
      createTime: '创建时间',
      detail: {
        back: '返回列表',
        createTime: '创建时间',
        title: '标签详情',
        updateTime: '更新时间'
      },
      editTag: '编辑标签',
      form: {
        code: '请输入标签编码',
        name: '请输入标签名称',
        parentId: '请选择父级标签',
        remark: '请输入备注',
        sort: '请输入排序',
        status: '请选择状态'
      },
      name: '标签名称',
      parentId: '父级标签',
      remark: '备注',
      sectionBasic: '基础信息',
      sectionStatus: '状态与备注',
      sort: '排序',
      status: '状态',
      title: '客户标签'
    }
  },
  function: {
    multiTab: {
      backTab: '返回 function_tab',
      routeParam: '路由参数'
    },
    request: {
      repeatedError: '重复请求错误',
      repeatedErrorMsg1: '自定义请求错误 1',
      repeatedErrorMsg2: '自定义请求错误 2',
      repeatedErrorOccurOnce: '重复请求错误只出现一次'
    },
    tab: {
      tabOperate: {
        addMultiTab: '添加多标签页',
        addMultiTabDesc1: '跳转到多标签页页面',
        addMultiTabDesc2: '跳转到多标签页页面(带有查询参数)',
        addTab: '添加标签页',
        addTabDesc: '跳转到关于页面',
        closeAboutTab: '关闭"关于"标签页',
        closeCurrentTab: '关闭当前标签页',
        closeTab: '关闭标签页',
        title: '标签页操作'
      },
      tabTitle: {
        change: '修改',
        changeTitle: '修改标题',
        reset: '重置',
        resetTitle: '重置标题',
        title: '标签页标题'
      }
    },
    toggleAuth: {
      adminOrUserVisible: '管理员和用户可见',
      adminVisible: '管理员可见',
      authHook: '权限钩子函数 `hasAuth`',
      superAdminVisible: '超级管理员可见',
      toggleAccount: '切换账号'
    }
  },
  goods: {
    detail: {
      back: '返回列表',
      title: '商品详情',
      updateTime: '更新时间'
    },
    list: {
      brand: '品牌',
      code: '商品编码',
      createTime: '创建时间',
      defaultSupplierId: '默认供应商',
      defaultWareId: '默认仓库',
      form: {
        code: '请输入商品编码',
        defaultSupplierId: '请选择默认供应商',
        defaultWareId: '请选择默认仓库',
        goodsTypeId: '请选择商品分类',
        isOnSale: '请选择上下架状态',
        name: '请输入商品名称',
        status: '请选择状态'
      },
      goodsTypeId: '商品分类',
      isOnSale: '上下架',
      name: '商品名称',
      offSale: '下架',
      onSale: '上架',
      spec: '规格',
      status: '状态',
      title: '商品列表'
    },
    operate: {
      addTitle: '新增商品',
      baseUnitId: '基本单位',
      brand: '品牌',
      code: '商品编码',
      defaultSupplierId: '默认供应商',
      defaultWareId: '默认仓库',
      description: '描述',
      editTitle: '编辑商品',
      form: {
        baseUnitId: '请选择基本单位',
        brand: '请输入品牌',
        code: '请输入商品编码',
        defaultSupplierId: '请选择默认供应商',
        defaultWareId: '请选择默认仓库',
        description: '请输入描述',
        goodsTypeId: '请选择商品分类',
        name: '请输入商品名称',
        origin: '请输入产地',
        remark: '请输入备注',
        spec: '请输入规格',
        supplierIds: '请选择关联供应商',
        taxRate: '请输入税率'
      },
      goodsTypeId: '商品分类',
      isOnSale: '是否上架',
      name: '商品名称',
      origin: '产地',
      remark: '备注',
      sectionBasic: '基础信息',
      sectionSale: '销售与状态',
      sectionSupply: '计量与供应',
      spec: '规格',
      status: '状态',
      supplierIds: '关联供应商',
      taxRate: '税率'
    },
    quotation: {
      add: '新增报价单',
      audit: '审核',
      audited: '已审核',
      code: '报价单编码',
      customerIds: '关联客户',
      description: '描述',
      detail: {
        back: '返回列表',
        createTime: '创建时间',
        title: '报价单详情',
        updateTime: '更新时间'
      },
      edit: '编辑报价单',
      effectiveEnd: '生效结束',
      effectiveStart: '生效开始',
      form: {
        code: '请输入报价单编码',
        customerIds: '请选择客户',
        description: '请输入描述',
        isAudited: '请选择审核状态',
        name: '请输入报价单名称',
        remark: '请输入备注',
        status: '请选择状态'
      },
      isAudited: '审核状态',
      manageGoods: '维护商品',
      name: '报价单名称',
      remark: '备注',
      sectionBasic: '基础信息',
      sectionGoods: '报价商品',
      sectionStatus: '状态与备注',
      status: '状态',
      title: '报价单',
      unaudit: '反审核',
      unaudited: '未审核'
    },
    quotationGoods: {
      add: '新增报价商品',
      edit: '编辑报价商品',
      form: {
        goodsId: '请选择商品',
        goodsUnitId: '请选择商品单位',
        isOnSale: '请选择上下架',
        minOrderQuantity: '请输入起订量',
        quotationId: '请选择报价单',
        remark: '请输入备注',
        unitPrice: '请输入单价'
      },
      goodsId: '商品',
      goodsUnitId: '商品单位',
      isOnSale: '上下架',
      minOrderQuantity: '起订量',
      quotationId: '报价单',
      remark: '备注',
      title: '报价商品',
      unitPrice: '单价'
    },
    type: {
      add: '新增商品分类',
      code: '分类编码',
      defaultTaxRate: '默认税率',
      detail: {
        back: '返回列表',
        createTime: '创建时间',
        title: '分类详情',
        updateTime: '更新时间'
      },
      edit: '编辑商品分类',
      form: {
        code: '请输入分类编码',
        defaultTaxRate: '请输入默认税率',
        invoiceGoodsShortName: '请输入开票简称',
        name: '请输入分类名称',
        parentId: '请选择上级分类',
        remark: '请输入备注',
        sort: '请输入排序',
        status: '请选择状态',
        taxCategoryCode: '请输入税收分类编码',
        taxCategoryName: '请输入税收分类名称',
        taxPolicyBasis: '请输入税收政策依据'
      },
      invoiceGoodsShortName: '开票简称',
      isTaxExempt: '免税',
      name: '分类名称',
      parentId: '上级分类',
      remark: '备注',
      sectionBasic: '基础信息',
      sectionStatus: '状态与备注',
      sectionTax: '税务信息',
      sort: '排序',
      status: '状态',
      taxCategoryCode: '税收分类编码',
      taxCategoryName: '税收分类名称',
      taxPolicyBasis: '税收政策依据',
      title: '商品分类'
    },
    unit: {
      add: '新增商品单位',
      code: '单位编码',
      conversionRate: '换算率',
      detail: {
        back: '返回列表',
        createTime: '创建时间',
        title: '单位详情',
        updateTime: '更新时间'
      },
      edit: '编辑商品单位',
      form: {
        code: '请输入单位编码',
        conversionRate: '请输入换算率',
        goodsId: '请选择商品',
        name: '请输入单位名称',
        remark: '请输入备注',
        sort: '请输入排序',
        status: '请选择状态'
      },
      goodsCode: '商品编码',
      goodsId: '商品',
      isBaseUnit: '基本单位',
      name: '单位名称',
      remark: '备注',
      sectionBasic: '基础信息',
      sectionStatus: '状态与备注',
      sort: '排序',
      status: '状态',
      title: '商品单位'
    }
  },
  home: {
    creativity: '创意',
    dealCount: '成交量',
    downloadCount: '下载量',
    entertainment: '娱乐',
    greeting: '早安，{{userName}}, 今天又是充满活力的一天!',
    message: '消息',
    projectCount: '项目数',
    projectNews: {
      desc1: 'Skyroc 在2021年5月28日创建了开源项目 skyroc-admin!',
      desc2: 'Yanbowe 向 skyroc-admin 提交了一个bug，多标签栏不会自适应。',
      desc3: 'Skyroc 准备为 skyroc-admin 的发布做充分的准备工作!',
      desc4: 'Skyroc 正在忙于为skyroc-admin写项目说明文档！',
      desc5: 'Skyroc 刚才把工作台页面随便写了一些，凑合能看了！',
      moreNews: '更多动态',
      title: '项目动态'
    },
    registerCount: '注册量',
    rest: '休息',
    schedule: '作息安排',
    study: '学习',
    todo: '待办',
    turnover: '成交额',
    visitCount: '访问量',
    weatherDesc: '今日多云转晴，20℃ - 25℃!',
    work: '工作'
  },
  login: {
    bindWeChat: {
      title: '绑定微信'
    },
    codeLogin: {
      getCode: '获取验证码',
      imageCodePlaceholder: '请输入图片验证码',
      reGetCode: '{{time}}秒后重新获取',
      sendCodeSuccess: '验证码发送成功',
      title: '验证码登录'
    },
    common: {
      back: '返回',
      codeLogin: '验证码登录',
      codePlaceholder: '请输入验证码',
      confirm: '确定',
      confirmPasswordPlaceholder: '请再次输入密码',
      loginOrRegister: '登录 / 注册',
      loginSuccess: '登录成功',
      passwordPlaceholder: '请输入密码',
      phonePlaceholder: '请输入手机号',
      userNamePlaceholder: '请输入用户名',
      validateSuccess: '验证成功',
      welcomeBack: '欢迎回来，{{userName}} ！'
    },
    pwdLogin: {
      admin: '管理员',
      forgetPassword: '忘记密码？',
      otherAccountLogin: '其他账号登录',
      otherLoginMode: '其他登录方式',
      register: '注册账号',
      rememberMe: '记住我',
      superAdmin: '超级管理员',
      title: '密码登录',
      user: '普通用户'
    },
    register: {
      agreement: '我已经仔细阅读并接受',
      policy: '《隐私权政策》',
      protocol: '《用户协议》',
      title: '注册账号'
    },
    resetPwd: {
      title: '重置密码'
    }
  },
  manage: {
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
  },
  purchase: {
    purchaser: {
      addPurchaser: '新增采购员',
      code: '采购员编码',
      createTime: '创建时间',
      departmentId: '所属部门',
      detail: {
        back: '返回列表',
        createTime: '创建时间',
        title: '采购员详情',
        updateTime: '更新时间'
      },
      editPurchaser: '编辑采购员',
      form: {
        code: '请输入采购员编码',
        departmentId: '请选择所属部门',
        name: '请输入采购员名称',
        phone: '请输入手机号',
        remark: '请输入备注',
        status: '请选择状态'
      },
      name: '采购员名称',
      phone: '手机号',
      remark: '备注',
      sectionBasic: '基础信息',
      sectionStatus: '状态与备注',
      status: '状态',
      title: '采购员列表',
      userName: '关联用户'
    },
    rule: {
      addRule: '新增采购规则',
      code: '规则编码',
      createTime: '创建时间',
      editRule: '编辑采购规则',
      form: {
        code: '请输入规则编码',
        goodsTypeId: '请选择商品分类',
        name: '请输入规则名称',
        purchasePattern: '请选择采购模式',
        purchaserId: '请选择采购员',
        remark: '请输入备注',
        status: '请选择状态',
        supplierId: '请选择供应商',
        wareId: '请选择仓库'
      },
      goodsTypeId: '商品分类',
      name: '规则名称',
      purchasePattern: '采购模式',
      purchasePatternDirect: '供应商直供',
      purchasePatternMarket: '市场自采',
      purchaserId: '采购员',
      remark: '备注',
      status: '状态',
      supplierId: '供应商',
      title: '采购规则列表',
      wareId: '仓库'
    },
    supplier: {
      address: '地址',
      addSupplier: '新增供应商',
      bankAccount: '银行账号',
      bankName: '开户银行',
      code: '供应商编码',
      contactName: '联系人',
      contactPhone: '联系电话',
      createTime: '创建时间',
      detail: {
        back: '返回列表',
        createTime: '创建时间',
        title: '供应商详情',
        updateTime: '更新时间'
      },
      editSupplier: '编辑供应商',
      form: {
        address: '请输入地址',
        bankAccount: '请输入银行账号',
        bankName: '请输入开户银行',
        code: '请输入供应商编码',
        contactName: '请输入联系人',
        contactPhone: '请输入联系电话',
        name: '请输入供应商名称',
        remark: '请输入备注',
        status: '请选择状态',
        taxNo: '请输入税号'
      },
      name: '供应商名称',
      remark: '备注',
      sectionBasic: '基础信息',
      sectionFinance: '财务信息',
      sectionStatus: '状态与备注',
      status: '状态',
      taxNo: '税号',
      title: '供应商列表'
    }
  },
  storage: {
    ware: {
      address: '地址',
      addWare: '新增仓库',
      code: '仓库编码',
      contactName: '联系人',
      contactPhone: '联系电话',
      createTime: '创建时间',
      editWare: '编辑仓库',
      form: {
        address: '请输入地址',
        code: '请输入仓库编码',
        contactName: '请输入联系人',
        contactPhone: '请输入联系电话',
        name: '请输入仓库名称',
        remark: '请输入备注',
        sort: '请输入排序',
        status: '请选择状态'
      },
      name: '仓库名称',
      remark: '备注',
      sort: '排序',
      status: '状态',
      title: '仓库列表'
    }
  }
};

export default page;
