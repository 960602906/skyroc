import {
  orderOutStorageStatusRecord,
  orderPrintStatusRecord,
  orderReturnStatusRecord,
  saleOrderStatusRecord
} from '@/constants/business';
import {
  ORDER_OUT_STORAGE_STATUS_MAP,
  ORDER_PRINT_STATUS_MAP,
  ORDER_RETURN_STATUS_MAP,
  SALE_ORDER_STATUS_MAP
} from '@/constants/common';

interface SaleOrderStatusBadgeProps {
  /** 订单业务状态；为空时不渲染 */
  orderStatus: Api.Order.OrderStatus | null | undefined;
}

/** 销售订单业务状态徽标 */
function SaleOrderStatusBadge({ orderStatus }: SaleOrderStatusBadgeProps) {
  const { t } = useTranslation();

  if (orderStatus === null || orderStatus === undefined) {
    return null;
  }

  return (
    <ABadge
      status={SALE_ORDER_STATUS_MAP[orderStatus]}
      text={t(saleOrderStatusRecord[orderStatus])}
    />
  );
}

interface OrderReturnStatusBadgeProps {
  returnStatus: Api.Order.ReturnStatus | null | undefined;
}

/** 回单状态徽标 */
function OrderReturnStatusBadge({ returnStatus }: OrderReturnStatusBadgeProps) {
  const { t } = useTranslation();

  if (returnStatus === null || returnStatus === undefined) {
    return null;
  }

  return (
    <ABadge
      status={ORDER_RETURN_STATUS_MAP[returnStatus]}
      text={t(orderReturnStatusRecord[returnStatus])}
    />
  );
}

interface OrderPrintStatusBadgeProps {
  printStatus: Api.Order.PrintStatus | null | undefined;
}

/** 打印状态徽标 */
function OrderPrintStatusBadge({ printStatus }: OrderPrintStatusBadgeProps) {
  const { t } = useTranslation();

  if (printStatus === null || printStatus === undefined) {
    return null;
  }

  return (
    <ABadge
      status={ORDER_PRINT_STATUS_MAP[printStatus]}
      text={t(orderPrintStatusRecord[printStatus])}
    />
  );
}

interface OrderOutStorageStatusBadgeProps {
  outStorageStatus: Api.Order.OutStorageStatus | null | undefined;
}

/** 出库生成状态徽标 */
function OrderOutStorageStatusBadge({ outStorageStatus }: OrderOutStorageStatusBadgeProps) {
  const { t } = useTranslation();

  if (outStorageStatus === null || outStorageStatus === undefined) {
    return null;
  }

  return (
    <ABadge
      status={ORDER_OUT_STORAGE_STATUS_MAP[outStorageStatus]}
      text={t(orderOutStorageStatusRecord[outStorageStatus])}
    />
  );
}

export { OrderOutStorageStatusBadge, OrderPrintStatusBadge, OrderReturnStatusBadge, SaleOrderStatusBadge };
export default SaleOrderStatusBadge;
