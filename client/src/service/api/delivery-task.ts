import { request } from '../request';
import { DELIVERY_TASK_URLS } from '../urls';

/** 分页查询配送订单任务。需要配送读取权限。 */
export function fetchGetDeliveryTaskList(params?: Api.DeliveryTask.SearchParams) {
  return request<Api.DeliveryTask.List>({
    method: 'get',
    params,
    url: DELIVERY_TASK_URLS.BASE
  });
}

/** 分页查询已经分配司机的配送任务。需要配送读取权限。 */
export function fetchGetDeliveryTaskDriver(params?: Api.DeliveryTask.SearchParams) {
  return request<Api.DeliveryTask.Result>({
    method: 'get',
    params,
    url: DELIVERY_TASK_URLS.DRIVER
  });
}

/** 为待分配或已分配配送任务批量指定启用司机。需要配送更新权限。 */
export function fetchUpdateDeliveryTaskDriver(data?: Api.DeliveryTask.Payload) {
  return request<Api.DeliveryTask.Entity>({
    data,
    method: 'put',
    url: DELIVERY_TASK_URLS.DRIVER
  });
}

/** 从已审核销售出库单幂等生成配送任务。需要配送创建权限。 */
export function fetchGenerateDeliveryTask(stockOutOrderId: string, data?: Api.DeliveryTask.Payload) {
  return request<Api.DeliveryTask.Entity>({
    data,
    method: 'post',
    url: `${DELIVERY_TASK_URLS.GENERATE}/${stockOutOrderId}`
  });
}

/** 按客户已配置的启用配送路线批量规划任务。需要配送更新权限。 */
export function fetchUpdateDeliveryTaskIntelligentPlan(data?: Api.DeliveryTask.Payload) {
  return request<Api.DeliveryTask.Entity>({
    data,
    method: 'put',
    url: DELIVERY_TASK_URLS.INTELLIGENT_PLAN
  });
}

/** 查询配送任务详情。需要配送读取权限。 */
export function fetchGetDeliveryTaskDetail(id: string) {
  return request<Api.DeliveryTask.Entity>({
    method: 'get',
    url: `${DELIVERY_TASK_URLS.BASE}/${id}`
  });
}

/** 归档已签收任务的纸质扫描件或电子回单，并在整单完成时同步回单状态。需要配送更新权限。 */
export function fetchUpdateDeliveryTaskReceipt(id: string, data?: Api.DeliveryTask.Payload) {
  return request<Api.DeliveryTask.Entity>({
    data,
    method: 'put',
    url: `${DELIVERY_TASK_URLS.BASE}/${id}/receipt`
  });
}

/** 签收配送中任务，保存全部出库商品验收结果，并在整单完成时同步订单结算状态。需要配送更新权限。 */
export function fetchUpdateDeliveryTaskSign(id: string, data?: Api.DeliveryTask.Payload) {
  return request<Api.DeliveryTask.Entity>({
    data,
    method: 'put',
    url: `${DELIVERY_TASK_URLS.BASE}/${id}/sign`
  });
}

/** 将已分配任务推进到配送中，并同步销售订单状态。需要配送更新权限。 */
export function fetchUpdateDeliveryTaskStart(id: string, data?: Api.DeliveryTask.Payload) {
  return request<Api.DeliveryTask.Entity>({
    data,
    method: 'put',
    url: `${DELIVERY_TASK_URLS.BASE}/${id}/start`
  });
}
