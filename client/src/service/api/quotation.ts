import { request } from '../request';
import { QUOTATION_URLS } from '../urls';

import { withSelectionOptionInvalidation } from './selection-option';

/** 分页查询。 */
export function fetchGetQuotationList(params?: Api.Quotation.SearchParams) {
  return request<Api.Quotation.List>({
    method: 'get',
    params,
    url: QUOTATION_URLS.LIST
  });
}

/** 查询全部。 */
export function fetchGetAllQuotations() {
  return request<Api.Quotation.AllEntity[]>({
    method: 'get',
    url: QUOTATION_URLS.BASE
  });
}

/** 查询全部下拉选项（仅 id/name/code，不含明细）。 */
export function fetchGetQuotationOptions() {
  return request<Api.Quotation.AllEntity[]>({
    method: 'get',
    url: QUOTATION_URLS.OPTIONS
  });
}

/** 根据 ID 查询。 */
export function fetchGetQuotationDetail(id: string) {
  return request<Api.Quotation.Entity>({
    method: 'get',
    url: `${QUOTATION_URLS.BASE}/${id}`
  });
}

/** 创建。 */
export function fetchAddQuotation(data: Api.Quotation.CreateParams) {
  return withSelectionOptionInvalidation(
    QUOTATION_URLS.BASE,
    request<Api.Quotation.Entity>({
      data,
      method: 'post',
      url: QUOTATION_URLS.BASE
    })
  );
}

/** 更新。 */
export function fetchUpdateQuotation(data: Api.Quotation.UpdateParams) {
  return withSelectionOptionInvalidation(
    QUOTATION_URLS.BASE,
    request<Api.Quotation.Entity>({
      data,
      method: 'put',
      url: QUOTATION_URLS.BASE
    })
  );
}

/** 删除。 */
export function fetchDeleteQuotation(id: string) {
  return withSelectionOptionInvalidation(
    QUOTATION_URLS.BASE,
    request<Api.Quotation.Entity>({
      method: 'delete',
      url: `${QUOTATION_URLS.BASE}/${id}`
    })
  );
}

/** 批量删除。 */
export function fetchBatchDeleteQuotation(ids: string[]) {
  return withSelectionOptionInvalidation(
    QUOTATION_URLS.BASE,
    request<Api.Quotation.Entity>({
      data: ids,
      method: 'delete',
      url: QUOTATION_URLS.BATCH_DELETE
    })
  );
}

/** 启用或禁用。 */
export function fetchToggleQuotationStatus(params: Api.Base.ToggleStatusParams) {
  return withSelectionOptionInvalidation(
    QUOTATION_URLS.BASE,
    request<Api.Quotation.Entity>({
      method: 'patch',
      params: { status: params.status },
      url: `${QUOTATION_URLS.BASE}/${params.id}/status`
    })
  );
}

/** 审核或反审核报价单。 */
export function fetchAuditQuotation(id: string, isAudited: boolean) {
  return withSelectionOptionInvalidation(
    QUOTATION_URLS.BASE,
    request<Api.Quotation.Entity>({
      method: 'patch',
      params: { isAudited },
      url: `${QUOTATION_URLS.BASE}/${id}/audit`
    })
  );
}
