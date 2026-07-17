import { request } from '../request';
import { GOODS_URLS } from '../urls';

/** 分页查询。 */
export function fetchGetGoodsList(params?: Api.Goods.SearchParams) {
  return request<Api.Goods.List>({
    method: 'get',
    params,
    url: GOODS_URLS.LIST
  });
}

/** 查询全部。 */
export function fetchGetAllGoods() {
  return request<Api.Goods.AllEntity[]>({
    method: 'get',
    url: GOODS_URLS.BASE
  });
}

/** 根据 ID 查询。 */
export function fetchGetGoodsDetail(id: string) {
  return request<Api.Goods.Entity>({
    method: 'get',
    url: `${GOODS_URLS.BASE}/${id}`
  });
}

/** 创建。 */
export function fetchAddGoods(data: Api.Goods.CreateParams) {
  return request<Api.Goods.Entity>({
    data,
    method: 'post',
    url: GOODS_URLS.BASE
  });
}

/** 更新。 */
export function fetchUpdateGoods(data: Api.Goods.UpdateParams) {
  return request<Api.Goods.Entity>({
    data,
    method: 'put',
    url: GOODS_URLS.BASE
  });
}

/** 删除。 */
export function fetchDeleteGoods(id: string) {
  return request<Api.Goods.Entity>({
    method: 'delete',
    url: `${GOODS_URLS.BASE}/${id}`
  });
}

/** 批量删除。 */
export function fetchBatchDeleteGoods(ids: string[]) {
  return request<Api.Goods.Entity>({
    data: ids,
    method: 'delete',
    url: GOODS_URLS.BATCH_DELETE
  });
}

/** 启用或禁用。 */
export function fetchToggleGoodsStatus(params: Api.Base.ToggleStatusParams) {
  return request<Api.Goods.Entity>({
    method: 'patch',
    params: { status: params.status },
    url: `${GOODS_URLS.BASE}/${params.id}/status`
  });
}

/** 修改商品上下架状态。 */
export function fetchToggleGoodsSaleStatus(id: string, isOnSale: boolean) {
  return request<Api.Goods.Entity>({
    method: 'patch',
    params: { isOnSale },
    url: `${GOODS_URLS.BASE}/${id}/sale-status`
  });
}
