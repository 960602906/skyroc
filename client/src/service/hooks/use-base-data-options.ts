import { useQuery } from '@tanstack/react-query';

import {
  fetchGetAllGoodsTypes,
  fetchGetBoundedSelectionOptions,
  fetchGetGoodsTypeTree,
  fetchGetGoodsUnitsByGoods,
  fetchSearchSelectionOptions
} from '@/service/api';
import { QUERY_KEYS } from '@/service/keys';

import { SELECTION_OPTION_RESOURCES, selectionOptionQueryKey } from './use-remote-options';

type LightweightNamedOption = {
  code?: null | string;
  id: string;
  name: string;
};

function mapLightweightOptions(data?: Api.SelectionOption.Entity[]): LightweightNamedOption[] {
  return data?.map(item => ({ code: item.secondaryText, id: item.id, name: item.label })) ?? [];
}

function useBoundedOptions(resource: string, queryKey: readonly unknown[]) {
  return useQuery({
    queryFn: () => fetchGetBoundedSelectionOptions(resource),
    queryKey: [...selectionOptionQueryKey(resource), 'bounded', queryKey],
    select: mapLightweightOptions,
    staleTime: 60_000
  });
}

function useLimitedOptions(resource: string) {
  return useQuery({
    queryFn: () => fetchSearchSelectionOptions(resource, { limit: 20 }),
    queryKey: [...selectionOptionQueryKey(resource), 'legacy-default'],
    select: data => mapLightweightOptions(data.items),
    staleTime: 60_000
  });
}

function useLimitedSelectOptions(resource: string) {
  return useQuery({
    queryFn: () => fetchSearchSelectionOptions(resource, { limit: 20 }),
    queryKey: [...selectionOptionQueryKey(resource), 'legacy-select-default'],
    select: data => data.items.map(item => ({ label: item.label, value: item.id })),
    staleTime: 60_000
  });
}

function toOptions<T extends { id: string; name: string }>(data?: T[]) {
  return data?.map(item => ({ label: item.name, value: item.id })) ?? [];
}

export function useCompanyOptions() {
  return useBoundedOptions('/companies', QUERY_KEYS.BASE.COMPANIES);
}

/** @deprecated 新选择控件请使用 RemoteOptionSelect。 */
export function useCustomerOptions() {
  return useLimitedOptions(SELECTION_OPTION_RESOURCES.CUSTOMER);
}

export function useCustomerTagOptions() {
  return useBoundedOptions('/customer-tags', QUERY_KEYS.BASE.CUSTOMER_TAGS);
}

/** 查询全部司机，供配送与售后取货任务调度、筛选复用。 */
export function useDriverOptions() {
  return useBoundedOptions('/drivers', QUERY_KEYS.BASE.DRIVERS);
}

/** @deprecated 新选择控件请使用 RemoteOptionSelect。 */
export function useGoodsOptions() {
  return useLimitedOptions(SELECTION_OPTION_RESOURCES.GOODS);
}

export function useGoodsTypeOptions() {
  return useQuery({
    queryFn: () => fetchGetAllGoodsTypes(),
    queryKey: QUERY_KEYS.BASE.GOODS_TYPES,
    staleTime: 60_000
  });
}

export function useGoodsTypeTreeOptions() {
  return useQuery({
    queryFn: fetchGetGoodsTypeTree,
    queryKey: QUERY_KEYS.BASE.GOODS_TYPE_TREE,
    staleTime: 60_000
  });
}

export function useGoodsUnitsByGoodsOptions(goodsId?: string | null) {
  return useQuery({
    enabled: Boolean(goodsId),
    queryFn: () => fetchGetGoodsUnitsByGoods(goodsId!),
    queryKey: QUERY_KEYS.BASE.GOODS_UNITS_BY_GOODS(goodsId ?? ''),
    select: data =>
      data?.map(item => ({
        label: item.name ?? item.code ?? item.id,
        value: item.id
      })) ?? [],
    staleTime: 30_000
  });
}

export function usePurchaserOptions() {
  return useBoundedOptions('/purchasers', QUERY_KEYS.BASE.PURCHASERS);
}

/** @deprecated 新选择控件请使用 RemoteOptionSelect。 */
export function useQuotationOptions() {
  return useLimitedSelectOptions(SELECTION_OPTION_RESOURCES.QUOTATION);
}

/** @deprecated 新选择控件请使用 RemoteOptionSelect。 */
export function useProtocolOptions() {
  return useLimitedSelectOptions(SELECTION_OPTION_RESOURCES.PROTOCOL);
}

/** @deprecated 新选择控件请使用 RemoteOptionSelect。 */
export function useSupplierOptions() {
  return useLimitedOptions(SELECTION_OPTION_RESOURCES.SUPPLIER);
}

export function useWareOptions() {
  return useBoundedOptions('/wares', QUERY_KEYS.BASE.WARES);
}

export { toOptions };
