import { request } from '../request';
import { CUSTOMER_SETTLEMENT_URLS } from '../urls';

/** 分页查询客户结款凭证。 */
export function fetchGetCustomerSettlementList(params?: Api.CustomerSettlement.SearchParams) {
  return request<Api.CustomerSettlement.List>({
    method: 'get',
    params,
    url: CUSTOMER_SETTLEMENT_URLS.BASE
  });
}

/** 创建客户结款凭证并回写客户账单余额。 */
export function fetchAddCustomerSettlement(data?: Api.CustomerSettlement.Payload) {
  return request<Api.CustomerSettlement.Entity>({
    data,
    method: 'post',
    url: CUSTOMER_SETTLEMENT_URLS.BASE
  });
}

/** 分页查询客户账单，可筛选仍有未结余额的待结账单。 */
export function fetchGetCustomerSettlementBills(params?: Api.CustomerSettlement.SearchParams) {
  return request<Api.CustomerSettlement.Result>({
    method: 'get',
    params,
    url: CUSTOMER_SETTLEMENT_URLS.BILLS
  });
}

/** 查询客户结款凭证明细。 */
export function fetchGetCustomerSettlementDetail(id: string) {
  return request<Api.CustomerSettlement.Entity>({
    method: 'get',
    url: `${CUSTOMER_SETTLEMENT_URLS.BASE}/${id}`
  });
}

/** 作废客户结款凭证并回滚已核销账单金额。 */
export function fetchDeleteCustomerSettlementVoid(id: string) {
  return request<Api.CustomerSettlement.Entity>({
    method: 'delete',
    url: `${CUSTOMER_SETTLEMENT_URLS.BASE}/${id}/void`
  });
}
