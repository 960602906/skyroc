import { request } from '../request';
import { AFTER_SALE_URLS } from '../urls';

/** 分页查询售后单。 */
export function fetchGetAfterSaleList(params?: Api.AfterSale.SearchParams) {
  return request<Api.AfterSale.List>({
    method: 'get',
    params,
    url: AFTER_SALE_URLS.BASE
  });
}

/** 创建待提交售后单并固化来源业务快照。 */
export function fetchAddAfterSale(data: Api.AfterSale.CreatePayload) {
  return request<Api.AfterSale.Entity>({
    data,
    method: 'post',
    url: AFTER_SALE_URLS.BASE
  });
}

/** 更新待提交售后单并原子替换全部商品申请行。 */
export function fetchUpdateAfterSale(data: Api.AfterSale.UpdatePayload) {
  return request<Api.AfterSale.Entity>({
    data,
    method: 'put',
    url: AFTER_SALE_URLS.BASE
  });
}

/** 分页查询取货任务及其销售退货入库衔接状态。 */
export function fetchGetAfterSalePickupTasks(params?: Api.AfterSale.PickupTaskSearchParams) {
  return request<Api.AfterSale.PickupTaskList>({
    method: 'get',
    params,
    url: AFTER_SALE_URLS.PICKUP_TASKS
  });
}

/** 查询单个取货任务的售后来源、调度和履约详情。 */
export function fetchGetAfterSalePickupTaskDetail(id: string) {
  return request<Api.AfterSale.PickupTask>({
    method: 'get',
    url: `${AFTER_SALE_URLS.PICKUP_TASKS}/${id}`
  });
}

/** 为尚未开始的取货任务分配或更换启用司机。 */
export function fetchUpdateAfterSalePickupTasksAssign(id: string, data: Api.AfterSale.AssignPickupTaskPayload) {
  return request<Api.AfterSale.PickupTask>({
    data,
    method: 'put',
    url: `${AFTER_SALE_URLS.PICKUP_TASKS}/${id}/assign`
  });
}

/** 完成取货中的任务，使其可以作为销售退货入库来源。 */
export function fetchPickupTasksCompleteAfterSale(id: string) {
  return request<Api.AfterSale.PickupTask>({
    method: 'post',
    url: `${AFTER_SALE_URLS.PICKUP_TASKS}/${id}/complete`
  });
}

/** 开始执行已分配的取货任务并记录开始时间。 */
export function fetchPickupTasksStartAfterSale(id: string) {
  return request<Api.AfterSale.PickupTask>({
    method: 'post',
    url: `${AFTER_SALE_URLS.PICKUP_TASKS}/${id}/start`
  });
}

/** 删除从未提交过的售后草稿。 */
export function fetchDeleteAfterSale(id: string) {
  return request<Api.AfterSale.Entity>({
    method: 'delete',
    url: `${AFTER_SALE_URLS.BASE}/${id}`
  });
}

/** 查询售后单商品明细和审核轨迹。 */
export function fetchGetAfterSaleDetail(id: string) {
  return request<Api.AfterSale.Entity>({
    method: 'get',
    url: `${AFTER_SALE_URLS.BASE}/${id}`
  });
}

/** 审核通过售后并进入实物或退款处理；退货退款商品会幂等生成取货任务。 */
export function fetchApproveAfterSale(id: string, data?: Api.AfterSale.ActionParams) {
  return request<Api.AfterSale.Entity>({
    data,
    method: 'post',
    url: `${AFTER_SALE_URLS.BASE}/${id}/approve`
  });
}

/** 完成待退货、补货、换货或待退款处理；退货退款必须已完成并审核退货入库。 */
export function fetchCompleteAfterSale(id: string, data?: Api.AfterSale.ActionParams) {
  return request<Api.AfterSale.Entity>({
    data,
    method: 'post',
    url: `${AFTER_SALE_URLS.BASE}/${id}/complete`
  });
}

/** 驳回待审核售后到可修改草稿。 */
export function fetchRejectAfterSale(id: string, data?: Api.AfterSale.ActionParams) {
  return request<Api.AfterSale.Entity>({
    data,
    method: 'post',
    url: `${AFTER_SALE_URLS.BASE}/${id}/reject`
  });
}

/** 将已驳回并修正的售后单重新提交审核。 */
export function fetchResubmitAfterSale(id: string, data?: Api.AfterSale.ActionParams) {
  return request<Api.AfterSale.Entity>({
    data,
    method: 'post',
    url: `${AFTER_SALE_URLS.BASE}/${id}/resubmit`
  });
}

/** 撤销尚未产生下游取货任务的审核结论。 */
export function fetchReverseAfterSale(id: string, data?: Api.AfterSale.ActionParams) {
  return request<Api.AfterSale.Entity>({
    data,
    method: 'post',
    url: `${AFTER_SALE_URLS.BASE}/${id}/reverse`
  });
}

/** 首次提交售后草稿进入待审核。 */
export function fetchSubmitAfterSale(id: string, data?: Api.AfterSale.ActionParams) {
  return request<Api.AfterSale.Entity>({
    data,
    method: 'post',
    url: `${AFTER_SALE_URLS.BASE}/${id}/submit`
  });
}
