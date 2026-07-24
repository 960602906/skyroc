/** 前端统一的时间显示与选择器格式。 页面禁止硬编码 YYYY-MM-DD / HH:mm:ss 等格式串。 */
export const DISPLAY_FORMATS = {
  /** 业务日期展示格式 */
  DATE: 'YYYY-MM-DD',
  /** 审计时间展示格式（本地时区） */
  DATETIME: 'YYYY-MM-DD HH:mm:ss',
  /** 时分秒展示格式 */
  TIME: 'HH:mm:ss',
  /** 年月展示格式 */
  YEAR_MONTH: 'YYYY-MM'
} as const;

/** Antd DatePicker / RangePicker 的 format 属性。 */
export const PICKER_FORMATS = {
  DATE: 'YYYY-MM-DD',
  DATETIME: 'YYYY-MM-DD HH:mm:ss',
  MONTH: 'YYYY-MM',
  YEAR: 'YYYY'
} as const;
