import { request } from '../request';
import { PURCHASER_URLS } from '../urls';

/** 分页查询。 */
export function fetchGetPurchaserList(params?: Api.Purchaser.SearchParams) {
  return request<Api.Purchaser.List>({
    method: 'get',
    params,
    url: PURCHASER_URLS.LIST
  });
}

/** 查询全部。 */
export function fetchGetAllPurchasers() {
  return request<Api.Purchaser.AllEntity[]>({
    method: 'get',
    url: PURCHASER_URLS.BASE
  });
}

/** 根据 ID 查询。 */
export function fetchGetPurchaserDetail(id: string) {
  return request<Api.Purchaser.Entity>({
    method: 'get',
    url: `${PURCHASER_URLS.BASE}/${id}`
  });
}

/** 创建。 */
export function fetchAddPurchaser(data: Api.Purchaser.CreateParams) {
  return request<Api.Purchaser.Entity>({
    data,
    method: 'post',
    url: PURCHASER_URLS.BASE
  });
}

/** 更新。 */
export function fetchUpdatePurchaser(data: Api.Purchaser.UpdateParams) {
  return request<Api.Purchaser.Entity>({
    data,
    method: 'put',
    url: PURCHASER_URLS.BASE
  });
}

/** 删除。 */
export function fetchDeletePurchaser(id: string) {
  return request<Api.Purchaser.Entity>({
    method: 'delete',
    url: `${PURCHASER_URLS.BASE}/${id}`
  });
}

/** 批量删除。 */
export function fetchBatchDeletePurchaser(ids: string[]) {
  return request<Api.Purchaser.Entity>({
    data: ids,
    method: 'delete',
    url: PURCHASER_URLS.BATCH_DELETE
  });
}

/** 启用或禁用。 */
export function fetchTogglePurchaserStatus(params: Api.Base.ToggleStatusParams) {
  return request<Api.Purchaser.Entity>({
    method: 'patch',
    params: { status: params.status },
    url: `${PURCHASER_URLS.BASE}/${params.id}/status`
  });
}
