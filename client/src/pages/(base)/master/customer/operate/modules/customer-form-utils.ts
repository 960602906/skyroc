import type { Dayjs } from 'dayjs';
import dayjs from 'dayjs';

import { formatBusinessDate, toBackendDate } from '@/utils/datetime';

export function formatDateValue(value: unknown) {
  return toBackendDate(value as Dayjs | string | number | Date | null | undefined);
}

export function toCustomerFormValues(detail: Api.Customer.Entity) {
  const establishDate = formatBusinessDate(detail.establishDate);
  return {
    ...detail,
    // 与 DatePicker 字符串值约定一致；getValueProps 再转 dayjs
    establishDate: establishDate ? dayjs(establishDate) : null
  };
}

export function normalizeCustomerPayload(
  values: Record<string, unknown>
): Api.Customer.CreateParams | Api.Customer.UpdateParams {
  return {
    ...(values as Api.Customer.CreateParams),
    establishDate: formatDateValue(values.establishDate)
  };
}
