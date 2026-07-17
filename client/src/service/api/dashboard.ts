import { request } from '../request';
import { DASHBOARD_URLS } from '../urls';

/** 查询统计周期内已签收订单的经营概览。 */
export function fetchGetDashboardBrief(params?: Api.Dashboard.SearchParams) {
  return request<Api.Dashboard.Result>({
    method: 'get',
    params,
    url: DASHBOARD_URLS.BRIEF
  });
}

/** 查询按客户验收销售额降序截取的客户销售排行。 */
export function fetchGetDashboardCustomerSalesRank(params?: Api.Dashboard.SearchParams) {
  return request<Api.Dashboard.Result>({
    method: 'get',
    params,
    url: DASHBOARD_URLS.CUSTOMER_SALES_RANK
  });
}

/** 查询按客户验收销售额降序截取的商品分类销售排行。 */
export function fetchGetDashboardGoodsTypeSalesRank(params?: Api.Dashboard.SearchParams) {
  return request<Api.Dashboard.Result>({
    method: 'get',
    params,
    url: DASHBOARD_URLS.GOODS_TYPE_SALES_RANK
  });
}

/** 查询按取货任务创建时间筛选并按当前状态汇总的取货任务数。 */
export function fetchGetDashboardPickupStatuses(params?: Api.Dashboard.SearchParams) {
  return request<Api.Dashboard.Result>({
    method: 'get',
    params,
    url: DASHBOARD_URLS.PICKUP_STATUSES
  });
}

/** 查询按客户账单业务日期汇总的应收、已结和待结金额。 */
export function fetchGetDashboardReconciliation(params?: Api.Dashboard.SearchParams) {
  return request<Api.Dashboard.Result>({
    method: 'get',
    params,
    url: DASHBOARD_URLS.RECONCILIATION
  });
}

/** 查询按订单日期排列的已签收销售趋势。 */
export function fetchGetDashboardSalesTrend(params?: Api.Dashboard.SearchParams) {
  return request<Api.Dashboard.Result>({
    method: 'get',
    params,
    url: DASHBOARD_URLS.SALES_TREND
  });
}
