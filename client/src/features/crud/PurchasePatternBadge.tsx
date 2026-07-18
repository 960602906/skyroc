import { purchasePatternRecord } from '@/constants/business';
import { PURCHASE_PATTERN_MAP } from '@/constants/common';

interface PurchasePatternBadgeProps {
  /** 采购模式：1 供应商直供，2 市场自采；为空时不渲染 */
  purchasePattern: Api.PurchaseRule.PurchasePattern | null | undefined;
}

/** 全局采购模式徽标 */
function PurchasePatternBadge({ purchasePattern }: PurchasePatternBadgeProps) {
  const { t } = useTranslation();

  if (purchasePattern === null || purchasePattern === undefined) {
    return null;
  }

  return (
    <ABadge
      status={PURCHASE_PATTERN_MAP[purchasePattern]}
      text={t(purchasePatternRecord[purchasePattern])}
    />
  );
}

export default PurchasePatternBadge;
