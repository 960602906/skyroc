import { request } from '../request';
import { TRACEABILITY_URLS } from '../urls';

/** 分页查询检测报告。 */
export function fetchGetTraceabilityInspectionReports(params?: Api.Traceability.SearchParams) {
  return request<Api.Traceability.Result>({
    method: 'get',
    params,
    url: TRACEABILITY_URLS.INSPECTION_REPORTS
  });
}

/** 创建检测报告并固化采购入库、商品、仓库和供应商快照。 */
export function fetchInspectionReportsTraceability(data?: Api.Traceability.Payload) {
  return request<Api.Traceability.Entity>({
    data,
    method: 'post',
    url: TRACEABILITY_URLS.INSPECTION_REPORTS
  });
}

/** 分页查询可创建检测报告的已审核采购入库单。 */
export function fetchGetTraceabilityInspectionReportsEligibleStockIns(params?: Api.Traceability.SearchParams) {
  return request<Api.Traceability.Result>({
    method: 'get',
    params,
    url: TRACEABILITY_URLS.INSPECTION_REPORTS_ELIGIBLE_STOCK_INS
  });
}

/** 读取指定已审核采购入库单的商品明细，供客户端选择送检商品。 */
export function fetchGetTraceabilityInspectionReportsEligibleStockInsDetails(
  stockInOrderId: string,
  params?: Api.Traceability.SearchParams
) {
  return request<Api.Traceability.Result>({
    method: 'get',
    params,
    url: `${TRACEABILITY_URLS.INSPECTION_REPORTS_ELIGIBLE_STOCK_INS}/${stockInOrderId}/details`
  });
}

/** 删除未被商品溯源引用的检测报告及其附件。 */
export function fetchDeleteTraceabilityInspectionReports(id: string) {
  return request<Api.Traceability.Entity>({
    method: 'delete',
    url: `${TRACEABILITY_URLS.INSPECTION_REPORTS}/${id}`
  });
}

/** 读取检测报告详情。 */
export function fetchGetTraceabilityInspectionReportsDetail(id: string) {
  return request<Api.Traceability.Entity>({
    method: 'get',
    url: `${TRACEABILITY_URLS.INSPECTION_REPORTS}/${id}`
  });
}

/** 更新未被溯源引用的检测报告；一旦生成二维码溯源，报告全文冻结不可修改。 */
export function fetchUpdateTraceabilityInspectionReports(id: string, data?: Api.Traceability.Payload) {
  return request<Api.Traceability.Entity>({
    data,
    method: 'put',
    url: `${TRACEABILITY_URLS.INSPECTION_REPORTS}/${id}`
  });
}

/** 分页查询外部平台报送日志。 */
export function fetchGetTraceabilityPushLogs(params?: Api.Traceability.SearchParams) {
  return request<Api.Traceability.Result>({
    method: 'get',
    params,
    url: TRACEABILITY_URLS.PUSH_LOGS
  });
}

/** 分页查询商品溯源记录。 */
export function fetchGetTraceabilityTraces(params?: Api.Traceability.SearchParams) {
  return request<Api.Traceability.Result>({
    method: 'get',
    params,
    url: TRACEABILITY_URLS.TRACES
  });
}

/** 读取二维码公开详情，仅返回已固化的商品、批次、供应商、仓库和检测报告快照。 */
export function fetchGetTraceabilityTracesQrDetail(traceNo: string) {
  return request<Api.Traceability.Entity>({
    method: 'get',
    url: `${TRACEABILITY_URLS.TRACES_QR}/${traceNo}`
  });
}

/** 为销售订单中已审核销售出库的商品生成缺失溯源记录。 */
export function fetchTracesSaleOrdersGenerateTraceability(saleOrderId: string, data?: Api.Traceability.Payload) {
  return request<Api.Traceability.Entity>({
    data,
    method: 'post',
    url: `${TRACEABILITY_URLS.TRACES_SALE_ORDERS}/${saleOrderId}/generate`
  });
}
