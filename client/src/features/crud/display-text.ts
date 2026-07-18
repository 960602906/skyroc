/** 详情描述空值占位 */
export const DETAIL_EMPTY = '-';

/** 详情描述字段展示：null / undefined / 空字符串显示 `-`，其余转字符串。 */
export function displayText(value: string | number | boolean | null | undefined) {
  if (value === null || value === undefined || value === '') {
    return DETAIL_EMPTY;
  }
  return String(value);
}
