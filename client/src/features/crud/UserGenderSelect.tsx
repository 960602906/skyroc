import { userGenderOptions } from '@/constants/business';

import UserGenderBadge from './UserGenderBadge';

type UserGenderSelectProps = Omit<React.ComponentProps<typeof ASelect<Api.SystemManage.UserGender>>, 'options'>;

/** 全局用户性别下拉：选项与选中值均使用 UserGenderBadge */
function UserGenderSelect({ allowClear = true, ...props }: UserGenderSelectProps) {
  const options = userGenderOptions.map(item => ({
    label: <UserGenderBadge gender={item.value as Api.SystemManage.UserGender} />,
    value: item.value as Api.SystemManage.UserGender
  }));

  return (
    <ASelect
      allowClear={allowClear}
      options={options}
      {...props}
    />
  );
}

export default UserGenderSelect;
