/** URL / 表单回填可能是字符串或 0/1，统一转成 boolean */
export function toBooleanValue(value: unknown): boolean | undefined {
  if (value === null || value === undefined || value === '') {
    return undefined;
  }
  if (value === true || value === 'true' || value === 1 || value === '1') {
    return true;
  }
  if (value === false || value === 'false' || value === 0 || value === '0') {
    return false;
  }
  return undefined;
}

/** 布尔与下拉内部值（1/0）互转，规避 antd Select 不支持 boolean value */
export type BooleanSelectValue = 0 | 1;

export function booleanToSelectValue(value: unknown): BooleanSelectValue | undefined {
  const bool = toBooleanValue(value);
  if (bool === undefined) {
    return undefined;
  }
  return bool ? 1 : 0;
}

export function selectValueToBoolean(value: unknown): boolean | undefined {
  if (value === null || value === undefined || value === '') {
    return undefined;
  }
  return toBooleanValue(value);
}
