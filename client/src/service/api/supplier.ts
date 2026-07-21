import { request } from '../request';
import { SUPPLIER_URLS } from '../urls';

import { withSelectionOptionInvalidation } from './selection-option';

/** 分页查询。 */
export function fetchGetSupplierList(params?: Api.Supplier.SearchParams) {
  return request<Api.Supplier.List>({
    method: 'get',
    params,
    url: SUPPLIER_URLS.LIST
  });
}

/** 查询全部。 */
export function fetchGetAllSuppliers() {
  return request<Api.Supplier.AllEntity[]>({
    method: 'get',
    url: SUPPLIER_URLS.BASE
  });
}

/** 根据 ID 查询。 */
export function fetchGetSupplierDetail(id: string) {
  return request<Api.Supplier.Entity>({
    method: 'get',
    url: `${SUPPLIER_URLS.BASE}/${id}`
  });
}

/** 创建。 */
export function fetchAddSupplier(data: Api.Supplier.CreateParams) {
  return withSelectionOptionInvalidation(
    SUPPLIER_URLS.BASE,
    request<Api.Supplier.Entity>({
      data,
      method: 'post',
      url: SUPPLIER_URLS.BASE
    })
  );
}

/** 更新。 */
export function fetchUpdateSupplier(data: Api.Supplier.UpdateParams) {
  return withSelectionOptionInvalidation(
    SUPPLIER_URLS.BASE,
    request<Api.Supplier.Entity>({
      data,
      method: 'put',
      url: SUPPLIER_URLS.BASE
    })
  );
}

/** 删除。 */
export function fetchDeleteSupplier(id: string) {
  return withSelectionOptionInvalidation(
    SUPPLIER_URLS.BASE,
    request<Api.Supplier.Entity>({
      method: 'delete',
      url: `${SUPPLIER_URLS.BASE}/${id}`
    })
  );
}

/** 批量删除。 */
export function fetchBatchDeleteSupplier(ids: string[]) {
  return withSelectionOptionInvalidation(
    SUPPLIER_URLS.BASE,
    request<Api.Supplier.Entity>({
      data: ids,
      method: 'delete',
      url: SUPPLIER_URLS.BATCH_DELETE
    })
  );
}

/** 启用或禁用。 */
export function fetchToggleSupplierStatus(params: Api.Base.ToggleStatusParams) {
  return withSelectionOptionInvalidation(
    SUPPLIER_URLS.BASE,
    request<Api.Supplier.Entity>({
      method: 'patch',
      params: { status: params.status },
      url: `${SUPPLIER_URLS.BASE}/${params.id}/status`
    })
  );
}
