import { request } from '../request';
import { WARE_URLS } from '../urls';

/** 分页查询。 */
export function fetchGetWareList(params?: Api.Ware.SearchParams) {
  return request<Api.Ware.List>({
    method: 'get',
    params,
    url: WARE_URLS.LIST
  });
}

/** 查询全部。 */
export function fetchGetAllWares() {
  return request<Api.Ware.AllEntity[]>({
    method: 'get',
    url: WARE_URLS.BASE
  });
}

/** 根据 ID 查询。 */
export function fetchGetWareDetail(id: string) {
  return request<Api.Ware.Entity>({
    method: 'get',
    url: `${WARE_URLS.BASE}/${id}`
  });
}

/** 创建。 */
export function fetchAddWare(data: Api.Ware.CreateParams) {
  return request<Api.Ware.Entity>({
    data,
    method: 'post',
    url: WARE_URLS.BASE
  });
}

/** 更新。 */
export function fetchUpdateWare(data: Api.Ware.UpdateParams) {
  return request<Api.Ware.Entity>({
    data,
    method: 'put',
    url: WARE_URLS.BASE
  });
}

/** 删除。 */
export function fetchDeleteWare(id: string) {
  return request<Api.Ware.Entity>({
    method: 'delete',
    url: `${WARE_URLS.BASE}/${id}`
  });
}

/** 批量删除。 */
export function fetchBatchDeleteWare(ids: string[]) {
  return request<Api.Ware.Entity>({
    data: ids,
    method: 'delete',
    url: WARE_URLS.BATCH_DELETE
  });
}

/** 启用或禁用。 */
export function fetchToggleWareStatus(params: Api.Base.ToggleStatusParams) {
  return request<Api.Ware.Entity>({
    method: 'patch',
    params: { status: params.status },
    url: `${WARE_URLS.BASE}/${params.id}/status`
  });
}
