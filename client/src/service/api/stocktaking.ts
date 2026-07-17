import { request } from '../request';
import { STOCKTAKING_URLS } from '../urls';

/** 创建库存盘点草稿，按当前批次余额固化账面数量和成本并计算差异。需要库存创建权限。 */
export function fetchAddStocktaking(data?: Api.Stocktaking.Payload) {
  return request<Api.Stocktaking.Entity>({
    data,
    method: 'post',
    url: STOCKTAKING_URLS.BASE
  });
}

/** 分页查询库存盘点单和批次账实差异。需要库存读取权限。 */
export function fetchGetStocktakingList(params?: Api.Stocktaking.SearchParams) {
  return request<Api.Stocktaking.List>({
    method: 'get',
    params,
    url: STOCKTAKING_URLS.LIST
  });
}

/** 查询库存盘点单完整详情。需要库存读取权限。 */
export function fetchGetStocktakingDetail(id: string) {
  return request<Api.Stocktaking.Entity>({
    method: 'get',
    url: `${STOCKTAKING_URLS.BASE}/${id}`
  });
}

/** 审核库存盘点，锁定批次后执行盘盈盘亏并只追加调整流水。需要库存更新权限。 */
export function fetchAuditStocktaking(id: string, data?: Api.Stocktaking.Payload) {
  return request<Api.Stocktaking.Entity>({
    data,
    method: 'post',
    url: `${STOCKTAKING_URLS.BASE}/${id}/audit`
  });
}
