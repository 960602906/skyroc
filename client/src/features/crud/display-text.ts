import type { DescriptionsProps } from 'antd';

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

/** 金额展示：保留 2 位小数；null/undefined/NaN 显示 `-` */
export function displayMoney(value: number | null | undefined) {
  if (value === null || value === undefined || Number.isNaN(Number(value))) {
    return DETAIL_EMPTY;
  }
  return Number(value).toFixed(2);
}

/** 数量展示（可带单位）；null/undefined/NaN 显示 `-`；有单位返回 `123 个` */
export function displayQuantity(value: number | null | undefined, unit?: string | null) {
  if (value === null || value === undefined || Number.isNaN(Number(value))) {
    return DETAIL_EMPTY;
  }
  return unit ? `${value} ${unit}` : String(value);
}

/** 详情页默认 DescriptionsProps：响应式 2 列、显式覆盖 antd xxl/xl 默认值 */
export const DEFAULT_DETAIL_DESC_PROPS: Pick<DescriptionsProps, 'column' | 'size'> = {
  column: { lg: 2, md: 2, sm: 1, xl: 2, xs: 1, xxl: 2 },
  size: 'middle'
};
