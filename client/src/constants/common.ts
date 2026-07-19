import { transformRecordToOption } from '@/utils/common';

/** Ant Design Badge.status 预设色 */
export type BadgeStatus = 'default' | 'error' | 'processing' | 'success' | 'warning';

export const yesOrNoRecord: Record<CommonType.YesOrNo, App.I18n.I18nKey> = {
  N: 'common.yesOrNo.no',
  Y: 'common.yesOrNo.yes'
};

export const yesOrNoOptions = transformRecordToOption(yesOrNoRecord);

export const DARK_CLASS = 'dark';

/** 启用状态：启用 success / 禁用 warning */
export const ATG_MAP: Record<Api.Common.EnableStatus, BadgeStatus> = {
  1: 'success',
  2: 'warning'
};

/** 用户性别：男 processing / 女 error */
export const USER_GENDER_MAP: Record<Api.SystemManage.UserGender, BadgeStatus> = {
  1: 'processing',
  2: 'error'
};

/** 菜单类型：目录 default / 菜单 processing */
export const MENU_TYPE_MAP: Record<Api.SystemManage.MenuType, BadgeStatus> = {
  1: 'default',
  2: 'processing'
};

/** 是否：是 success / 否 default */
export const YES_OR_NO_MAP: Record<CommonType.YesOrNo, BadgeStatus> = {
  N: 'default',
  Y: 'success'
};

/** 采购模式：供应商直供 processing / 市场自采 default */
export const PURCHASE_PATTERN_MAP: Record<Api.PurchaseRule.PurchasePattern, BadgeStatus> = {
  1: 'processing',
  2: 'default'
};

/** 售后单状态徽标色 */
export const AFTER_SALE_STATUS_MAP: Record<Api.AfterSale.AfterStatus, BadgeStatus> = {
  1: 'default',
  2: 'processing',
  3: 'warning',
  4: 'warning',
  5: 'success'
};

/** 销售订单状态徽标色 */
export const SALE_ORDER_STATUS_MAP: Record<Api.Order.OrderStatus, BadgeStatus> = {
  [-1]: 'warning',
  1: 'processing',
  2: 'processing',
  3: 'success',
  4: 'processing',
  5: 'success',
  6: 'error'
};

/** 回单状态徽标色 */
export const ORDER_RETURN_STATUS_MAP: Record<Api.Order.ReturnStatus, BadgeStatus> = {
  0: 'default',
  1: 'success'
};

/** 打印状态徽标色 */
export const ORDER_PRINT_STATUS_MAP: Record<Api.Order.PrintStatus, BadgeStatus> = {
  0: 'default',
  1: 'success'
};

/** 出库生成状态徽标色 */
export const ORDER_OUT_STORAGE_STATUS_MAP: Record<Api.Order.OutStorageStatus, BadgeStatus> = {
  0: 'default',
  1: 'warning',
  2: 'success'
};

export const LAYOUT_MODE_VERTICAL: UnionKey.ThemeLayoutMode = 'vertical';
export const LAYOUT_MODE_HORIZONTAL: UnionKey.ThemeLayoutMode = 'horizontal';
export const LAYOUT_MODE_VERTICAL_MIX: UnionKey.ThemeLayoutMode = 'vertical-mix';
export const LAYOUT_MODE_HORIZONTAL_MIX: UnionKey.ThemeLayoutMode = 'horizontal-mix';
