import { useQuery } from '@tanstack/react-query';

import {
  fetchGetAllCompanies,
  fetchGetAllCustomerTags,
  fetchGetAllCustomers,
  fetchGetAllDrivers,
  fetchGetAllGoods,
  fetchGetAllGoodsTypes,
  fetchGetAllGoodsUnits,
  fetchGetAllPurchasers,
  fetchGetAllSuppliers,
  fetchGetAllWares,
  fetchGetCustomerProtocolOptions,
  fetchGetGoodsTypeTree,
  fetchGetGoodsUnitsByGoods,
  fetchGetQuotationOptions
} from '@/service/api';
import { QUERY_KEYS } from '@/service/keys';

function toOptions<T extends { id: string; name: string }>(data?: T[]) {
  return data?.map(item => ({ label: item.name, value: item.id })) ?? [];
}

export function useCompanyOptions() {
  return useQuery({
    queryFn: () => fetchGetAllCompanies(),
    queryKey: QUERY_KEYS.BASE.COMPANIES,
    staleTime: 60_000
  });
}

export function useCustomerOptions() {
  return useQuery({
    queryFn: () => fetchGetAllCustomers(),
    queryKey: QUERY_KEYS.BASE.CUSTOMERS,
    staleTime: 60_000
  });
}

export function useCustomerTagOptions() {
  return useQuery({
    queryFn: () => fetchGetAllCustomerTags(),
    queryKey: QUERY_KEYS.BASE.CUSTOMER_TAGS,
    staleTime: 60_000
  });
}

/** 查询全部司机，供配送与售后取货任务调度、筛选复用。 */
export function useDriverOptions() {
  return useQuery({
    queryFn: () => fetchGetAllDrivers(),
    queryKey: QUERY_KEYS.BASE.DRIVERS,
    staleTime: 60_000
  });
}

export function useGoodsOptions() {
  return useQuery({
    queryFn: () => fetchGetAllGoods(),
    queryKey: QUERY_KEYS.BASE.GOODS,
    staleTime: 60_000
  });
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

export function useGoodsUnitOptions() {
  return useQuery({
    queryFn: () => fetchGetAllGoodsUnits(),
    queryKey: QUERY_KEYS.BASE.GOODS_UNITS,
    select: data =>
      data?.map(item => ({
        label: item.name ?? item.id,
        value: item.id
      })) ?? [],
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
  return useQuery({
    queryFn: () => fetchGetAllPurchasers(),
    queryKey: QUERY_KEYS.BASE.PURCHASERS,
    staleTime: 60_000
  });
}

export function useQuotationOptions() {
  return useQuery({
    queryFn: () => fetchGetQuotationOptions(),
    queryKey: QUERY_KEYS.BASE.QUOTATIONS,
    select: data => toOptions(data),
    staleTime: 60_000
  });
}

export function useProtocolOptions() {
  return useQuery({
    queryFn: () => fetchGetCustomerProtocolOptions(),
    queryKey: QUERY_KEYS.BASE.PROTOCOLS,
    select: data => toOptions(data),
    staleTime: 60_000
  });
}

export function useSupplierOptions() {
  return useQuery({
    queryFn: () => fetchGetAllSuppliers(),
    queryKey: QUERY_KEYS.BASE.SUPPLIERS,
    staleTime: 60_000
  });
}

export function useWareOptions() {
  return useQuery({
    queryFn: () => fetchGetAllWares(),
    queryKey: QUERY_KEYS.BASE.WARES,
    staleTime: 60_000
  });
}

export { toOptions };
