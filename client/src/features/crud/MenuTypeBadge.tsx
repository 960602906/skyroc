import { menuTypeRecord } from '@/constants/business';
import { MENU_TYPE_MAP } from '@/constants/common';

interface MenuTypeBadgeProps {
  /** 菜单类型：1 目录，2 菜单；为空时不渲染 */
  menuType: Api.SystemManage.MenuType | null | undefined;
}

/** 全局菜单类型徽标 */
function MenuTypeBadge({ menuType }: MenuTypeBadgeProps) {
  const { t } = useTranslation();

  if (menuType === null || menuType === undefined) {
    return null;
  }

  return (
    <ABadge
      status={MENU_TYPE_MAP[menuType]}
      text={t(menuTypeRecord[menuType])}
    />
  );
}

export default MenuTypeBadge;
