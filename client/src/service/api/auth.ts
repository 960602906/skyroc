import { request } from '../request';
import { AUTH_URLS } from '../urls';

/** 登录 */
export function fetchLogin(params: Api.Auth.LoginParams) {
  return request<Api.Auth.LoginResponse>({
    data: params,
    method: 'post',
    url: AUTH_URLS.LOGIN
  });
}

/** 获取用户信息 */
export function fetchGetUserInfo() {
  return request<Api.Auth.UserInfo>({ url: AUTH_URLS.GET_USER_INFO });
}

/** 获取路由信息 */
export function fetchGetRoutes() {
  return request<Api.Auth.Routes>({ url: AUTH_URLS.GET_ROUTES });
}

/** 刷新令牌 */
export function fetchRefreshToken(refreshToken: string) {
  return request<Api.Auth.LoginToken>({
    data: {
      refreshToken
    } satisfies Api.Auth.RefreshTokenParams,
    method: 'post',
    url: AUTH_URLS.REFRESH_TOKEN
  });
}

/** 注销登录 */
export function fetchLogout() {
  return request<null>({
    method: 'post',
    url: AUTH_URLS.LOGOUT
  });
}

/**
 * return custom backend error
 *
 * @param code error code
 * @param msg error message
 */
export function fetchCustomBackendError(code: string, msg: string) {
  return request({ params: { code, msg }, url: AUTH_URLS.ERROR });
}
