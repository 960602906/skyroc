import BooleanYesNoBadge from './BooleanYesNoBadge';
import EnableStatusBadge from './EnableStatusBadge';
import MenuTypeBadge from './MenuTypeBadge';
import PurchasePatternBadge from './PurchasePatternBadge';
import {
  OrderOutStorageStatusBadge,
  OrderPrintStatusBadge,
  OrderReturnStatusBadge,
  SaleOrderStatusBadge
} from './SaleOrderStatusBadge';
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
    <BooleanYesNoBadge
      falseText={falseText}
      trueText={trueText}
      value={value}
    />
  );
}

/** 表格列等场景的布尔是否渲染（默认「是/否」） */
export function renderBooleanYesNo(value: boolean | null | undefined) {
  return <BooleanYesNoBadge value={value} />;
}

/** 表格列等场景的销售订单业务状态渲染 */
export function renderSaleOrderStatus(orderStatus: Api.Order.OrderStatus | null | undefined) {
  return <SaleOrderStatusBadge orderStatus={orderStatus} />;
}

/** 表格列等场景的回单状态渲染 */
export function renderOrderReturnStatus(returnStatus: Api.Order.ReturnStatus | null | undefined) {
  return <OrderReturnStatusBadge returnStatus={returnStatus} />;
}

/** 表格列等场景的打印状态渲染 */
export function renderOrderPrintStatus(printStatus: Api.Order.PrintStatus | null | undefined) {
  return <OrderPrintStatusBadge printStatus={printStatus} />;
}

/** 表格列等场景的出库生成状态渲染 */
export function renderOrderOutStorageStatus(outStorageStatus: Api.Order.OutStorageStatus | null | undefined) {
  return <OrderOutStorageStatusBadge outStorageStatus={outStorageStatus} />;
}
