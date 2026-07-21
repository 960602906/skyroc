import { queryClient } from '@/service/queryClient';

import { request } from '../request';

/** 限量搜索增长型业务资源的选择项。 */
export function fetchSearchSelectionOptions(resource: string, params?: Api.SelectionOption.SearchParams) {
  return request<Api.SelectionOption.SearchResult>({
    method: 'get',
    params,
    url: `${resource}/options/search`
  });
}

/** 解析已选择的业务资源，供编辑和多选场景恢复显示标签。 */
export function fetchResolveSelectionOptions(resource: string, ids: string[]) {
  // 全局 paramsSerializer 使用 qs.stringify，对 URLSearchParams 会序列化成空串；
  // 也不要用 ids[] / ids[0] 形式，ASP.NET 绑定的是重复键 ids=a&ids=b。
  const query = new URLSearchParams();
  ids.forEach(id => query.append('ids', id));
  const qs = query.toString();
  return request<Api.SelectionOption.Entity[]>({
    method: 'get',
    url: qs ? `${resource}/options/resolve?${qs}` : `${resource}/options/resolve`
  });
}

/** 轻量加载具有明确业务数量边界的选择项。 */
export function fetchGetBoundedSelectionOptions(resource: string) {
  return request<Api.SelectionOption.Entity[]>({
    method: 'get',
    url: `${resource}/options/bounded`
  });
}

/** 写操作成功后失效对应资源的搜索与解析缓存。 */
export async function withSelectionOptionInvalidation<T>(resource: string, operation: Promise<T>) {
  const result = await operation;
  await queryClient.invalidateQueries({ queryKey: ['selection-options', resource] });
  return result;
}
