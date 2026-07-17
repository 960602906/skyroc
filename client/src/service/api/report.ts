import { request } from '../request';
import { REPORT_URLS } from '../urls';

/** 按售后申请类型、原因和处理方式汇总已完成售后的退款/减免；补货、换货、客户沟通数量计 0。 */
export function fetchGetReportAfterSales(params?: Api.Report.SearchParams) {
  return request<Api.Report.Result>({
    method: 'get',
    params,
    url: REPORT_URLS.AFTER_SALES
  });
}

/** 按商品汇总采购入库与采购退货出库的数量和金额。 */
export function fetchGetReportPurchaseInOutGoods(params?: Api.Report.SearchParams) {
  return request<Api.Report.Result>({
    method: 'get',
    params,
    url: REPORT_URLS.PURCHASE_IN_OUT_GOODS
  });
}

/** 按采购员汇总采购入库与采购退货出库的数量和金额；退货出库通过批次追溯原采购入库采购员。 */
export function fetchGetReportPurchaseInOutPurchasers(params?: Api.Report.SearchParams) {
  return request<Api.Report.Result>({
    method: 'get',
    params,
    url: REPORT_URLS.PURCHASE_IN_OUT_PURCHASERS
  });
}

/** 按供应商汇总采购入库与采购退货出库的数量和金额。 */
export function fetchGetReportPurchaseInOutSuppliers(params?: Api.Report.SearchParams) {
  return request<Api.Report.Result>({
    method: 'get',
    params,
    url: REPORT_URLS.PURCHASE_IN_OUT_SUPPLIERS
  });
}

/** 按订单配送地址快照汇总已签收销售订单的验收数量和金额。 */
export function fetchGetReportSalesAreas(params?: Api.Report.SearchParams) {
  return request<Api.Report.Result>({
    method: 'get',
    params,
    url: REPORT_URLS.SALES_AREAS
  });
}

/** 按商品分类汇总已签收销售订单的验收数量和金额。 */
export function fetchGetReportSalesCategories(params?: Api.Report.SearchParams) {
  return request<Api.Report.Result>({
    method: 'get',
    params,
    url: REPORT_URLS.SALES_CATEGORIES
  });
}

/** 按客户汇总已签收销售订单的验收数量和金额。 */
export function fetchGetReportSalesCustomers(params?: Api.Report.SearchParams) {
  return request<Api.Report.Result>({
    method: 'get',
    params,
    url: REPORT_URLS.SALES_CUSTOMERS
  });
}

/** 按商品汇总已签收销售订单的验收数量和金额。 */
export function fetchGetReportSalesGoods(params?: Api.Report.SearchParams) {
  return request<Api.Report.Result>({
    method: 'get',
    params,
    url: REPORT_URLS.SALES_GOODS
  });
}

/** 按自然日汇总已审核库存入库与出库的数量、金额和单据数。 */
export function fetchGetReportStockDaily(params?: Api.Report.SearchParams) {
  return request<Api.Report.Result>({
    method: 'get',
    params,
    url: REPORT_URLS.STOCK_DAILY
  });
}

/** 按自然日和商品汇总已审核库存入库与出库的数量和金额。 */
export function fetchGetReportStockDailyGoods(params?: Api.Report.SearchParams) {
  return request<Api.Report.Result>({
    method: 'get',
    params,
    url: REPORT_URLS.STOCK_DAILY_GOODS
  });
}
