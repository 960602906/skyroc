import dayjs from 'dayjs';

export function formatDateValue(value: unknown) {
  if (!value) return null;
  if (dayjs.isDayjs(value)) {
    return value.format('YYYY-MM-DD');
  }
  return String(value);
}

export function toCustomerFormValues(detail: Api.Customer.Entity) {
  return {
    ...detail,
    establishDate: detail.establishDate ? dayjs(detail.establishDate) : null
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
