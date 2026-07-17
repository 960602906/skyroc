import { request } from '../request';
import { STOCK_URLS } from '../urls';

/** 分页查询库存批次余额、成本、生产日期和到期日期。需要库存读取权限。 */
export function fetchGetStockBatches(params?: Api.Stock.SearchParams) {
  return request<Api.Stock.List>({
    method: 'get',
    params,
    url: STOCK_URLS.BATCHES
  });
}

/** 分页查询审核与反审核产生的库存增减台账，按发生时间倒序返回。需要库存读取权限。 */
export function fetchGetStockLedgers(params?: Api.Stock.SearchParams) {
  return request<Api.Stock.List>({
    method: 'get',
    params,
    url: STOCK_URLS.LEDGERS
  });
}

/** 按仓库和商品分页汇总当前数量、可用数量、占用量与货值。需要库存读取权限。 */
export function fetchGetStockOverview(params?: Api.Stock.SearchParams) {
  return request<Api.Stock.List>({
    method: 'get',
    params,
    url: STOCK_URLS.OVERVIEW
  });
}
