const functionPage: App.I18n.Schema['translation']['page']['function'] = {
  multiTab: {
    backTab: 'Back function_tab',
    routeParam: 'Route Param'
  },
  request: {
    repeatedError: 'Repeated Request Error',
    repeatedErrorMsg1: 'Custom Request Error 1',
    repeatedErrorMsg2: 'Custom Request Error 2',
    repeatedErrorOccurOnce: 'Repeated Request Error Occurs Once'
  },
  tab: {
    tabOperate: {
      addMultiTab: 'Add Multi Tab',
      addMultiTabDesc1: 'To MultiTab page',
      addMultiTabDesc2: 'To MultiTab page(with query params)',
      addTab: 'Add Tab',
      addTabDesc: 'To about page',
      closeAboutTab: 'Close "About" Tab',
      closeCurrentTab: 'Close Current Tab',
      closeTab: 'Close Tab',
      title: 'Tab Operation'
    },
    tabTitle: {
      change: 'Change',
      changeTitle: 'Change Title',
      reset: 'Reset',
      resetTitle: 'Reset Title',
      title: 'Tab Title'
    }
  },
  toggleAuth: {
    adminOrUserVisible: 'Admin and User Visible',
    adminVisible: 'Admin Visible',
    authHook: 'Auth Hook Function `hasAuth`',
    superAdminVisible: 'Super Admin Visible',
    toggleAccount: 'Toggle Account'
  }
};

export default functionPage;
