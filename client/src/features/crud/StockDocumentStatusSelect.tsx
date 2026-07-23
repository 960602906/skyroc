import { stockDocumentStatusOptions } from '@/constants/business';

import StockDocumentStatusBadge from './StockDocumentStatusBadge';

type StockDocumentStatusSelectProps = Omit<
  React.ComponentProps<typeof ASelect<Api.StockIn.StockDocumentStatus>>,
  'options'
>;

/** 库存单据业务状态筛选下拉：草稿、待审核、已审核、已反审核。 */
function StockDocumentStatusSelect({ allowClear = true, ...props }: StockDocumentStatusSelectProps) {
  const options = stockDocumentStatusOptions.map(item => ({
    label: <StockDocumentStatusBadge businessStatus={item.value as Api.StockIn.StockDocumentStatus} />,
    value: item.value as Api.StockIn.StockDocumentStatus
  }));

  return (
    <ASelect
      allowClear={allowClear}
      options={options}
      {...props}
    />
  );
}

export default StockDocumentStatusSelect;
