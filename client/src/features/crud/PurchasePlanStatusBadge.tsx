import { purchasePlanStatusRecord } from '@/constants/business';
import { PURCHASE_PLAN_STATUS_MAP } from '@/constants/common';

interface PurchasePlanStatusBadgeProps {
  /** 采购计划采购单生成进度；为空时不渲染。 */
  purchaseStatus: Api.PurchasePlan.PurchaseStatus | null | undefined;
}

/** 采购计划采购单生成进度徽标。 */
function PurchasePlanStatusBadge({ purchaseStatus }: PurchasePlanStatusBadgeProps) {
  const { t } = useTranslation();

  if (purchaseStatus === null || purchaseStatus === undefined) return null;

  return (
    <ABadge
      status={PURCHASE_PLAN_STATUS_MAP[purchaseStatus]}
      text={t(purchasePlanStatusRecord[purchaseStatus])}
    />
  );
}

export default PurchasePlanStatusBadge;
