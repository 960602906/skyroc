import type { TablePaginationConfig } from 'antd';

export function createIndexColumn(t: App.I18n.$T) {
  return {
    align: 'center' as const,
    dataIndex: 'index' as const,
    key: 'index' as const,
    title: t('common.index'),
    width: 64
  };
}

export function createDefaultPagination(): TablePaginationConfig {
  return { showQuickJumper: true };
}

export async function toggleEntityStatus(options: {
  params: Api.Base.ToggleStatusParams;
  refresh: (resetCurrent?: boolean) => Promise<void>;
  t: App.I18n.$T;
  toggleFn: (params: Api.Base.ToggleStatusParams) => Promise<unknown>;
}) {
  const { params, refresh, t, toggleFn } = options;

  await toggleFn(params);
  window.$message?.success(t('common.updateSuccess'));
  await refresh(false);
}

export function createDefaultSearchParams(): Api.Base.SearchParams {
  return {
    code: null,
    current: 1,
    name: null,
    size: 10,
    status: null
  };
}
