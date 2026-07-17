import { request } from '../request';
import { DELIVERY_EXCEPTION_URLS } from '../urls';

/** 分页查询配送异常及其任务、司机和客户信息。需要配送读取权限。 */
export function fetchGetDeliveryExceptionList(params?: Api.DeliveryException.SearchParams) {
  return request<Api.DeliveryException.List>({
    method: 'get',
    params,
    url: DELIVERY_EXCEPTION_URLS.BASE
  });
}

/** 为已分配且尚未签收的配送任务登记异常，并同步任务异常状态。需要配送创建权限。 */
export function fetchAddDeliveryException(data?: Api.DeliveryException.Payload) {
  return request<Api.DeliveryException.Entity>({
    data,
    method: 'post',
    url: DELIVERY_EXCEPTION_URLS.BASE
  });
}

/** 查询配送异常详情。需要配送读取权限。 */
export function fetchGetDeliveryExceptionDetail(id: string) {
  return request<Api.DeliveryException.Entity>({
    method: 'get',
    url: `${DELIVERY_EXCEPTION_URLS.BASE}/${id}`
  });
}

/** 完成待处理配送异常；没有其他待处理异常时恢复任务执行状态。需要配送更新权限。 */
export function fetchUpdateDeliveryExceptionHandle(id: string, data?: Api.DeliveryException.Payload) {
  return request<Api.DeliveryException.Entity>({
    data,
    method: 'put',
    url: `${DELIVERY_EXCEPTION_URLS.BASE}/${id}/handle`
  });
}
