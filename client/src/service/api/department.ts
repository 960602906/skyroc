import { request } from '../request';
import { DEPARTMENT_URLS } from '../urls';

/** 获取部门树 */
export function fetchGetDepartmentTree() {
  return request<Api.Department.Tree>({
    method: 'get',
    url: DEPARTMENT_URLS.TREE
  });
}

/** 根据 id 获取部门 */
export function fetchGetDepartmentDetail(id: string) {
  return request<Api.Department.Entity>({
    method: 'get',
    url: `${DEPARTMENT_URLS.BASE}/${id}`
  });
}

/** 创建部门 */
export function fetchAddDepartment(data: Api.Department.CreateParams) {
  return request<Api.Department.Entity>({
    data,
    method: 'post',
    url: DEPARTMENT_URLS.BASE
  });
}

/** 更新部门 */
export function fetchUpdateDepartment(data: Api.Department.UpdateParams) {
  return request<Api.Department.Entity>({
    data,
    method: 'put',
    url: DEPARTMENT_URLS.BASE
  });
}

/** 删除部门 */
export function fetchDeleteDepartment(id: string) {
  return request<Api.Department.Entity>({
    method: 'delete',
    url: `${DEPARTMENT_URLS.BASE}/${id}`
  });
}

/** 批量删除部门 */
export function fetchBatchDeleteDepartment(ids: string[]) {
  return request<Api.Department.Entity>({
    data: ids,
    method: 'delete',
    url: DEPARTMENT_URLS.BATCH_DELETE
  });
}

/** 切换部门状态 */
export function fetchToggleDepartmentStatus(params: Api.Base.ToggleStatusParams) {
  return request<Api.Department.Entity>({
    method: 'patch',
    params: { status: params.status },
    url: `${DEPARTMENT_URLS.BASE}/${params.id}/status`
  });
}

/** 获取部门下的用户列表 */
export function fetchGetDepartmentUsers(id: string) {
  return request<Api.User.Entity[]>({
    method: 'get',
    url: `${DEPARTMENT_URLS.BASE}/${id}/users`
  });
}
