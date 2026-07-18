/** 计算操作列应占栅格：占满当前行剩余，保证重置/搜索贴右（含换行后单独成行）。 */
export function getSearchActionsSpan(fieldCount: number, fieldSpan: number, cols = 24) {
  if (fieldCount < 0 || fieldSpan <= 0) {
    return cols;
  }
  const used = (fieldCount * fieldSpan) % cols;
  return used === 0 ? cols : cols - used;
}
