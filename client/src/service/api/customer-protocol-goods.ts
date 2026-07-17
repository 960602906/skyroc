import { request } from '../request';
import { CUSTOMER_PROTOCOL_GOODS_URLS } from '../urls';

/** 分页查询。 */
export function fetchGetCustomerProtocolGoodsList(params?: Api.CustomerProtocolGoods.SearchParams) {
  return request<Api.CustomerProtocolGoods.List>({
    method: 'get',
    params,
    url: CUSTOMER_PROTOCOL_GOODS_URLS.LIST
  });
}

/** 查询全部。 */
export function fetchGetAllCustomerProtocolGoods() {
  return request<Api.CustomerProtocolGoods.AllEntity[]>({
    method: 'get',
    url: CUSTOMER_PROTOCOL_GOODS_URLS.BASE
  });
}

/** 根据 ID 查询。 */
export function fetchGetCustomerProtocolGoodsDetail(id: string) {
  return request<Api.CustomerProtocolGoods.Entity>({
    method: 'get',
    url: `${CUSTOMER_PROTOCOL_GOODS_URLS.BASE}/${id}`
  });
}

/** 创建。 */
export function fetchAddCustomerProtocolGoods(data: Api.CustomerProtocolGoods.CreateParams) {
  return request<Api.CustomerProtocolGoods.Entity>({
    data,
    method: 'post',
    url: CUSTOMER_PROTOCOL_GOODS_URLS.BASE
  });
}

/** 更新。 */
export function fetchUpdateCustomerProtocolGoods(data: Api.CustomerProtocolGoods.UpdateParams) {
  return request<Api.CustomerProtocolGoods.Entity>({
    data,
    method: 'put',
    url: CUSTOMER_PROTOCOL_GOODS_URLS.BASE
  });
}

/** 删除。 */
export function fetchDeleteCustomerProtocolGoods(id: string) {
  return request<Api.CustomerProtocolGoods.Entity>({
    method: 'delete',
    url: `${CUSTOMER_PROTOCOL_GOODS_URLS.BASE}/${id}`
  });
}

/** 批量删除。 */
export function fetchBatchDeleteCustomerProtocolGoods(ids: string[]) {
  return request<Api.CustomerProtocolGoods.Entity>({
    data: ids,
    method: 'delete',
    url: CUSTOMER_PROTOCOL_GOODS_URLS.BATCH_DELETE
  });
}

/** 启用或禁用。 */
export function fetchToggleCustomerProtocolGoodsStatus(params: Api.Base.ToggleStatusParams) {
  return request<Api.CustomerProtocolGoods.Entity>({
    method: 'patch',
    params: { status: params.status },
    url: `${CUSTOMER_PROTOCOL_GOODS_URLS.BASE}/${params.id}/status`
  });
}
