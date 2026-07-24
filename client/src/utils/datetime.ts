import type { Dayjs } from 'dayjs';
import dayjs from 'dayjs';
import customParseFormat from 'dayjs/plugin/customParseFormat';
import utc from 'dayjs/plugin/utc';

import { DISPLAY_FORMATS } from '@/constants/datetime';

dayjs.extend(utc);
dayjs.extend(customParseFormat);

/** 兼容旧后端无时区后缀的墙钟格式（仍按 UTC 解析） */
const LEGACY_BACKEND_DATETIME = 'YYYY-MM-DD HH:mm:ss';

function isEmpty(value: unknown): boolean {
  return value === null || value === undefined || value === '';
}

/**
 * 将后端 UTC 时间戳转为本地时区展示。 后端 CreateTime/UpdateTime 等为 UTC，JSON 为 ISO 8601（如 `2026-07-24T15:30:00Z`）。 兼容历史无后缀格式 `yyyy-MM-dd
 * HH:mm:ss`（按 UTC 墙钟解析）。
 */
export function formatUtcDateTime(value: string | number | Date | null | undefined) {
  if (isEmpty(value)) {
    return null;
  }

  const text = String(value).trim();
  if (!text) {
    return null;
  }

  // ISO 8601（含 Z / 偏移）优先
  let parsed = dayjs.utc(text);
  if (!parsed.isValid()) {
    // 兼容旧格式：无时区后缀，按 UTC 解析
    parsed = dayjs.utc(text, LEGACY_BACKEND_DATETIME, true);
  }
  if (!parsed.isValid()) {
    return text;
  }

  return parsed.local().format(DISPLAY_FORMATS.DATETIME);
}

/** 业务日期（下单日等）：只取日期，不做本地时区换算，避免跨日。 后端可能返回 `YYYY-MM-DD` 或 UTC 午夜 ISO 字符串。 */
export function formatBusinessDate(value: string | number | Date | null | undefined) {
  if (isEmpty(value)) {
    return null;
  }

  const text = String(value).trim();
  if (!text) {
    return null;
  }

  // 用 utc 取日历日，避免东八区把 00:00Z 显示成前一天
  const parsed = dayjs.utc(text);
  return parsed.isValid() ? parsed.format(DISPLAY_FORMATS.DATE) : null;
}

/** 详情/表格空值占位 */
export function displayUtcDateTime(value: string | number | Date | null | undefined, empty = '-') {
  return formatUtcDateTime(value) ?? empty;
}

export function displayBusinessDate(value: string | number | Date | null | undefined, empty = '-') {
  return formatBusinessDate(value) ?? empty;
}

/** 业务日期提交：统一 `YYYY-MM-DD` 字符串。 不要拼本地时分秒或随意加 Z，除非接口明确要求时刻。 */
export function toBackendDate(date: Dayjs | string | number | Date | null | undefined): string | null {
  if (isEmpty(date)) {
    return null;
  }

  const parsed = dayjs.isDayjs(date) ? date : dayjs(date);
  if (!parsed.isValid()) {
    const text = String(date).trim();
    return text || null;
  }

  return parsed.format(DISPLAY_FORMATS.DATE);
}

/** 时刻提交：将本地日历时间转为 ISO 8601 UTC（带 Z）。 用于需要完整时刻的字段；业务日历日请用 `toBackendDate`。 */
export function toBackendDateTime(date: Dayjs | string | number | Date | null | undefined): string | null {
  if (isEmpty(date)) {
    return null;
  }

  const parsed = dayjs.isDayjs(date) ? date : dayjs(date);
  if (!parsed.isValid()) {
    return null;
  }

  return parsed.utc().format();
}

/** 查询区间边界：本地日 start/end 后转 UTC ISO 8601。 用于事件时间戳类筛选；业务日筛选优先 `toBackendDate`（后端可扩整天）。 */
export function toBackendUtcBoundary(
  value: Dayjs | string | number | Date | null | undefined,
  edge: 'end' | 'start'
): string | null {
  if (isEmpty(value)) {
    return null;
  }

  const parsed = dayjs.isDayjs(value) ? value : dayjs(value);
  if (!parsed.isValid()) {
    return null;
  }

  return parsed[edge === 'start' ? 'startOf' : 'endOf']('day').utc().format();
}
