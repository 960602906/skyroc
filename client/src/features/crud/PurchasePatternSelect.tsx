import { purchasePatternOptions } from '@/constants/business';

import PurchasePatternBadge from './PurchasePatternBadge';

type PurchasePatternSelectProps = Omit<
  React.ComponentProps<typeof ASelect<Api.PurchaseRule.PurchasePattern>>,
  'options'
>;

/** 全局采购模式下拉：选项与选中值均使用 PurchasePatternBadge */
function PurchasePatternSelect({ allowClear = true, ...props }: PurchasePatternSelectProps) {
  const options = purchasePatternOptions.map(item => ({
    label: <PurchasePatternBadge purchasePattern={item.value as Api.PurchaseRule.PurchasePattern} />,
    value: item.value as Api.PurchaseRule.PurchasePattern
  }));

  return (
    <ASelect
      allowClear={allowClear}
      options={options}
      {...props}
    />
  );
}

export default PurchasePatternSelect;
