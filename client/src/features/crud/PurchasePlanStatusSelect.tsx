import { purchasePlanStatusOptions } from '@/constants/business';

import PurchasePlanStatusBadge from './PurchasePlanStatusBadge';

type PurchasePlanStatusSelectProps = Omit<
  React.ComponentProps<typeof ASelect<Api.PurchasePlan.PurchaseStatus>>,
  'options'
>;

/** 采购计划生成进度筛选下拉。 */
function PurchasePlanStatusSelect({ allowClear = true, ...props }: PurchasePlanStatusSelectProps) {
  const options = purchasePlanStatusOptions.map(item => ({
    label: <PurchasePlanStatusBadge purchaseStatus={item.value as Api.PurchasePlan.PurchaseStatus} />,
    value: item.value as Api.PurchasePlan.PurchaseStatus
  }));

  return (
    <ASelect
      allowClear={allowClear}
      options={options}
      {...props}
    />
  );
}

export default PurchasePlanStatusSelect;
