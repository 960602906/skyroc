import { request } from '../request';
import { CUSTOMER_URLS } from '../urls';

import { withSelectionOptionInvalidation } from './selection-option';

/** 分页查询。 */
export function fetchGetCustomerList(params?: Api.Customer.SearchParams) {
  return request<Api.Customer.List>({
    method: 'get',
    params,
    url: CUSTOMER_URLS.LIST
  });
}

/** 查询全部。 */
export function fetchGetAllCustomers() {
  return request<Api.Customer.AllEntity[]>({
    method: 'get',
    url: CUSTOMER_URLS.BASE
  });
}

/** 根据 ID 查询。 */
export function fetchGetCustomerDetail(id: string) {
  return request<Api.Customer.Entity>({
    method: 'get',
    url: `${CUSTOMER_URLS.BASE}/${id}`
  });
}

/** 创建。 */
export function fetchAddCustomer(data: Api.Customer.CreateParams) {
  return withSelectionOptionInvalidation(
    CUSTOMER_URLS.BASE,
    request<Api.Customer.Entity>({
      data,
      method: 'post',
      url: CUSTOMER_URLS.BASE
    })
  );
}

/** 更新。 */
export function fetchUpdateCustomer(data: Api.Customer.UpdateParams) {
  return withSelectionOptionInvalidation(
    CUSTOMER_URLS.BASE,
    request<Api.Customer.Entity>({
      data,
      method: 'put',
      url: CUSTOMER_URLS.BASE
    })
  );
}

/** 删除。 */
export function fetchDeleteCustomer(id: string) {
  return withSelectionOptionInvalidation(
    CUSTOMER_URLS.BASE,
    request<Api.Customer.Entity>({
      method: 'delete',
      url: `${CUSTOMER_URLS.BASE}/${id}`
    })
  );
}

/** 批量删除。 */
export function fetchBatchDeleteCustomer(ids: string[]) {
  return withSelectionOptionInvalidation(
    CUSTOMER_URLS.BASE,
    request<Api.Customer.Entity>({
      data: ids,
      method: 'delete',
      url: CUSTOMER_URLS.BATCH_DELETE
    })
  );
}

/** 启用或禁用。 */
export function fetchToggleCustomerStatus(params: Api.Base.ToggleStatusParams) {
  return withSelectionOptionInvalidation(
    CUSTOMER_URLS.BASE,
    request<Api.Customer.Entity>({
      method: 'patch',
      params: { status: params.status },
      url: `${CUSTOMER_URLS.BASE}/${params.id}/status`
    })
  );
}
