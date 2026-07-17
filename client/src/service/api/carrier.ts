import { request } from '../request';
import { CARRIER_URLS } from '../urls';

/** 查询全部。 */
export function fetchGetAllCarriers() {
  return request<Api.Carrier.AllEntity[]>({
    method: 'get',
    url: CARRIER_URLS.BASE
  });
}

/** 创建。 */
export function fetchAddCarrier(data?: Api.Carrier.Payload) {
  return request<Api.Carrier.Entity>({
    data,
    method: 'post',
    url: CARRIER_URLS.BASE
  });
}

/** 更新。 */
export function fetchUpdateCarrier(data?: Api.Carrier.Payload) {
  return request<Api.Carrier.Entity>({
    data,
    method: 'put',
    url: CARRIER_URLS.BASE
  });
}

/** 批量删除。 */
export function fetchBatchDeleteCarrier(ids: string[]) {
  return request<Api.Carrier.Entity>({
    data: ids,
    method: 'delete',
    url: CARRIER_URLS.BATCH_DELETE
  });
}

/** 分页查询。 */
export function fetchGetCarrierList(params?: Api.Carrier.SearchParams) {
  return request<Api.Carrier.List>({
    method: 'get',
    params,
    url: CARRIER_URLS.LIST
  });
}

/** 删除。 */
export function fetchDeleteCarrier(id: string) {
  return request<Api.Carrier.Entity>({
    method: 'delete',
    url: `${CARRIER_URLS.BASE}/${id}`
  });
}

/** 根据 ID 查询。 */
export function fetchGetCarrierDetail(id: string) {
  return request<Api.Carrier.Entity>({
    method: 'get',
    url: `${CARRIER_URLS.BASE}/${id}`
  });
}

/** 启用或禁用。 */
export function fetchToggleCarrierStatus(params: Api.Base.ToggleStatusParams) {
  return request<Api.Carrier.Entity>({
    method: 'patch',
    params: { status: params.status },
    url: `${CARRIER_URLS.BASE}/${params.id}/status`
  });
}
