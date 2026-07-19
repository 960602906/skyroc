import { afterSaleStatusOptions } from '@/constants/business';

import AfterSaleStatusBadge from './AfterSaleStatusBadge';

type AfterSaleStatusSelectProps = Omit<React.ComponentProps<typeof ASelect<Api.AfterSale.AfterStatus>>, 'options'>;

/** 售后单业务状态下拉 */
function AfterSaleStatusSelect({ allowClear = true, value, ...props }: AfterSaleStatusSelectProps) {
  const rawValue = value === null || value === undefined ? undefined : Number(value);
  const normalizedValue = Number.isNaN(rawValue) ? undefined : (rawValue as Api.AfterSale.AfterStatus | undefined);
  const options = afterSaleStatusOptions.map(item => ({
    label: <AfterSaleStatusBadge afterStatus={item.value as Api.AfterSale.AfterStatus} />,
    value: item.value as Api.AfterSale.AfterStatus
  }));

  return (
    <ASelect
      allowClear={allowClear}
      options={options}
      value={normalizedValue}
      {...props}
    />
  );
}

export default AfterSaleStatusSelect;
