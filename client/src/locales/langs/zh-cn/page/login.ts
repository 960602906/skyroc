const login: App.I18n.Schema['translation']['page']['login'] = {
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
};

export default login;
