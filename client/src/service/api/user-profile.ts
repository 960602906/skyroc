import { request } from '../request';
import { USER_PROFILE_URLS } from '../urls';

/** 获取当前用户个人资料 */
export function fetchGetUserProfile(params?: Api.UserProfile.SearchParams) {
  return request<Api.UserProfile.Result>({
    method: 'get',
    params,
    url: USER_PROFILE_URLS.BASE
  });
}

/** 更新当前用户个人资料 */
export function fetchUpdateUserProfile(data?: Api.UserProfile.Payload) {
  return request<Api.UserProfile.Entity>({
    data,
    method: 'put',
    url: USER_PROFILE_URLS.BASE
  });
}

/** 修改当前用户密码 */
export function fetchUpdateUserPassword(data?: Api.UserProfile.Payload) {
  return request<Api.UserProfile.Entity>({
    data,
    method: 'put',
    url: USER_PROFILE_URLS.UPDATE_PWD
  });
}
