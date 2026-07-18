import { orderDateTypeOptions, orderReturnStatusOptions, saleOrderStatusOptions } from '@/constants/business';

import { OrderReturnStatusBadge, SaleOrderStatusBadge } from './SaleOrderStatusBadge';

/** URL / 表单回填可能是字符串，统一转成数字枚举 */
function toEnumNumber<T extends number>(value: unknown): T | undefined {
  if (value === null || value === undefined || value === '') {
    return undefined;
  }
  const num = Number(value);
  return Number.isNaN(num) ? undefined : (num as T);
}

type SaleOrderStatusSelectProps = Omit<React.ComponentProps<typeof ASelect<Api.Order.OrderStatus>>, 'options'>;

/** 销售订单业务状态下拉 */
function SaleOrderStatusSelect({ allowClear = true, value, ...props }: SaleOrderStatusSelectProps) {
  const options = saleOrderStatusOptions.map(item => ({
    label: <SaleOrderStatusBadge orderStatus={item.value as Api.Order.OrderStatus} />,
    value: item.value as Api.Order.OrderStatus
  }));

  return (
    <ASelect
      allowClear={allowClear}
      options={options}
      value={toEnumNumber<Api.Order.OrderStatus>(value)}
      {...props}
    />
  );
}

type OrderReturnStatusSelectProps = Omit<React.ComponentProps<typeof ASelect<Api.Order.ReturnStatus>>, 'options'>;

/** 回单状态下拉 */
function OrderReturnStatusSelect({ allowClear = true, value, ...props }: OrderReturnStatusSelectProps) {
  const options = orderReturnStatusOptions.map(item => ({
    label: <OrderReturnStatusBadge returnStatus={item.value as Api.Order.ReturnStatus} />,
    value: item.value as Api.Order.ReturnStatus
  }));

  return (
    <ASelect
      allowClear={allowClear}
      options={options}
      value={toEnumNumber<Api.Order.ReturnStatus>(value)}
      {...props}
    />
  );
}

type OrderDateTypeSelectProps = Omit<React.ComponentProps<typeof ASelect<Api.Order.DateType>>, 'options'>;

/** 订单日期类型下拉：枚举包装为 i18n 文案，并兼容 URL 回填的字符串数值 */
function OrderDateTypeSelect({ allowClear = false, value, ...props }: OrderDateTypeSelectProps) {
  const { t } = useTranslation();

  const options = orderDateTypeOptions.map(item => ({
    label: t(item.label as App.I18n.I18nKey),
    value: item.value as Api.Order.DateType
  }));

  return (
    <ASelect
      allowClear={allowClear}
      options={options}
      value={toEnumNumber<Api.Order.DateType>(value)}
      {...props}
    />
  );
}

export { OrderDateTypeSelect, OrderReturnStatusSelect, SaleOrderStatusSelect };
export default SaleOrderStatusSelect;
