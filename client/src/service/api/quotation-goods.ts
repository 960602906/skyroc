import { request } from '../request';
import { QUOTATION_GOODS_URLS } from '../urls';

/** 分页查询。 */
export function fetchGetQuotationGoodsList(params?: Api.QuotationGoods.SearchParams) {
  return request<Api.QuotationGoods.List>({
    method: 'get',
    params,
    url: QUOTATION_GOODS_URLS.LIST
  });
}

/** 查询全部。 */
export function fetchGetAllQuotationGoods() {
  return request<Api.QuotationGoods.AllEntity[]>({
    method: 'get',
    url: QUOTATION_GOODS_URLS.BASE
  });
}

/** 根据 ID 查询。 */
export function fetchGetQuotationGoodsDetail(id: string) {
  return request<Api.QuotationGoods.Entity>({
    method: 'get',
    url: `${QUOTATION_GOODS_URLS.BASE}/${id}`
  });
}

/** 创建。 */
export function fetchAddQuotationGoods(data: Api.QuotationGoods.CreateParams) {
  return request<Api.QuotationGoods.Entity>({
    data,
    method: 'post',
    url: QUOTATION_GOODS_URLS.BASE
  });
}

/** 更新。 */
export function fetchUpdateQuotationGoods(data: Api.QuotationGoods.UpdateParams) {
  return request<Api.QuotationGoods.Entity>({
    data,
    method: 'put',
    url: QUOTATION_GOODS_URLS.BASE
  });
}

/** 删除。 */
export function fetchDeleteQuotationGoods(id: string) {
  return request<Api.QuotationGoods.Entity>({
    method: 'delete',
    url: `${QUOTATION_GOODS_URLS.BASE}/${id}`
  });
}

/** 批量删除。 */
export function fetchBatchDeleteQuotationGoods(ids: string[]) {
  return request<Api.QuotationGoods.Entity>({
    data: ids,
    method: 'delete',
    url: QUOTATION_GOODS_URLS.BATCH_DELETE
  });
}

/** 启用或禁用。 */
export function fetchToggleQuotationGoodsStatus(params: Api.Base.ToggleStatusParams) {
  return request<Api.QuotationGoods.Entity>({
    method: 'patch',
    params: { status: params.status },
    url: `${QUOTATION_GOODS_URLS.BASE}/${params.id}/status`
  });
}
