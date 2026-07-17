import { request } from '../request';
import { GOODS_UNIT_URLS } from '../urls';

/** 分页查询。 */
export function fetchGetGoodsUnitList(params?: Api.GoodsUnit.SearchParams) {
  return request<Api.GoodsUnit.List>({
    method: 'get',
    params,
    url: GOODS_UNIT_URLS.LIST
  });
}

/** 查询全部。 */
export function fetchGetAllGoodsUnits() {
  return request<Api.GoodsUnit.AllEntity[]>({
    method: 'get',
    url: GOODS_UNIT_URLS.BASE
  });
}

/** 根据 ID 查询。 */
export function fetchGetGoodsUnitDetail(id: string) {
  return request<Api.GoodsUnit.Entity>({
    method: 'get',
    url: `${GOODS_UNIT_URLS.BASE}/${id}`
  });
}

/** 创建。 */
export function fetchAddGoodsUnit(data: Api.GoodsUnit.CreateParams) {
  return request<Api.GoodsUnit.Entity>({
    data,
    method: 'post',
    url: GOODS_UNIT_URLS.BASE
  });
}

/** 更新。 */
export function fetchUpdateGoodsUnit(data: Api.GoodsUnit.UpdateParams) {
  return request<Api.GoodsUnit.Entity>({
    data,
    method: 'put',
    url: GOODS_UNIT_URLS.BASE
  });
}

/** 删除。 */
export function fetchDeleteGoodsUnit(id: string) {
  return request<Api.GoodsUnit.Entity>({
    method: 'delete',
    url: `${GOODS_UNIT_URLS.BASE}/${id}`
  });
}

/** 批量删除。 */
export function fetchBatchDeleteGoodsUnit(ids: string[]) {
  return request<Api.GoodsUnit.Entity>({
    data: ids,
    method: 'delete',
    url: GOODS_UNIT_URLS.BATCH_DELETE
  });
}

/** 启用或禁用。 */
export function fetchToggleGoodsUnitStatus(params: Api.Base.ToggleStatusParams) {
  return request<Api.GoodsUnit.Entity>({
    method: 'patch',
    params: { status: params.status },
    url: `${GOODS_UNIT_URLS.BASE}/${params.id}/status`
  });
}

/** 查询指定商品的单位列表。 */
export function fetchGetGoodsUnitsByGoods(goodsId: string) {
  return request<Api.GoodsUnit.Entity[]>({
    method: 'get',
    url: `${GOODS_UNIT_URLS.BY_GOODS}/${goodsId}`
  });
}
