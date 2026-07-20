import { afterSaleTypeOptions } from '@/constants/business';

import AfterSaleTypeBadge from './AfterSaleTypeBadge';

type AfterSaleTypeSelectProps = Omit<React.ComponentProps<typeof ASelect<Api.AfterSale.AfterSaleType>>, 'options'>;

/** 售后申请类型下拉，选项和选中值均使用徽标展示。 */
function AfterSaleTypeSelect({ allowClear = true, ...props }: AfterSaleTypeSelectProps) {
  const options = afterSaleTypeOptions.map(item => ({
    label: <AfterSaleTypeBadge afterSaleType={item.value as Api.AfterSale.AfterSaleType} />,
    value: item.value as Api.AfterSale.AfterSaleType
  }));

  return (
    <ASelect
      allowClear={allowClear}
      options={options}
      {...props}
    />
  );
}

export default AfterSaleTypeSelect;
