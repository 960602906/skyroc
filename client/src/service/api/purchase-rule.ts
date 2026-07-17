import { request } from '../request';
import { PURCHASE_RULE_URLS } from '../urls';

/** 分页查询。 */
export function fetchGetPurchaseRuleList(params?: Api.PurchaseRule.SearchParams) {
  return request<Api.PurchaseRule.List>({
    method: 'get',
    params,
    url: PURCHASE_RULE_URLS.LIST
  });
}

/** 查询全部。 */
export function fetchGetAllPurchaseRules() {
  return request<Api.PurchaseRule.AllEntity[]>({
    method: 'get',
    url: PURCHASE_RULE_URLS.BASE
  });
}

/** 根据 ID 查询。 */
export function fetchGetPurchaseRuleDetail(id: string) {
  return request<Api.PurchaseRule.Entity>({
    method: 'get',
    url: `${PURCHASE_RULE_URLS.BASE}/${id}`
  });
}

/** 创建。 */
export function fetchAddPurchaseRule(data: Api.PurchaseRule.CreateParams) {
  return request<Api.PurchaseRule.Entity>({
    data,
    method: 'post',
    url: PURCHASE_RULE_URLS.BASE
  });
}

/** 更新。 */
export function fetchUpdatePurchaseRule(data: Api.PurchaseRule.UpdateParams) {
  return request<Api.PurchaseRule.Entity>({
    data,
    method: 'put',
    url: PURCHASE_RULE_URLS.BASE
  });
}

/** 删除。 */
export function fetchDeletePurchaseRule(id: string) {
  return request<Api.PurchaseRule.Entity>({
    method: 'delete',
    url: `${PURCHASE_RULE_URLS.BASE}/${id}`
  });
}

/** 批量删除。 */
export function fetchBatchDeletePurchaseRule(ids: string[]) {
  return request<Api.PurchaseRule.Entity>({
    data: ids,
    method: 'delete',
    url: PURCHASE_RULE_URLS.BATCH_DELETE
  });
}

/** 启用或禁用。 */
export function fetchTogglePurchaseRuleStatus(params: Api.Base.ToggleStatusParams) {
  return request<Api.PurchaseRule.Entity>({
    method: 'patch',
    params: { status: params.status },
    url: `${PURCHASE_RULE_URLS.BASE}/${params.id}/status`
  });
}
