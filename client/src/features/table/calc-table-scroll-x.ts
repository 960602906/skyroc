/** 可参与横向宽度累计的列字段（兼容 antd width 与业务侧 minWidth） */
export type TableScrollColumnLike =
  | {
      minWidth?: number;
      width?: number | string;
    }
  | null
  | undefined;

export type CalcTableScrollXOptions = {
  /** 未声明 width/minWidth 时的默认列宽 */
  defaultColumnWidth?: number;
  /** 勾选列、序号间隙等额外宽度 */
  extraWidth?: number;
  /** 最小横向滚动宽度，避免列过少时过窄 */
  minScrollX?: number;
};

const DEFAULT_COLUMN_WIDTH = 120;
const DEFAULT_EXTRA_WIDTH = 48;
const DEFAULT_MIN_SCROLL_X = 702;

/** 解析单列宽度：优先 width，其次 minWidth，否则默认值 */
export function resolveColumnWidth(
  column: TableScrollColumnLike,
  defaultColumnWidth: number = DEFAULT_COLUMN_WIDTH
): number {
  if (!column) {
    return 0;
  }

  const { width } = column;
  if (typeof width === 'number' && Number.isFinite(width) && width > 0) {
    return width;
  }

  if (typeof width === 'string') {
    const parsed = Number.parseFloat(width);
    if (Number.isFinite(parsed) && parsed > 0) {
      return parsed;
    }
  }

  if (typeof column.minWidth === 'number' && Number.isFinite(column.minWidth) && column.minWidth > 0) {
    return column.minWidth;
  }

  return defaultColumnWidth;
}

/** 按列定义累计表格横向 scroll.x。 Ant Design 在 scroll.x 小于列宽总和时会压缩列，而不是出横向滚动条。 */
export function calcTableScrollX(
  columns: readonly TableScrollColumnLike[] = [],
  options: CalcTableScrollXOptions = {}
): number {
  const {
    defaultColumnWidth = DEFAULT_COLUMN_WIDTH,
    extraWidth = DEFAULT_EXTRA_WIDTH,
    minScrollX = DEFAULT_MIN_SCROLL_X
  } = options;

  const columnsWidth = columns.reduce((total, column) => total + resolveColumnWidth(column, defaultColumnWidth), 0);

  return Math.max(minScrollX, Math.ceil(columnsWidth + extraWidth));
}
