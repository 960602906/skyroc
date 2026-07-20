import { pickupTaskStatusOptions } from '@/constants/business';

import PickupTaskStatusBadge from './PickupTaskStatusBadge';

type PickupTaskStatusSelectProps = Omit<React.ComponentProps<typeof ASelect<Api.AfterSale.PickupStatus>>, 'options'>;

/** 售后取货任务履约状态下拉。 */
function PickupTaskStatusSelect({ allowClear = true, value, ...props }: PickupTaskStatusSelectProps) {
  const rawValue = value === null || value === undefined ? undefined : Number(value);
  const normalizedValue = Number.isNaN(rawValue) ? undefined : (rawValue as Api.AfterSale.PickupStatus | undefined);
  const options = pickupTaskStatusOptions.map(item => ({
    label: <PickupTaskStatusBadge pickupStatus={item.value as Api.AfterSale.PickupStatus} />,
    value: item.value as Api.AfterSale.PickupStatus
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

export default PickupTaskStatusSelect;
