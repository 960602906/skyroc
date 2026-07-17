import { request } from '../request';
import { CUSTOMER_SUB_ACCOUNT_URLS } from '../urls';

/** 分页查询。 */
export function fetchGetCustomerSubAccountList(params?: Api.CustomerSubAccount.SearchParams) {
  return request<Api.CustomerSubAccount.List>({
    method: 'get',
    params,
    url: CUSTOMER_SUB_ACCOUNT_URLS.LIST
  });
}

/** 查询全部。 */
export function fetchGetAllCustomerSubAccounts() {
  return request<Api.CustomerSubAccount.AllEntity[]>({
    method: 'get',
    url: CUSTOMER_SUB_ACCOUNT_URLS.BASE
  });
}

/** 根据 ID 查询。 */
export function fetchGetCustomerSubAccountDetail(id: string) {
  return request<Api.CustomerSubAccount.Entity>({
    method: 'get',
    url: `${CUSTOMER_SUB_ACCOUNT_URLS.BASE}/${id}`
  });
}

/** 创建。 */
export function fetchAddCustomerSubAccount(data: Api.CustomerSubAccount.CreateParams) {
  return request<Api.CustomerSubAccount.Entity>({
    data,
    method: 'post',
    url: CUSTOMER_SUB_ACCOUNT_URLS.BASE
  });
}

/** 更新。 */
export function fetchUpdateCustomerSubAccount(data: Api.CustomerSubAccount.UpdateParams) {
  return request<Api.CustomerSubAccount.Entity>({
    data,
    method: 'put',
    url: CUSTOMER_SUB_ACCOUNT_URLS.BASE
  });
}

/** 删除。 */
export function fetchDeleteCustomerSubAccount(id: string) {
  return request<Api.CustomerSubAccount.Entity>({
    method: 'delete',
    url: `${CUSTOMER_SUB_ACCOUNT_URLS.BASE}/${id}`
  });
}

/** 批量删除。 */
export function fetchBatchDeleteCustomerSubAccount(ids: string[]) {
  return request<Api.CustomerSubAccount.Entity>({
    data: ids,
    method: 'delete',
    url: CUSTOMER_SUB_ACCOUNT_URLS.BATCH_DELETE
  });
}

/** 启用或禁用。 */
export function fetchToggleCustomerSubAccountStatus(params: Api.Base.ToggleStatusParams) {
  return request<Api.CustomerSubAccount.Entity>({
    method: 'patch',
    params: { status: params.status },
    url: `${CUSTOMER_SUB_ACCOUNT_URLS.BASE}/${params.id}/status`
  });
}
