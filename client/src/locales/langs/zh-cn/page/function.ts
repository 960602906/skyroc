const functionPage: App.I18n.Schema['translation']['page']['function'] = {
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
};

export default functionPage;
