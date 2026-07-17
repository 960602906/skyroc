import { request } from '../request';
import { COMPANY_URLS } from '../urls';

/** 分页查询。 */
export function fetchGetCompanyList(params?: Api.Company.SearchParams) {
  return request<Api.Company.List>({
    method: 'get',
    params,
    url: COMPANY_URLS.LIST
  });
}

/** 查询全部。 */
export function fetchGetAllCompanies() {
  return request<Api.Company.AllEntity[]>({
    method: 'get',
    url: COMPANY_URLS.BASE
  });
}

/** 根据 ID 查询。 */
export function fetchGetCompanyDetail(id: string) {
  return request<Api.Company.Entity>({
    method: 'get',
    url: `${COMPANY_URLS.BASE}/${id}`
  });
}

/** 创建。 */
export function fetchAddCompany(data: Api.Company.CreateParams) {
  return request<Api.Company.Entity>({
    data,
    method: 'post',
    url: COMPANY_URLS.BASE
  });
}

/** 更新。 */
export function fetchUpdateCompany(data: Api.Company.UpdateParams) {
  return request<Api.Company.Entity>({
    data,
    method: 'put',
    url: COMPANY_URLS.BASE
  });
}

/** 删除。 */
export function fetchDeleteCompany(id: string) {
  return request<Api.Company.Entity>({
    method: 'delete',
    url: `${COMPANY_URLS.BASE}/${id}`
  });
}

/** 批量删除。 */
export function fetchBatchDeleteCompany(ids: string[]) {
  return request<Api.Company.Entity>({
    data: ids,
    method: 'delete',
    url: COMPANY_URLS.BATCH_DELETE
  });
}

/** 启用或禁用。 */
export function fetchToggleCompanyStatus(params: Api.Base.ToggleStatusParams) {
  return request<Api.Company.Entity>({
    method: 'patch',
    params: { status: params.status },
    url: `${COMPANY_URLS.BASE}/${params.id}/status`
  });
}
