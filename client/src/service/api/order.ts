import { request } from '../request';
import { ORDER_URLS } from '../urls';

/** 创建销售订单。 */
export function fetchAddOrder(data: Api.Order.CreateParams) {
  return request<Api.Order.Entity>({
    data,
    method: 'post',
    url: ORDER_URLS.BASE
  });
}

/** 编辑销售订单及其商品明细。 */
export function fetchUpdateOrder(data: Api.Order.UpdateParams) {
  return request<Api.Order.Entity>({
    data,
    method: 'put',
    url: ORDER_URLS.BASE
  });
}

/** 分页查询销售订单。 */
export function fetchGetOrderList(params?: Api.Order.SearchParams) {
  return request<Api.Order.List>({
    method: 'get',
    params,
    url: ORDER_URLS.LIST
  });
}

/** 删除销售订单。 */
export function fetchDeleteOrder(id: string) {
  return request<boolean>({
    method: 'delete',
    url: `${ORDER_URLS.BASE}/${id}`
  });
}

/** 查询销售订单详情。 */
export function fetchGetOrderDetail(id: string) {
  return request<Api.Order.Entity>({
    method: 'get',
    url: `${ORDER_URLS.BASE}/${id}`
  });
}

/** 审核通过待审核订单。 */
export function fetchApproveOrder(id: string, data?: Api.Order.AuditParams) {
  return request<Api.Order.Entity>({
    data,
    method: 'post',
    url: `${ORDER_URLS.BASE}/${id}/approve`
  });
}

/** 驳回待审核订单。 */
export function fetchRejectOrder(id: string, data?: Api.Order.AuditParams) {
  return request<Api.Order.Entity>({
    data,
    method: 'post',
    url: `${ORDER_URLS.BASE}/${id}/reject`
  });
}

/** 重新提交已驳回订单。 */
export function fetchResubmitOrder(id: string, data?: Api.Order.AuditParams) {
  return request<Api.Order.Entity>({
    data,
    method: 'post',
    url: `${ORDER_URLS.BASE}/${id}/resubmit`
  });
}
