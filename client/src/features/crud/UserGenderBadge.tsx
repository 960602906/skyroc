import { userGenderRecord } from '@/constants/business';
import { USER_GENDER_MAP } from '@/constants/common';

interface UserGenderBadgeProps {
  /** 用户性别：1 男，2 女；为空时不渲染 */
  gender: Api.SystemManage.UserGender | null | undefined;
}

/** 全局用户性别徽标 */
function UserGenderBadge({ gender }: UserGenderBadgeProps) {
  const { t } = useTranslation();

  if (gender === null || gender === undefined) {
    return null;
  }

  return (
    <ABadge
      status={USER_GENDER_MAP[gender]}
      text={t(userGenderRecord[gender])}
    />
  );
}

export default UserGenderBadge;
