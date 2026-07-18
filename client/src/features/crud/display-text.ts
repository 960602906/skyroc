import { displayBusinessDate, displayUtcDateTime } from '@/utils/datetime';

/** 详情描述空值占位 */
export const DETAIL_EMPTY = '-';

/** 详情描述字段展示：null / undefined / 空字符串显示 `-`，其余转字符串。 */
export function displayText(value: string | number | boolean | null | undefined) {
  if (value === null || value === undefined || value === '') {
    return DETAIL_EMPTY;
  }
  return String(value);
}

/** 审计时间戳（CreateTime 等）：后端 UTC → 本地时区 */
export function displayDateTime(value: string | number | Date | null | undefined) {
  return displayUtcDateTime(value, DETAIL_EMPTY);
}

/** 业务日期（下单日等）：只展示日期，不做时区换算 */
export function displayDate(value: string | number | Date | null | undefined) {
  return displayBusinessDate(value, DETAIL_EMPTY);
}
