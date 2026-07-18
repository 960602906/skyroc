import dayjs from 'dayjs';
import customParseFormat from 'dayjs/plugin/customParseFormat';
import utc from 'dayjs/plugin/utc';

dayjs.extend(utc);
dayjs.extend(customParseFormat);

/** 后端 FixedDateTime 写出格式（UTC 墙钟，无时区后缀） */
const BACKEND_DATETIME = 'YYYY-MM-DD HH:mm:ss';

const DISPLAY_DATETIME = 'YYYY-MM-DD HH:mm:ss';
const DISPLAY_DATE = 'YYYY-MM-DD';

/**
 * 将后端 UTC 时间字符串转为本地时区展示。 后端 CreateTime/UpdateTime 等为 UTC，JSON 写成 `yyyy-MM-dd HH:mm:ss` 且不带 Z， 前端若原样展示会比本地时间慢约 8
 * 小时（中国时区）。
 */
export function formatUtcDateTime(value: string | number | Date | null | undefined) {
  if (value === null || value === undefined || value === '') {
    return null;
  }

  const text = String(value).trim();
  if (!text) {
    return null;
  }

  // 优先按后端固定格式按 UTC 解析
  let parsed = dayjs.utc(text, BACKEND_DATETIME, true);
  if (!parsed.isValid()) {
    // 兼容 ISO / 带 Z 的写法
    parsed = dayjs.utc(text);
  }
  if (!parsed.isValid()) {
    return text;
  }

  return parsed.local().format(DISPLAY_DATETIME);
}

/** 业务日期（下单日/收货日等）：只取日期部分，不做时区换算。 后端日期-only 会规范为 UTC 午夜再写出，若当时间戳本地化可能跨日。 */
export function formatBusinessDate(value: string | number | Date | null | undefined) {
  if (value === null || value === undefined || value === '') {
    return null;
  }

  const text = String(value).trim();
  if (!text) {
    return null;
  }

  // 已是日期或前 10 位为 yyyy-MM-dd
  if (/^\d{4}-\d{2}-\d{2}/.test(text)) {
    return text.slice(0, 10);
  }

  const parsed = dayjs(text);
  if (!parsed.isValid()) {
    return text;
  }
  return parsed.format(DISPLAY_DATE);
}

/** 详情/表格空值占位 */
export function displayUtcDateTime(value: string | number | Date | null | undefined, empty = '-') {
  return formatUtcDateTime(value) ?? empty;
}

export function displayBusinessDate(value: string | number | Date | null | undefined, empty = '-') {
  return formatBusinessDate(value) ?? empty;
}
