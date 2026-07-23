const login: App.I18n.Schema['translation']['page']['login'] = {
  bindWeChat: {
    title: 'Bind WeChat'
  },
  codeLogin: {
    getCode: 'Get verification code',
    imageCodePlaceholder: 'Please enter image verification code',
    reGetCode: 'Reacquire after {{time}}s',
    sendCodeSuccess: 'Verification code sent successfully',
    title: 'Verification Code Login'
  },
  common: {
    back: 'Back',
    codeLogin: 'Verification code login',
    codePlaceholder: 'Please enter verification code',
    confirm: 'Confirm',
    confirmPasswordPlaceholder: 'Please enter password again',
    loginOrRegister: 'Login / Register',
    loginSuccess: 'Login successfully',
    passwordPlaceholder: 'Please enter password',
    phonePlaceholder: 'Please enter phone number',
    userNamePlaceholder: 'Please enter user name',
    validateSuccess: 'Verification passed',
    welcomeBack: 'Welcome back, {{userName}} !'
  },
  pwdLogin: {
    admin: 'Admin',
    forgetPassword: 'Forget password?',
    otherAccountLogin: 'Other Account Login',
    otherLoginMode: 'Other Login Mode',
    register: 'Register',
    rememberMe: 'Remember me',
    superAdmin: 'Super Admin',
    title: 'Password Login',
    user: 'User'
  },
  register: {
    agreement: 'I have read and agree to',
    policy: '《Privacy Policy》',
    protocol: '《User Agreement》',
    title: 'Register'
  },
  resetPwd: {
    title: 'Reset Password'
  }
};

export default login;
