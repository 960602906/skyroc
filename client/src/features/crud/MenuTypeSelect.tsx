import { menuTypeOptions } from '@/constants/business';

import MenuTypeBadge from './MenuTypeBadge';

type MenuTypeSelectProps = Omit<React.ComponentProps<typeof ASelect<Api.SystemManage.MenuType>>, 'options'>;

/** 全局菜单类型下拉：选项与选中值均使用 MenuTypeBadge */
function MenuTypeSelect({ allowClear = true, ...props }: MenuTypeSelectProps) {
  const options = menuTypeOptions.map(item => ({
    label: <MenuTypeBadge menuType={item.value as Api.SystemManage.MenuType} />,
    value: item.value as Api.SystemManage.MenuType
  }));

  return (
    <ASelect
      allowClear={allowClear}
      options={options}
      {...props}
    />
  );
}

export default MenuTypeSelect;
