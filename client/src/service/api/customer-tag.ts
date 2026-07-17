import { request } from '../request';
import { CUSTOMER_TAG_URLS } from '../urls';

/** 分页查询。 */
export function fetchGetCustomerTagList(params?: Api.CustomerTag.SearchParams) {
  return request<Api.CustomerTag.List>({
    method: 'get',
    params,
    url: CUSTOMER_TAG_URLS.LIST
  });
}

/** 查询全部。 */
export function fetchGetAllCustomerTags() {
  return request<Api.CustomerTag.AllEntity[]>({
    method: 'get',
    url: CUSTOMER_TAG_URLS.BASE
  });
}

/** 根据 ID 查询。 */
export function fetchGetCustomerTagDetail(id: string) {
  return request<Api.CustomerTag.Entity>({
    method: 'get',
    url: `${CUSTOMER_TAG_URLS.BASE}/${id}`
  });
}

/** 创建。 */
export function fetchAddCustomerTag(data: Api.CustomerTag.CreateParams) {
  return request<Api.CustomerTag.Entity>({
    data,
    method: 'post',
    url: CUSTOMER_TAG_URLS.BASE
  });
}

/** 更新。 */
export function fetchUpdateCustomerTag(data: Api.CustomerTag.UpdateParams) {
  return request<Api.CustomerTag.Entity>({
    data,
    method: 'put',
    url: CUSTOMER_TAG_URLS.BASE
  });
}

/** 删除。 */
export function fetchDeleteCustomerTag(id: string) {
  return request<Api.CustomerTag.Entity>({
    method: 'delete',
    url: `${CUSTOMER_TAG_URLS.BASE}/${id}`
  });
}

/** 批量删除。 */
export function fetchBatchDeleteCustomerTag(ids: string[]) {
  return request<Api.CustomerTag.Entity>({
    data: ids,
    method: 'delete',
    url: CUSTOMER_TAG_URLS.BATCH_DELETE
  });
}

/** 启用或禁用。 */
export function fetchToggleCustomerTagStatus(params: Api.Base.ToggleStatusParams) {
  return request<Api.CustomerTag.Entity>({
    method: 'patch',
    params: { status: params.status },
    url: `${CUSTOMER_TAG_URLS.BASE}/${params.id}/status`
  });
}

/** 获取客户标签树（后端以分页结构返回，取 records 为树根列表）。 */
export async function fetchGetCustomerTagTree() {
  const page = await request<Api.CustomerTag.List>({
    method: 'get',
    url: CUSTOMER_TAG_URLS.TREE
  });

  return page?.records ?? [];
}
