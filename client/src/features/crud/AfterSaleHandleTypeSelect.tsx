import { afterSaleHandleTypeOptions } from '@/constants/business';

import AfterSaleHandleTypeBadge from './AfterSaleHandleTypeBadge';

type AfterSaleHandleTypeSelectProps = Omit<React.ComponentProps<typeof ASelect<Api.AfterSale.HandleType>>, 'options'>;

/** 售后处理方式下拉，选项和选中值均使用徽标展示。 */
function AfterSaleHandleTypeSelect({ allowClear = true, ...props }: AfterSaleHandleTypeSelectProps) {
  const options = afterSaleHandleTypeOptions.map(item => ({
    label: <AfterSaleHandleTypeBadge handleType={item.value as Api.AfterSale.HandleType} />,
    value: item.value as Api.AfterSale.HandleType
  }));

  return (
    <ASelect
      allowClear={allowClear}
      options={options}
      {...props}
    />
  );
}

export default AfterSaleHandleTypeSelect;
