import { request } from '../request';
import { SUPPLIER_SETTLEMENT_URLS } from '../urls';

/** 分页查询供应商结算单。 */
export function fetchGetSupplierSettlementList(params?: Api.SupplierSettlement.SearchParams) {
  return request<Api.SupplierSettlement.List>({
    method: 'get',
    params,
    url: SUPPLIER_SETTLEMENT_URLS.BASE
  });
}

/** 创建供应商结算单并回写待结单据余额。 */
export function fetchAddSupplierSettlement(data?: Api.SupplierSettlement.Payload) {
  return request<Api.SupplierSettlement.Entity>({
    data,
    method: 'post',
    url: SUPPLIER_SETTLEMENT_URLS.BASE
  });
}

/** 分页查询供应商待结单据，可筛选仍有未结余额的待处理单据。 */
export function fetchGetSupplierSettlementBills(params?: Api.SupplierSettlement.SearchParams) {
  return request<Api.SupplierSettlement.Result>({
    method: 'get',
    params,
    url: SUPPLIER_SETTLEMENT_URLS.BILLS
  });
}

/** 查询供应商结算单明细。 */
export function fetchGetSupplierSettlementDetail(id: string) {
  return request<Api.SupplierSettlement.Entity>({
    method: 'get',
    url: `${SUPPLIER_SETTLEMENT_URLS.BASE}/${id}`
  });
}

/** 作废供应商结算单并回滚已核销待结单据金额。 */
export function fetchDeleteSupplierSettlementVoid(id: string) {
  return request<Api.SupplierSettlement.Entity>({
    method: 'delete',
    url: `${SUPPLIER_SETTLEMENT_URLS.BASE}/${id}/void`
  });
}
