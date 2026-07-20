import { useQuery } from '@tanstack/react-query';

import { fetchGetDashboardPickupStatuses, fetchGetDashboardReconciliation } from '../api';
import { QUERY_KEYS } from '../keys';

/** 查询统计周期内的客户对账金额汇总。 */
export function useDashboardReconciliation(params?: Api.Dashboard.SearchParams) {
  return useQuery({
    queryFn: () => fetchGetDashboardReconciliation(params),
    queryKey: QUERY_KEYS.DASHBOARD.RECONCILIATION(params)
  });
}

/** 查询统计周期内按当前履约状态汇总的取货任务数量。 */
export function useDashboardPickupStatuses(params?: Api.Dashboard.SearchParams) {
  return useQuery({
    queryFn: () => fetchGetDashboardPickupStatuses(params),
    queryKey: QUERY_KEYS.DASHBOARD.PICKUP_STATUSES(params)
  });
}
