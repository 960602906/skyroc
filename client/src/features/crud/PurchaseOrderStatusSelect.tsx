import { purchaseOrderStatusOptions } from '@/constants/business';

import PurchaseOrderStatusBadge from './PurchaseOrderStatusBadge';

type PurchaseOrderStatusSelectProps = Omit<
  React.ComponentProps<typeof ASelect<Api.PurchaseOrder.BusinessStatus>>,
  'options'
>;

/** 采购单执行状态筛选下拉：草稿、已完成、已取消。 */
function PurchaseOrderStatusSelect({ allowClear = true, ...props }: PurchaseOrderStatusSelectProps) {
  const options = purchaseOrderStatusOptions.map(item => ({
    label: <PurchaseOrderStatusBadge businessStatus={item.value as Api.PurchaseOrder.BusinessStatus} />,
    value: item.value as Api.PurchaseOrder.BusinessStatus
  }));

  return (
    <ASelect
      allowClear={allowClear}
      options={options}
      {...props}
    />
  );
}

export default PurchaseOrderStatusSelect;
