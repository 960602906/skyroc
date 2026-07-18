import { orderDateTypeOptions, orderReturnStatusOptions, saleOrderStatusOptions } from '@/constants/business';

import { OrderReturnStatusBadge, SaleOrderStatusBadge } from './SaleOrderStatusBadge';

type SaleOrderStatusSelectProps = Omit<React.ComponentProps<typeof ASelect<Api.Order.OrderStatus>>, 'options'>;

/** 销售订单业务状态下拉 */
function SaleOrderStatusSelect({ allowClear = true, ...props }: SaleOrderStatusSelectProps) {
  const options = saleOrderStatusOptions.map(item => ({
    label: <SaleOrderStatusBadge orderStatus={item.value as Api.Order.OrderStatus} />,
    value: item.value as Api.Order.OrderStatus
  }));

  return (
    <ASelect
      allowClear={allowClear}
      options={options}
      {...props}
    />
  );
}

type OrderReturnStatusSelectProps = Omit<React.ComponentProps<typeof ASelect<Api.Order.ReturnStatus>>, 'options'>;

/** 回单状态下拉 */
function OrderReturnStatusSelect({ allowClear = true, ...props }: OrderReturnStatusSelectProps) {
  const options = orderReturnStatusOptions.map(item => ({
    label: <OrderReturnStatusBadge returnStatus={item.value as Api.Order.ReturnStatus} />,
    value: item.value as Api.Order.ReturnStatus
  }));

  return (
    <ASelect
      allowClear={allowClear}
      options={options}
      {...props}
    />
  );
}

type OrderDateTypeSelectProps = Omit<React.ComponentProps<typeof ASelect<Api.Order.DateType>>, 'options'>;

/** 订单日期类型下拉 */
function OrderDateTypeSelect({ allowClear = false, ...props }: OrderDateTypeSelectProps) {
  const { t } = useTranslation();

  const options = orderDateTypeOptions.map(item => ({
    label: t(item.label as App.I18n.I18nKey),
    value: item.value as Api.Order.DateType
  }));

  return (
    <ASelect
      allowClear={allowClear}
      options={options}
      {...props}
    />
  );
}

export { OrderDateTypeSelect, OrderReturnStatusSelect, SaleOrderStatusSelect };
export default SaleOrderStatusSelect;
