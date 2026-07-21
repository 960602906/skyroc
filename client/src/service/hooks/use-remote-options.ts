import { useQuery } from '@tanstack/react-query';
import { useDebounce } from 'ahooks';

import { fetchResolveSelectionOptions, fetchSearchSelectionOptions } from '@/service/api';

const DEFAULT_LIMIT = 20;

export const SELECTION_OPTION_RESOURCES = {
  CUSTOMER: '/customers',
  GOODS: '/goods',
  ORDER: '/orders',
  PROTOCOL: '/customer-protocols',
  QUOTATION: '/quotations',
  SUPPLIER: '/suppliers'
} as const;

export function selectionOptionQueryKey(resource: string) {
  return ['selection-options', resource] as const;
}

type RemoteOptionsParams = {
  contextKey?: readonly unknown[];
  keyword: string;
  limit?: number;
  open: boolean;
  resource: string;
  value?: unknown;
};

function getSelectedIds(value: unknown) {
  if (Array.isArray(value)) {
    return value.filter((item): item is string => typeof item === 'string');
  }

  return typeof value === 'string' && value ? [value] : [];
}

/** 为远程选择器提供防抖搜索、回填解析和稳定缓存键。 */
export function useRemoteOptions({
  contextKey = [],
  keyword,
  limit = DEFAULT_LIMIT,
  open,
  resource,
  value
}: RemoteOptionsParams) {
  const debouncedKeyword = useDebounce(keyword.trim(), { wait: 300 });
  const selectedIds = getSelectedIds(value);
  const stableIds = [...new Set(selectedIds)].sort();
  const baseKey = selectionOptionQueryKey(resource);
  const searchQuery = useQuery({
    enabled: open,
    queryFn: () =>
      fetchSearchSelectionOptions(resource, {
        keyword: debouncedKeyword || undefined,
        limit
      }),
    queryKey: [...baseKey, 'search', contextKey, debouncedKeyword, limit],
    staleTime: 60_000
  });
  const resolveQuery = useQuery({
    enabled: stableIds.length > 0,
    queryFn: () => fetchResolveSelectionOptions(resource, stableIds),
    queryKey: [...baseKey, 'resolve', contextKey, stableIds],
    staleTime: 60_000
  });

  const merged = new Map<string, Api.SelectionOption.Entity>();
  resolveQuery.data?.forEach(item => merged.set(item.id, item));
  searchQuery.data?.items.forEach(item => merged.set(item.id, item));

  return {
    hasMore: searchQuery.data?.hasMore ?? false,
    isLoading: searchQuery.isFetching || resolveQuery.isFetching,
    options: [...merged.values()].map(item => ({
      label: item.secondaryText ? `${item.label} · ${item.secondaryText}` : item.label,
      value: item.id
    }))
  };
}
