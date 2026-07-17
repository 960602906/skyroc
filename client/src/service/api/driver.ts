import { request } from '../request';
import { DRIVER_URLS } from '../urls';

/** 查询全部。 */
export function fetchGetAllDrivers() {
  return request<Api.Driver.AllEntity[]>({
    method: 'get',
    url: DRIVER_URLS.BASE
  });
}

/** 创建。 */
export function fetchAddDriver(data?: Api.Driver.Payload) {
  return request<Api.Driver.Entity>({
    data,
    method: 'post',
    url: DRIVER_URLS.BASE
  });
}

/** 更新。 */
export function fetchUpdateDriver(data?: Api.Driver.Payload) {
  return request<Api.Driver.Entity>({
    data,
    method: 'put',
    url: DRIVER_URLS.BASE
  });
}

/** 批量删除。 */
export function fetchBatchDeleteDriver(ids: string[]) {
  return request<Api.Driver.Entity>({
    data: ids,
    method: 'delete',
    url: DRIVER_URLS.BATCH_DELETE
  });
}

/** 分页查询。 */
export function fetchGetDriverList(params?: Api.Driver.SearchParams) {
  return request<Api.Driver.List>({
    method: 'get',
    params,
    url: DRIVER_URLS.LIST
  });
}

/** 删除。 */
export function fetchDeleteDriver(id: string) {
  return request<Api.Driver.Entity>({
    method: 'delete',
    url: `${DRIVER_URLS.BASE}/${id}`
  });
}

/** 根据 ID 查询。 */
export function fetchGetDriverDetail(id: string) {
  return request<Api.Driver.Entity>({
    method: 'get',
    url: `${DRIVER_URLS.BASE}/${id}`
  });
}

/** 启用或禁用。 */
export function fetchToggleDriverStatus(params: Api.Base.ToggleStatusParams) {
  return request<Api.Driver.Entity>({
    method: 'patch',
    params: { status: params.status },
    url: `${DRIVER_URLS.BASE}/${params.id}/status`
  });
}
