declare namespace App {
  namespace I18n {
    interface PageLogin {
      bindWeChat: {
        title: string;
      };
      codeLogin: {
        getCode: string;
        imageCodePlaceholder: string;
        reGetCode: string;
        sendCodeSuccess: string;
        title: string;
      };
      common: {
        back: string;
        codeLogin: string;
        codePlaceholder: string;
        confirm: string;
        confirmPasswordPlaceholder: string;
        loginOrRegister: string;
        loginSuccess: string;
        passwordPlaceholder: string;
        phonePlaceholder: string;
        userNamePlaceholder: string;
        validateSuccess: string;
        welcomeBack: string;
      };
      pwdLogin: {
        admin: string;
        forgetPassword: string;
        otherAccountLogin: string;
        otherLoginMode: string;
        register: string;
        rememberMe: string;
        superAdmin: string;
        title: string;
        user: string;
      };
      register: {
        agreement: string;
        policy: string;
        protocol: string;
        title: string;
      };
      resetPwd: {
        title: string;
      };
    }
  }
}
