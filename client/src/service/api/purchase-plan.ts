import { request } from '../request';
import { PURCHASE_PLAN_URLS } from '../urls';

/** 手工新增采购计划及其商品明细。需要采购创建权限。 */
export function fetchAddPurchasePlan(data: Api.PurchasePlan.CreateParams) {
  return request<Api.PurchasePlan.Entity>({
    data,
    method: 'post',
    url: PURCHASE_PLAN_URLS.BASE
  });
}

/** 从已审核通过的销售订单批量生成采购计划。需要采购创建权限。 */
export function fetchGeneratePurchasePlan(data: Api.PurchasePlan.GenerateParams) {
  return request<Api.PurchasePlan.Entity[]>({
    data,
    method: 'post',
    url: PURCHASE_PLAN_URLS.GENERATE
  });
}

/** 分页查询采购计划。需要采购读取权限。 */
export function fetchGetPurchasePlanList(params?: Api.PurchasePlan.SearchParams) {
  return request<Api.PurchasePlan.List>({
    method: 'get',
    params,
    url: PURCHASE_PLAN_URLS.LIST
  });
}

/** 合并采购模式、供应商和采购员一致的未发布采购计划。需要采购更新权限。 */
export function fetchMergePurchasePlan(data: Api.PurchasePlan.MergeParams) {
  return request<Api.PurchasePlan.Entity>({
    data,
    method: 'post',
    url: PURCHASE_PLAN_URLS.MERGE
  });
}

/** 批量分配或清除未发布采购计划的采购员。需要采购更新权限。 */
export function fetchUpdatePurchasePlanPurchaser(data: Api.PurchasePlan.AssignPurchaserParams) {
  return request<Api.PurchasePlan.Entity[]>({
    data,
    method: 'put',
    url: PURCHASE_PLAN_URLS.PURCHASER
  });
}

/** 按来源销售订单拆分未发布采购计划。需要采购更新权限。 */
export function fetchSplitOrdersPurchasePlan(data: Api.PurchasePlan.SplitOrdersParams) {
  return request<Api.PurchasePlan.Entity>({
    data,
    method: 'post',
    url: PURCHASE_PLAN_URLS.SPLIT_ORDERS
  });
}

/** 按商品采购数量拆分未发布采购计划。需要采购更新权限。 */
export function fetchSplitQuantityPurchasePlan(data: Api.PurchasePlan.SplitQuantityParams) {
  return request<Api.PurchasePlan.Entity>({
    data,
    method: 'post',
    url: PURCHASE_PLAN_URLS.SPLIT_QUANTITY
  });
}

/** 批量分配或清除未发布采购计划的供应商。需要采购更新权限。 */
export function fetchUpdatePurchasePlanSupplier(data: Api.PurchasePlan.AssignSupplierParams) {
  return request<Api.PurchasePlan.Entity[]>({
    data,
    method: 'put',
    url: PURCHASE_PLAN_URLS.SUPPLIER
  });
}

/** 查询采购计划详情，含商品明细与订单来源关系。需要采购读取权限。 */
export function fetchGetPurchasePlanDetail(id: string) {
  return request<Api.PurchasePlan.Entity>({
    method: 'get',
    url: `${PURCHASE_PLAN_URLS.BASE}/${id}`
  });
}

/** 查询指定采购计划中可按订单拆分的来源订单。需要采购读取权限。 */
export function fetchGetPurchasePlanSplitOrders(planId: string) {
  return request<Api.PurchasePlan.SplittableOrder[]>({
    method: 'get',
    url: `${PURCHASE_PLAN_URLS.BASE}/${planId}/split-orders`
  });
}
