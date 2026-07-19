import { transformRecordToOption } from '@/utils/common';

export const enableStatusRecord: Record<Api.Common.EnableStatus, App.I18n.I18nKey> = {
  1: 'page.manage.common.status.enable',
  2: 'page.manage.common.status.disable'
};

export const enableStatusOptions = transformRecordToOption(enableStatusRecord, true);

export const userGenderRecord: Record<Api.SystemManage.UserGender, App.I18n.I18nKey> = {
  1: 'page.manage.user.gender.male',
  2: 'page.manage.user.gender.female'
};

export const userGenderOptions = transformRecordToOption(userGenderRecord, true);

export const menuTypeRecord: Record<Api.SystemManage.MenuType, App.I18n.I18nKey> = {
  1: 'page.manage.menu.type.directory',
  2: 'page.manage.menu.type.menu'
};

export const menuTypeOptions = transformRecordToOption(menuTypeRecord, true);

export const menuIconTypeRecord: Record<Api.SystemManage.IconType, App.I18n.I18nKey> = {
  1: 'page.manage.menu.iconType.iconify',
  2: 'page.manage.menu.iconType.local'
};

export const menuIconTypeOptions = transformRecordToOption(menuIconTypeRecord, true);

export const purchasePatternRecord: Record<Api.PurchaseRule.PurchasePattern, App.I18n.I18nKey> = {
  1: 'page.purchase.rule.purchasePatternDirect',
  2: 'page.purchase.rule.purchasePatternMarket'
};

export const purchasePatternOptions = transformRecordToOption(purchasePatternRecord, true);

export const afterSaleStatusRecord: Record<Api.AfterSale.AfterStatus, App.I18n.I18nKey> = {
  1: 'page.afterSale.list.statusDraft',
  2: 'page.afterSale.list.statusPendingAudit',
  3: 'page.afterSale.list.statusReturnPending',
  4: 'page.afterSale.list.statusRefundPending',
  5: 'page.afterSale.list.statusCompleted'
};

export const afterSaleStatusOptions = transformRecordToOption(afterSaleStatusRecord, true);

export const afterSaleTypeRecord: Record<Api.AfterSale.AfterSaleType, App.I18n.I18nKey> = {
  1: 'page.afterSale.list.typeRefundOnly',
  2: 'page.afterSale.list.typeReturnAndRefund'
};

export const afterSaleTypeOptions = transformRecordToOption(afterSaleTypeRecord, true);

export const afterSaleHandleTypeRecord: Record<Api.AfterSale.HandleType, App.I18n.I18nKey> = {
  1: 'page.afterSale.list.handleGoodsDiscount',
  2: 'page.afterSale.list.handleReplenishment',
  3: 'page.afterSale.list.handleExchange',
  4: 'page.afterSale.list.handleBillAdjustment',
  5: 'page.afterSale.list.handleCustomerCommunication',
  6: 'page.afterSale.list.handleOther'
};

export const afterSaleHandleTypeOptions = transformRecordToOption(afterSaleHandleTypeRecord, true);

export const saleOrderStatusRecord: Record<Api.Order.OrderStatus, App.I18n.I18nKey> = {
  [-1]: 'page.order.list.orderStatusPendingAudit',
  1: 'page.order.list.orderStatusSortingPending',
  2: 'page.order.list.orderStatusSorting',
  3: 'page.order.list.orderStatusSortingCompleted',
  4: 'page.order.list.orderStatusDelivering',
  5: 'page.order.list.orderStatusSigned',
  6: 'page.order.list.orderStatusRejected'
};

export const saleOrderStatusOptions = transformRecordToOption(saleOrderStatusRecord, true);

export const orderReturnStatusRecord: Record<Api.Order.ReturnStatus, App.I18n.I18nKey> = {
  0: 'page.order.list.returnStatusNotReturned',
  1: 'page.order.list.returnStatusReturned'
};

export const orderReturnStatusOptions = transformRecordToOption(orderReturnStatusRecord, true);

export const orderPrintStatusRecord: Record<Api.Order.PrintStatus, App.I18n.I18nKey> = {
  0: 'page.order.list.printStatusNotPrinted',
  1: 'page.order.list.printStatusPrinted'
};

export const orderOutStorageStatusRecord: Record<Api.Order.OutStorageStatus, App.I18n.I18nKey> = {
  0: 'page.order.list.outStorageStatusNotGenerated',
  1: 'page.order.list.outStorageStatusPartiallyGenerated',
  2: 'page.order.list.outStorageStatusGenerated'
};

export const orderDateTypeRecord: Record<Api.Order.DateType, App.I18n.I18nKey> = {
  0: 'page.order.list.dateTypeOrderDate',
  1: 'page.order.list.dateTypeReceiveDate',
  2: 'page.order.list.dateTypeOutDate'
};

export const orderDateTypeOptions = transformRecordToOption(orderDateTypeRecord, true);

export const orderAuditActionRecord: Record<Api.Order.AuditAction, App.I18n.I18nKey> = {
  0: 'page.order.detail.auditActionSubmit',
  1: 'page.order.detail.auditActionApprove',
  2: 'page.order.detail.auditActionReject',
  3: 'page.order.detail.auditActionResubmit'
};
