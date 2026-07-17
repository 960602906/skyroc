import { request } from '../request';
import { LOG_URLS } from '../urls';

/** 分页查询登录审计日志。 */
export function fetchGetLogLogins(params?: Api.Log.SearchParams) {
  return request<Api.Log.Result>({
    method: 'get',
    params,
    url: LOG_URLS.LOGINS
  });
}

/** 分页查询关键操作审计日志。 */
export function fetchGetLogOperations(params?: Api.Log.SearchParams) {
  return request<Api.Log.Result>({
    method: 'get',
    params,
    url: LOG_URLS.OPERATIONS
  });
}
