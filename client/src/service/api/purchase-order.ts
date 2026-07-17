import { request } from '../request';
import { PURCHASE_ORDER_URLS } from '../urls';

/** 手工创建采购单草稿和商品明细。需要采购创建权限。 */
export function fetchAddPurchaseOrder(data?: Api.PurchaseOrder.Payload) {
  return request<Api.PurchaseOrder.Entity>({
    data,
    method: 'post',
    url: PURCHASE_ORDER_URLS.BASE
  });
}

/** 编辑采购单草稿及其全部商品明细。需要采购更新权限。 */
export function fetchUpdatePurchaseOrder(data?: Api.PurchaseOrder.Payload) {
  return request<Api.PurchaseOrder.Entity>({
    data,
    method: 'put',
    url: PURCHASE_ORDER_URLS.BASE
  });
}

/** 从采购计划全部剩余数量批量生成采购单草稿。需要采购创建权限。 */
export function fetchGenerateFromPlansPurchaseOrder(data?: Api.PurchaseOrder.Payload) {
  return request<Api.PurchaseOrder.Entity>({
    data,
    method: 'post',
    url: PURCHASE_ORDER_URLS.GENERATE_FROM_PLANS
  });
}

/** 分页查询采购单及商品明细。需要采购读取权限。 */
export function fetchGetPurchaseOrderList(params?: Api.PurchaseOrder.SearchParams) {
  return request<Api.PurchaseOrder.List>({
    method: 'get',
    params,
    url: PURCHASE_ORDER_URLS.LIST
  });
}

/** 删除采购单草稿并释放采购计划数量占用。需要采购删除权限。 */
export function fetchDeletePurchaseOrder(id: string) {
  return request<Api.PurchaseOrder.Entity>({
    method: 'delete',
    url: `${PURCHASE_ORDER_URLS.BASE}/${id}`
  });
}

/** 查询采购单详情及其采购计划来源。需要采购读取权限。 */
export function fetchGetPurchaseOrderDetail(id: string) {
  return request<Api.PurchaseOrder.Entity>({
    method: 'get',
    url: `${PURCHASE_ORDER_URLS.BASE}/${id}`
  });
}

/** 取消采购单草稿并释放采购计划数量占用。需要采购更新权限。 */
export function fetchCancelPurchaseOrder(id: string, data?: Api.PurchaseOrder.Payload) {
  return request<Api.PurchaseOrder.Entity>({
    data,
    method: 'post',
    url: `${PURCHASE_ORDER_URLS.BASE}/${id}/cancel`
  });
}

/** 完成采购单草稿，使其可供后续采购入库引用。需要采购更新权限。 */
export function fetchCompletePurchaseOrder(id: string, data?: Api.PurchaseOrder.Payload) {
  return request<Api.PurchaseOrder.Entity>({
    data,
    method: 'post',
    url: `${PURCHASE_ORDER_URLS.BASE}/${id}/complete`
  });
}
