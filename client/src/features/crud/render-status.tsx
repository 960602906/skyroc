import EnableStatusBadge from './EnableStatusBadge';
import MenuTypeBadge from './MenuTypeBadge';
import PurchasePatternBadge from './PurchasePatternBadge';
import UserGenderBadge from './UserGenderBadge';
import YesOrNoBadge from './YesOrNoBadge';

/** 表格列等场景的启用/禁用状态渲染 */
export function renderEnableStatus(status: Api.Common.EnableStatus | null | undefined) {
  return <EnableStatusBadge status={status} />;
}

/** 表格列等场景的用户性别渲染 */
export function renderUserGender(gender: Api.SystemManage.UserGender | null | undefined) {
  return <UserGenderBadge gender={gender} />;
}

/** 表格列等场景的菜单类型渲染 */
export function renderMenuType(menuType: Api.SystemManage.MenuType | null | undefined) {
  return <MenuTypeBadge menuType={menuType} />;
}

/** 表格列等场景的采购模式渲染 */
export function renderPurchasePattern(purchasePattern: Api.PurchaseRule.PurchasePattern | null | undefined) {
  return <PurchasePatternBadge purchasePattern={purchasePattern} />;
}

/** 表格列等场景的是否渲染 */
export function renderYesOrNo(value: CommonType.YesOrNo | null | undefined) {
  return <YesOrNoBadge value={value} />;
}

/** 布尔值徽标（自定义文案，非标准 YesOrNo 枚举时使用） */
export function renderBooleanTag(value: boolean, trueText: string, falseText: string) {
  return (
    <ABadge
      status={value ? 'success' : 'default'}
      text={value ? trueText : falseText}
    />
  );
}
