import { request } from '../request';
import { GOODS_TYPE_URLS } from '../urls';

/** 分页查询。 */
export function fetchGetGoodsTypeList(params?: Api.GoodsType.SearchParams) {
  return request<Api.GoodsType.List>({
    method: 'get',
    params,
    url: GOODS_TYPE_URLS.LIST
  });
}

/** 查询全部。 */
export function fetchGetAllGoodsTypes() {
  return request<Api.GoodsType.AllEntity[]>({
    method: 'get',
    url: GOODS_TYPE_URLS.BASE
  });
}

/** 根据 ID 查询。 */
export function fetchGetGoodsTypeDetail(id: string) {
  return request<Api.GoodsType.Entity>({
    method: 'get',
    url: `${GOODS_TYPE_URLS.BASE}/${id}`
  });
}

/** 创建。 */
export function fetchAddGoodsType(data: Api.GoodsType.CreateParams) {
  return request<Api.GoodsType.Entity>({
    data,
    method: 'post',
    url: GOODS_TYPE_URLS.BASE
  });
}

/** 更新。 */
export function fetchUpdateGoodsType(data: Api.GoodsType.UpdateParams) {
  return request<Api.GoodsType.Entity>({
    data,
    method: 'put',
    url: GOODS_TYPE_URLS.BASE
  });
}

/** 删除。 */
export function fetchDeleteGoodsType(id: string) {
  return request<Api.GoodsType.Entity>({
    method: 'delete',
    url: `${GOODS_TYPE_URLS.BASE}/${id}`
  });
}

/** 批量删除。 */
export function fetchBatchDeleteGoodsType(ids: string[]) {
  return request<Api.GoodsType.Entity>({
    data: ids,
    method: 'delete',
    url: GOODS_TYPE_URLS.BATCH_DELETE
  });
}

/** 启用或禁用。 */
export function fetchToggleGoodsTypeStatus(params: Api.Base.ToggleStatusParams) {
  return request<Api.GoodsType.Entity>({
    method: 'patch',
    params: { status: params.status },
    url: `${GOODS_TYPE_URLS.BASE}/${params.id}/status`
  });
}

/** 获取商品分类树。 */
export function fetchGetGoodsTypeTree() {
  return request<Api.GoodsType.Entity[]>({
    method: 'get',
    url: GOODS_TYPE_URLS.TREE
  });
}
