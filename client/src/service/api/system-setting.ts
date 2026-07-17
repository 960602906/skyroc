import { request } from '../request';
import { SYSTEM_SETTING_URLS } from '../urls';

/** 读取小程序下单全局设置。 */
export function fetchGetSystemSettingMiniProgramOrder(params?: Api.SystemSetting.SearchParams) {
  return request<Api.SystemSetting.Result>({
    method: 'get',
    params,
    url: SYSTEM_SETTING_URLS.MINI_PROGRAM_ORDER
  });
}

/** 保存小程序下单全局设置。 */
export function fetchUpdateSystemSettingMiniProgramOrder(data?: Api.SystemSetting.Payload) {
  return request<Api.SystemSetting.Entity>({
    data,
    method: 'put',
    url: SYSTEM_SETTING_URLS.MINI_PROGRAM_ORDER
  });
}

/** 查询运营服务时段。 */
export function fetchGetSystemSettingServicePeriods(params?: Api.SystemSetting.SearchParams) {
  return request<Api.SystemSetting.Result>({
    method: 'get',
    params,
    url: SYSTEM_SETTING_URLS.SERVICE_PERIODS
  });
}

/** 新增运营服务时段。 */
export function fetchServicePeriodsSystemSetting(data?: Api.SystemSetting.Payload) {
  return request<Api.SystemSetting.Entity>({
    data,
    method: 'post',
    url: SYSTEM_SETTING_URLS.SERVICE_PERIODS
  });
}

/** 删除运营服务时段。 */
export function fetchDeleteSystemSettingServicePeriods(id: string) {
  return request<Api.SystemSetting.Entity>({
    method: 'delete',
    url: `${SYSTEM_SETTING_URLS.SERVICE_PERIODS}/${id}`
  });
}

/** 查询单个运营服务时段。 */
export function fetchGetSystemSettingServicePeriodsDetail(id: string) {
  return request<Api.SystemSetting.Entity>({
    method: 'get',
    url: `${SYSTEM_SETTING_URLS.SERVICE_PERIODS}/${id}`
  });
}

/** 完整更新运营服务时段。 */
export function fetchUpdateSystemSettingServicePeriods(id: string, data?: Api.SystemSetting.Payload) {
  return request<Api.SystemSetting.Entity>({
    data,
    method: 'put',
    url: `${SYSTEM_SETTING_URLS.SERVICE_PERIODS}/${id}`
  });
}

/** 读取分拣排序权重设置。 */
export function fetchGetSystemSettingSortingWeights(params?: Api.SystemSetting.SearchParams) {
  return request<Api.SystemSetting.Result>({
    method: 'get',
    params,
    url: SYSTEM_SETTING_URLS.SORTING_WEIGHTS
  });
}

/** 保存分拣排序权重设置。 */
export function fetchUpdateSystemSettingSortingWeights(data?: Api.SystemSetting.Payload) {
  return request<Api.SystemSetting.Entity>({
    data,
    method: 'put',
    url: SYSTEM_SETTING_URLS.SORTING_WEIGHTS
  });
}
