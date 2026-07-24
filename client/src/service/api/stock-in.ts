import { request } from '../request';
import { STOCK_IN_URLS } from '../urls';

/** 创建其他入库草稿及商品明细。需要库存创建权限。 */
export function fetchAddStockInOther(data: Api.StockIn.CreateOtherPayload) {
  return request<Api.StockIn.Entity>({
    data,
    method: 'post',
    url: STOCK_IN_URLS.OTHER
  });
}

/** 编辑其他入库草稿及其全部商品明细。需要库存更新权限。 */
export function fetchUpdateStockInOther(data: Api.StockIn.UpdateOtherPayload) {
  return request<Api.StockIn.Entity>({
    data,
    method: 'put',
    url: STOCK_IN_URLS.OTHER
  });
}

/** 分页查询其他入库单及商品明细。需要库存读取权限。 */
export function fetchGetStockInOtherList(params?: Api.StockIn.SearchParams) {
  return request<Api.StockIn.List>({
    method: 'get',
    params,
    url: STOCK_IN_URLS.OTHER_LIST
  });
}

/** 删除其他入库草稿。需要库存删除权限。 */
export function fetchDeleteStockInOther(id: string) {
  return request<boolean>({
    method: 'delete',
    url: `${STOCK_IN_URLS.OTHER}/${id}`
  });
}

/** 查询其他入库单详情。需要库存读取权限。 */
export function fetchGetStockInOtherDetail(id: string) {
  return request<Api.StockIn.Entity>({
    method: 'get',
    url: `${STOCK_IN_URLS.OTHER}/${id}`
  });
}

/** 审核其他入库单，增加库存批次并写入库存流水。需要库存更新权限。 */
export function fetchAuditStockInOther(id: string, data?: Api.StockIn.AuditPayload) {
  return request<Api.StockIn.Entity>({
    data,
    method: 'post',
    url: `${STOCK_IN_URLS.OTHER}/${id}/audit`
  });
}

/** 反审核其他入库单，回滚库存批次并写入反向流水。需要库存更新权限。 */
export function fetchReverseAuditStockInOther(id: string, data?: Api.StockIn.AuditPayload) {
  return request<Api.StockIn.Entity>({
    data,
    method: 'post',
    url: `${STOCK_IN_URLS.OTHER}/${id}/reverse-audit`
  });
}

/** 创建采购入库草稿及商品明细。需要库存创建权限。 */
export function fetchAddStockInPurchase(data: Api.StockIn.CreatePurchasePayload) {
  return request<Api.StockIn.Entity>({
    data,
    method: 'post',
    url: STOCK_IN_URLS.PURCHASE
  });
}

/** 编辑采购入库草稿及其全部商品明细。需要库存更新权限。 */
export function fetchUpdateStockInPurchase(data: Api.StockIn.UpdatePurchasePayload) {
  return request<Api.StockIn.Entity>({
    data,
    method: 'put',
    url: STOCK_IN_URLS.PURCHASE
  });
}

/** 分页查询采购入库单及商品明细。需要库存读取权限。 */
export function fetchGetStockInPurchaseList(params?: Api.StockIn.SearchParams) {
  return request<Api.StockIn.List>({
    method: 'get',
    params,
    url: STOCK_IN_URLS.PURCHASE_LIST
  });
}

/** 删除采购入库草稿。需要库存删除权限。 */
export function fetchDeleteStockInPurchase(id: string) {
  return request<boolean>({
    method: 'delete',
    url: `${STOCK_IN_URLS.PURCHASE}/${id}`
  });
}

/** 查询采购入库单详情。需要库存读取权限。 */
export function fetchGetStockInPurchaseDetail(id: string) {
  return request<Api.StockIn.Entity>({
    method: 'get',
    url: `${STOCK_IN_URLS.PURCHASE}/${id}`
  });
}

/** 审核采购入库单，增加库存批次并写入库存流水。需要库存更新权限。 */
export function fetchAuditStockInPurchase(id: string, data?: Api.StockIn.AuditPayload) {
  return request<Api.StockIn.Entity>({
    data,
    method: 'post',
    url: `${STOCK_IN_URLS.PURCHASE}/${id}/audit`
  });
}

/** 反审核采购入库单，回滚库存批次并写入反向流水。需要库存更新权限。 */
export function fetchReverseAuditStockInPurchase(id: string, data?: Api.StockIn.AuditPayload) {
  return request<Api.StockIn.Entity>({
    data,
    method: 'post',
    url: `${STOCK_IN_URLS.PURCHASE}/${id}/reverse-audit`
  });
}

/** 创建销售退货入库草稿及商品明细；关联已完成取货任务时按来源幂等返回。需要库存创建权限。 */
export function fetchAddStockInSalesReturn(data: Api.StockIn.CreateSalesReturnPayload) {
  return request<Api.StockIn.Entity>({
    data,
    method: 'post',
    url: STOCK_IN_URLS.SALES_RETURN
  });
}

/** 编辑销售退货入库草稿及其全部商品明细。需要库存更新权限。 */
export function fetchUpdateStockInSalesReturn(data: Api.StockIn.UpdateSalesReturnPayload) {
  return request<Api.StockIn.Entity>({
    data,
    method: 'put',
    url: STOCK_IN_URLS.SALES_RETURN
  });
}

/** 分页查询销售退货入库单及商品明细。需要库存读取权限。 */
export function fetchGetStockInSalesReturnList(params?: Api.StockIn.SearchParams) {
  return request<Api.StockIn.List>({
    method: 'get',
    params,
    url: STOCK_IN_URLS.SALES_RETURN_LIST
  });
}

/** 删除销售退货入库草稿。需要库存删除权限。 */
export function fetchDeleteStockInSalesReturn(id: string) {
  return request<boolean>({
    method: 'delete',
    url: `${STOCK_IN_URLS.SALES_RETURN}/${id}`
  });
}

/** 查询销售退货入库单详情。需要库存读取权限。 */
export function fetchGetStockInSalesReturnDetail(id: string) {
  return request<Api.StockIn.Entity>({
    method: 'get',
    url: `${STOCK_IN_URLS.SALES_RETURN}/${id}`
  });
}

/** 审核销售退货入库单，增加库存批次并写入库存流水。需要库存更新权限。 */
export function fetchAuditStockInSalesReturn(id: string, data?: Api.StockIn.AuditPayload) {
  return request<Api.StockIn.Entity>({
    data,
    method: 'post',
    url: `${STOCK_IN_URLS.SALES_RETURN}/${id}/audit`
  });
}

/** 反审核销售退货入库单，回滚库存批次并写入反向流水。需要库存更新权限。 */
export function fetchReverseAuditStockInSalesReturn(id: string, data?: Api.StockIn.AuditPayload) {
  return request<Api.StockIn.Entity>({
    data,
    method: 'post',
    url: `${STOCK_IN_URLS.SALES_RETURN}/${id}/reverse-audit`
  });
}
