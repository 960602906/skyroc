import { request } from '../request';
import { DELIVERY_ROUTE_URLS } from '../urls';

/** 查询全部。 */
export function fetchGetAllDeliveryRoutes() {
  return request<Api.DeliveryRoute.AllEntity[]>({
    method: 'get',
    url: DELIVERY_ROUTE_URLS.BASE
  });
}

/** 创建。 */
export function fetchAddDeliveryRoute(data?: Api.DeliveryRoute.Payload) {
  return request<Api.DeliveryRoute.Entity>({
    data,
    method: 'post',
    url: DELIVERY_ROUTE_URLS.BASE
  });
}

/** 更新。 */
export function fetchUpdateDeliveryRoute(data?: Api.DeliveryRoute.Payload) {
  return request<Api.DeliveryRoute.Entity>({
    data,
    method: 'put',
    url: DELIVERY_ROUTE_URLS.BASE
  });
}

/** 批量删除。 */
export function fetchBatchDeleteDeliveryRoute(ids: string[]) {
  return request<Api.DeliveryRoute.Entity>({
    data: ids,
    method: 'delete',
    url: DELIVERY_ROUTE_URLS.BATCH_DELETE
  });
}

/** 分页查询。 */
export function fetchGetDeliveryRouteList(params?: Api.DeliveryRoute.SearchParams) {
  return request<Api.DeliveryRoute.List>({
    method: 'get',
    params,
    url: DELIVERY_ROUTE_URLS.LIST
  });
}

/** 删除。 */
export function fetchDeleteDeliveryRoute(id: string) {
  return request<Api.DeliveryRoute.Entity>({
    method: 'delete',
    url: `${DELIVERY_ROUTE_URLS.BASE}/${id}`
  });
}

/** 根据 ID 查询。 */
export function fetchGetDeliveryRouteDetail(id: string) {
  return request<Api.DeliveryRoute.Entity>({
    method: 'get',
    url: `${DELIVERY_ROUTE_URLS.BASE}/${id}`
  });
}

/** 启用或禁用。 */
export function fetchToggleDeliveryRouteStatus(params: Api.Base.ToggleStatusParams) {
  return request<Api.DeliveryRoute.Entity>({
    method: 'patch',
    params: { status: params.status },
    url: `${DELIVERY_ROUTE_URLS.BASE}/${params.id}/status`
  });
}

/** 分配客户到指定配送路线，用请求集合整体替换该路线的客户关系。 */
export function fetchUpdateDeliveryRouteCustomers(routeId: string, data?: Api.DeliveryRoute.Payload) {
  return request<Api.DeliveryRoute.Entity>({
    data,
    method: 'put',
    url: `${DELIVERY_ROUTE_URLS.BASE}/${routeId}/customers`
  });
}
