import { get } from 'lodash-es';

import { $t } from '@/locales';

type FormatFieldValue = boolean | null | number | string | undefined;

type FormatFieldDefaults<T extends object> = Partial<Record<Extract<keyof T, string>, FormatFieldValue>>;

/** 将单个字段值规范为可拼接的文本。 */
function toFormatFieldText(value: unknown): string | null {
  if (value === null || value === undefined) return null;

  if (typeof value === 'string') {
    const trimmedValue = value.trim();
    return trimmedValue || null;
  }

  if (typeof value === 'number' || typeof value === 'boolean') {
    return String(value);
  }

  return null;
}

/**
 * 按字段顺序拼接对象中的可展示值，支持嵌套字段和对象中提供的默认值。
 *
 * `0` 和 `false` 是有效展示值；忽略空白字符串、`null` 与 `undefined`。字段对象中的默认值会在原字段没有可展示值时生效。
 *
 * @param obj 源对象
 * @param fields 要格式化的字段数组，或以字段名为键、默认值为值的对象
 * @param separator 字段间分隔符
 */
export function formatField<T extends object, K extends Extract<keyof T, string>>(
  obj: T,
  fields: readonly K[],
  separator?: string
): string;
export function formatField<T extends object>(obj: T, fields: readonly string[], separator?: string): string;
export function formatField<T extends object>(obj: T, fields: FormatFieldDefaults<T>, separator?: string): string;
export function formatField(obj: object, fields: object | readonly string[], separator = '/'): string {
  const values = Array.isArray(fields)
    ? fields.map(field => toFormatFieldText(get(obj, field)))
    : Object.entries(fields).map(([field, defaultValue]) => {
        const fieldText = toFormatFieldText(get(obj, field));
        return fieldText ?? toFormatFieldText(defaultValue);
      });

  return values.filter((value): value is string => value !== null).join(separator);
}

/**
 * Transform record to option
 *
 * @example
 *   ```ts
 *   const record = {
 *     key1: 'label1',
 *     key2: 'label2'
 *   };
 *   const options = transformRecordToOption(record);
 *   // [
 *   //   { value: 'key1', label: 'label1' },
 *   //   { value: 'key2', label: 'label2' }
 *   // ]
 *   ```;
 *
 * @param record
 */
export function transformRecordToOption<T extends Record<string, string | number>>(
  record: T,
  convertNum: boolean = false
) {
  return Object.entries(record).map(([value, label]) => ({
    label,
    value: convertNum ? Number(value) : value
  })) as CommonType.Option<keyof T>[];
}

/**
 * Translate options
 *
 * @param options
 */
export function translateOptions(options: CommonType.Option<number>[]) {
  return options.map(option => ({
    ...option,
    label: $t(option.label as App.I18n.I18nKey)
  }));
}

/**
 * Toggle html class
 *
 * @param className
 */
export function toggleHtmlClass(className: string) {
  function add() {
    document.documentElement.classList.add(className);
  }

  function remove() {
    document.documentElement.classList.remove(className);
  }

  return {
    add,
    remove
  };
}

export function getKeys(obj: Record<string, any>, parentKeys: string[] = []): string[] {
  let keys: string[] = [];

  for (const key in obj) {
    if (key) {
      const newKeys = [...parentKeys, key];
      if (typeof obj[key] === 'object' && obj[key] !== null && !Array.isArray(obj[key])) {
        keys = keys.concat(getKeys(obj[key], newKeys));
      } else {
        keys = newKeys;
      }
    }
  }

  return keys;
}
