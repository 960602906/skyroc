import type { TablePaginationConfig } from 'antd';
import type { Dayjs } from 'dayjs';
import dayjs from 'dayjs';

import { toBackendDate, toBackendUtcBoundary } from '@/utils/datetime';

import { toBooleanValue } from './boolean-utils';

export function createIndexColumn(t: App.I18n.$T) {
  return {
    align: 'center' as const,
    dataIndex: 'index' as const,
    fixed: 'left' as const,
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
    current: 1,
    size: 10
  };
}

/** 业务日筛选：格式化为 YYYY-MM-DD（不做时区换算） */
export function formatLocalDate(value: unknown): string | null {
  return toBackendDate(value as Dayjs | string | number | Date | null | undefined);
}

/** 事件时间筛选边界：本地日 start/end 后转 ISO 8601 UTC */
export function formatUtcBoundary(value: unknown, edge: 'end' | 'start'): string | null {
  if (!value) return null;
  const parsed = dayjs.isDayjs(value) ? value : dayjs(String(value));
  if (!parsed.isValid()) return null;
  return toBackendUtcBoundary(parsed, edge);
}

type DateRangeFormatter = (value: unknown, edge: 'end' | 'start') => string | null;

type EnumFormatter = (value: unknown) => number | string;

/**
 * 转换日期范围字段为独立的 start/end 字段
 *
 * 将 RangePicker 输出的二元组展开为后端期望的两个独立字段，并按需删除原始范围字段。
 */
export function transformDateRange<T extends Record<string, any>>(
  params: T,
  config: {
    endKey: keyof T;
    formatter?: DateRangeFormatter;
    rangeKey: keyof T;
    startKey: keyof T;
  }
): T {
  const { endKey, formatter = formatLocalDate as DateRangeFormatter, rangeKey, startKey } = config;
  const next: T = { ...params };
  const range = next[rangeKey] as unknown;

  const apply = (key: keyof T, value: string | null) => {
    if (value === null || value === undefined) {
      const { [key]: _removed, ...rest } = next;
      Object.assign(next, rest);
    } else {
      (next as any)[key] = value;
    }
  };

  if (Array.isArray(range) && range.length === 2) {
    apply(startKey, formatter(range[0], 'start'));
    apply(endKey, formatter(range[1], 'end'));
  } else {
    apply(startKey, null);
    apply(endKey, null);
  }

  apply(rangeKey, null);
  return next;
}

const defaultEnumFormatter: EnumFormatter = value => Number(value);

/**
 * 转换枚举字段（字符串 → 数字）
 *
 * URL 回填的枚举值常为字符串，后端通常需要数字，此函数按字段名批量转换。 空值（null/undefined/''）保持不变。
 */
export function transformEnumFields<T extends Record<string, any>>(
  params: T,
  fields: (keyof T)[],
  formatter: EnumFormatter = defaultEnumFormatter
): T {
  const next: T = { ...params };

  for (const field of fields) {
    const value = next[field];
    if (value !== null && value !== undefined && value !== '') {
      (next as any)[field] = formatter(value);
    }
  }

  return next;
}

/**
 * 转换布尔字段（字符串 → boolean）
 *
 * URL 回填或表单提交的布尔值可能是 'true'/'false'/'1'/'0'，统一转成 boolean。 无法识别的值会被移除，避免脏数据进入查询参数。
 */
export function transformBooleanFields<T extends Record<string, any>>(params: T, fields: (keyof T)[]): T {
  const next: T = { ...params };

  for (const field of fields) {
    const boolValue = toBooleanValue((next as any)[field]);
    if (boolValue === undefined) {
      const { [field]: _removed, ...rest } = next;
      Object.assign(next, rest);
    } else {
      (next as any)[field] = boolValue;
    }
  }

  return next;
}
