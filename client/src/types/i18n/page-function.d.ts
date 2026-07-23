declare namespace App {
  namespace I18n {
    interface PageFunction {
      multiTab: {
        backTab: string;
        routeParam: string;
      };
      request: {
        repeatedError: string;
        repeatedErrorMsg1: string;
        repeatedErrorMsg2: string;
        repeatedErrorOccurOnce: string;
      };
      tab: {
        tabOperate: {
          addMultiTab: string;
          addMultiTabDesc1: string;
          addMultiTabDesc2: string;
          addTab: string;
          addTabDesc: string;
          closeAboutTab: string;
          closeCurrentTab: string;
          closeTab: string;
          title: string;
        };
        tabTitle: {
          change: string;
          changeTitle: string;
          reset: string;
          resetTitle: string;
          title: string;
        };
      };
      toggleAuth: {
        adminOrUserVisible: string;
        adminVisible: string;
        authHook: string;
        superAdminVisible: string;
        toggleAccount: string;
      };
    }
  }
}
