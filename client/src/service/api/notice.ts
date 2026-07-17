import { request } from '../request';
import { NOTICE_URLS } from '../urls';

/** 分页查询通知公告。 */
export function fetchGetNoticeList(params?: Api.Notice.SearchParams) {
  return request<Api.Notice.List>({
    method: 'get',
    params,
    url: NOTICE_URLS.BASE
  });
}

/** 新建草稿通知公告。 */
export function fetchAddNotice(data?: Api.Notice.Payload) {
  return request<Api.Notice.Entity>({
    data,
    method: 'post',
    url: NOTICE_URLS.BASE
  });
}

/** 删除通知公告。 */
export function fetchDeleteNotice(id: string) {
  return request<Api.Notice.Entity>({
    method: 'delete',
    url: `${NOTICE_URLS.BASE}/${id}`
  });
}

/** 更新通知公告内容，不改变当前发布状态。 */
export function fetchUpdateNotice(id: string, data?: Api.Notice.Payload) {
  return request<Api.Notice.Entity>({
    data,
    method: 'put',
    url: `${NOTICE_URLS.BASE}/${id}`
  });
}

/** 发布或撤回通知公告。 */
export function fetchToggleNoticeStatus(params: Api.Base.ToggleStatusParams) {
  return request<Api.Notice.Entity>({
    method: 'patch',
    params: { status: params.status },
    url: `${NOTICE_URLS.BASE}/${params.id}/status`
  });
}
