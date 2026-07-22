import { purchaseOrderStatusRecord } from '@/constants/business';
import { PURCHASE_ORDER_STATUS_MAP } from '@/constants/common';

interface PurchaseOrderStatusBadgeProps {
  /** 采购单执行状态：草稿、已完成或已取消；为空时不渲染。 */
  businessStatus: Api.PurchaseOrder.BusinessStatus | null | undefined;
}

/** 采购单执行状态徽标。 */
function PurchaseOrderStatusBadge({ businessStatus }: PurchaseOrderStatusBadgeProps) {
  const { t } = useTranslation();

  if (businessStatus === null || businessStatus === undefined) return null;

  return (
    <ABadge
      status={PURCHASE_ORDER_STATUS_MAP[businessStatus]}
      text={t(purchaseOrderStatusRecord[businessStatus])}
    />
  );
}

export default PurchaseOrderStatusBadge;
