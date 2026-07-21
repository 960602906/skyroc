import { request } from '../request';
import { CUSTOMER_PROTOCOL_URLS } from '../urls';

import { withSelectionOptionInvalidation } from './selection-option';

/** 分页查询。 */
export function fetchGetCustomerProtocolList(params?: Api.CustomerProtocol.SearchParams) {
  return request<Api.CustomerProtocol.List>({
    method: 'get',
    params,
    url: CUSTOMER_PROTOCOL_URLS.LIST
  });
}

/** 查询全部。 */
export function fetchGetAllCustomerProtocols() {
  return request<Api.CustomerProtocol.AllEntity[]>({
    method: 'get',
    url: CUSTOMER_PROTOCOL_URLS.BASE
  });
}

/** 查询全部下拉选项（仅 id/name/code，不含明细）。 */
export function fetchGetCustomerProtocolOptions() {
  return request<Api.CustomerProtocol.AllEntity[]>({
    method: 'get',
    url: CUSTOMER_PROTOCOL_URLS.OPTIONS
  });
}

/** 根据 ID 查询。 */
export function fetchGetCustomerProtocolDetail(id: string) {
  return request<Api.CustomerProtocol.Entity>({
    method: 'get',
    url: `${CUSTOMER_PROTOCOL_URLS.BASE}/${id}`
  });
}

/** 创建。 */
export function fetchAddCustomerProtocol(data: Api.CustomerProtocol.CreateParams) {
  return withSelectionOptionInvalidation(
    CUSTOMER_PROTOCOL_URLS.BASE,
    request<Api.CustomerProtocol.Entity>({
      data,
      method: 'post',
      url: CUSTOMER_PROTOCOL_URLS.BASE
    })
  );
}

/** 更新。 */
export function fetchUpdateCustomerProtocol(data: Api.CustomerProtocol.UpdateParams) {
  return withSelectionOptionInvalidation(
    CUSTOMER_PROTOCOL_URLS.BASE,
    request<Api.CustomerProtocol.Entity>({
      data,
      method: 'put',
      url: CUSTOMER_PROTOCOL_URLS.BASE
    })
  );
}

/** 删除。 */
export function fetchDeleteCustomerProtocol(id: string) {
  return withSelectionOptionInvalidation(
    CUSTOMER_PROTOCOL_URLS.BASE,
    request<Api.CustomerProtocol.Entity>({
      method: 'delete',
      url: `${CUSTOMER_PROTOCOL_URLS.BASE}/${id}`
    })
  );
}

/** 批量删除。 */
export function fetchBatchDeleteCustomerProtocol(ids: string[]) {
  return withSelectionOptionInvalidation(
    CUSTOMER_PROTOCOL_URLS.BASE,
    request<Api.CustomerProtocol.Entity>({
      data: ids,
      method: 'delete',
      url: CUSTOMER_PROTOCOL_URLS.BATCH_DELETE
    })
  );
}

/** 启用或禁用。 */
export function fetchToggleCustomerProtocolStatus(params: Api.Base.ToggleStatusParams) {
  return withSelectionOptionInvalidation(
    CUSTOMER_PROTOCOL_URLS.BASE,
    request<Api.CustomerProtocol.Entity>({
      method: 'patch',
      params: { status: params.status },
      url: `${CUSTOMER_PROTOCOL_URLS.BASE}/${params.id}/status`
    })
  );
}
