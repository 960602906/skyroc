import { afterSaleReasonTypeOptions } from '@/constants/business';

import AfterSaleReasonTypeBadge from './AfterSaleReasonTypeBadge';

type AfterSaleReasonTypeSelectProps = Omit<React.ComponentProps<typeof ASelect<Api.AfterSale.ReasonType>>, 'options'>;

/** 售后原因分类下拉，选项和选中值均使用徽标展示。 */
function AfterSaleReasonTypeSelect({ allowClear = true, ...props }: AfterSaleReasonTypeSelectProps) {
  const options = afterSaleReasonTypeOptions.map(item => ({
    label: <AfterSaleReasonTypeBadge reasonType={item.value as Api.AfterSale.ReasonType} />,
    value: item.value as Api.AfterSale.ReasonType
  }));

  return (
    <ASelect
      allowClear={allowClear}
      options={options}
      {...props}
    />
  );
}

export default AfterSaleReasonTypeSelect;
