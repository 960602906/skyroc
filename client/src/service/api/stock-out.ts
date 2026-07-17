import { request } from '../request';
import { STOCK_OUT_URLS } from '../urls';

/** 创建其他出库草稿及商品批次明细。需要库存创建权限。 */
export function fetchAddStockOutOther(data?: Api.StockOut.Payload) {
  return request<Api.StockOut.Entity>({
    data,
    method: 'post',
    url: STOCK_OUT_URLS.OTHER
  });
}

/** 编辑其他出库草稿及其全部商品批次明细。需要库存更新权限。 */
export function fetchUpdateStockOutOther(data?: Api.StockOut.Payload) {
  return request<Api.StockOut.Entity>({
    data,
    method: 'put',
    url: STOCK_OUT_URLS.OTHER
  });
}

/** 分页查询其他出库单及商品批次明细。需要库存读取权限。 */
export function fetchGetStockOutOtherList(params?: Api.StockOut.SearchParams) {
  return request<Api.StockOut.List>({
    method: 'get',
    params,
    url: STOCK_OUT_URLS.OTHER_LIST
  });
}

/** 删除其他出库草稿。需要库存删除权限。 */
export function fetchDeleteStockOutOther(id: string) {
  return request<Api.StockOut.Entity>({
    method: 'delete',
    url: `${STOCK_OUT_URLS.OTHER}/${id}`
  });
}

/** 查询其他出库单详情。需要库存读取权限。 */
export function fetchGetStockOutOtherDetail(id: string) {
  return request<Api.StockOut.Entity>({
    method: 'get',
    url: `${STOCK_OUT_URLS.OTHER}/${id}`
  });
}

/** 审核其他出库单，校验可用库存后扣减批次并写入库存流水。需要库存更新权限。 */
export function fetchAuditStockOutOther(id: string, data?: Api.StockOut.Payload) {
  return request<Api.StockOut.Entity>({
    data,
    method: 'post',
    url: `${STOCK_OUT_URLS.OTHER}/${id}/audit`
  });
}

/** 反审核其他出库单，恢复库存批次并写入反向流水。需要库存更新权限。 */
export function fetchReverseAuditStockOutOther(id: string, data?: Api.StockOut.Payload) {
  return request<Api.StockOut.Entity>({
    data,
    method: 'post',
    url: `${STOCK_OUT_URLS.OTHER}/${id}/reverse-audit`
  });
}

/** 创建采购退货出库草稿及商品批次明细。需要库存创建权限。 */
export function fetchAddStockOutPurchaseReturn(data?: Api.StockOut.Payload) {
  return request<Api.StockOut.Entity>({
    data,
    method: 'post',
    url: STOCK_OUT_URLS.PURCHASE_RETURN
  });
}

/** 编辑采购退货出库草稿及其全部商品批次明细。需要库存更新权限。 */
export function fetchUpdateStockOutPurchaseReturn(data?: Api.StockOut.Payload) {
  return request<Api.StockOut.Entity>({
    data,
    method: 'put',
    url: STOCK_OUT_URLS.PURCHASE_RETURN
  });
}

/** 分页查询采购退货出库单及商品批次明细。需要库存读取权限。 */
export function fetchGetStockOutPurchaseReturnList(params?: Api.StockOut.SearchParams) {
  return request<Api.StockOut.List>({
    method: 'get',
    params,
    url: STOCK_OUT_URLS.PURCHASE_RETURN_LIST
  });
}

/** 删除采购退货出库草稿。需要库存删除权限。 */
export function fetchDeleteStockOutPurchaseReturn(id: string) {
  return request<Api.StockOut.Entity>({
    method: 'delete',
    url: `${STOCK_OUT_URLS.PURCHASE_RETURN}/${id}`
  });
}

/** 查询采购退货出库单详情。需要库存读取权限。 */
export function fetchGetStockOutPurchaseReturnDetail(id: string) {
  return request<Api.StockOut.Entity>({
    method: 'get',
    url: `${STOCK_OUT_URLS.PURCHASE_RETURN}/${id}`
  });
}

/** 审核采购退货出库单，校验可用库存后扣减批次并写入库存流水。需要库存更新权限。 */
export function fetchAuditStockOutPurchaseReturn(id: string, data?: Api.StockOut.Payload) {
  return request<Api.StockOut.Entity>({
    data,
    method: 'post',
    url: `${STOCK_OUT_URLS.PURCHASE_RETURN}/${id}/audit`
  });
}

/** 反审核采购退货出库单，恢复库存批次并写入反向流水。需要库存更新权限。 */
export function fetchReverseAuditStockOutPurchaseReturn(id: string, data?: Api.StockOut.Payload) {
  return request<Api.StockOut.Entity>({
    data,
    method: 'post',
    url: `${STOCK_OUT_URLS.PURCHASE_RETURN}/${id}/reverse-audit`
  });
}

/** 创建销售出库草稿及商品批次明细。需要库存创建权限。 */
export function fetchAddStockOutSale(data?: Api.StockOut.Payload) {
  return request<Api.StockOut.Entity>({
    data,
    method: 'post',
    url: STOCK_OUT_URLS.SALE
  });
}

/** 编辑销售出库草稿及其全部商品批次明细。需要库存更新权限。 */
export function fetchUpdateStockOutSale(data?: Api.StockOut.Payload) {
  return request<Api.StockOut.Entity>({
    data,
    method: 'put',
    url: STOCK_OUT_URLS.SALE
  });
}

/** 分页查询销售出库单及商品批次明细。需要库存读取权限。 */
export function fetchGetStockOutSaleList(params?: Api.StockOut.SearchParams) {
  return request<Api.StockOut.List>({
    method: 'get',
    params,
    url: STOCK_OUT_URLS.SALE_LIST
  });
}

/** 删除销售出库草稿。需要库存删除权限。 */
export function fetchDeleteStockOutSale(id: string) {
  return request<Api.StockOut.Entity>({
    method: 'delete',
    url: `${STOCK_OUT_URLS.SALE}/${id}`
  });
}

/** 查询销售出库单详情。需要库存读取权限。 */
export function fetchGetStockOutSaleDetail(id: string) {
  return request<Api.StockOut.Entity>({
    method: 'get',
    url: `${STOCK_OUT_URLS.SALE}/${id}`
  });
}

/** 审核销售出库单，校验可用库存后扣减批次并写入库存流水。需要库存更新权限。 */
export function fetchAuditStockOutSale(id: string, data?: Api.StockOut.Payload) {
  return request<Api.StockOut.Entity>({
    data,
    method: 'post',
    url: `${STOCK_OUT_URLS.SALE}/${id}/audit`
  });
}

/** 反审核销售出库单，恢复库存批次并写入反向流水。需要库存更新权限。 */
export function fetchReverseAuditStockOutSale(id: string, data?: Api.StockOut.Payload) {
  return request<Api.StockOut.Entity>({
    data,
    method: 'post',
    url: `${STOCK_OUT_URLS.SALE}/${id}/reverse-audit`
  });
}
