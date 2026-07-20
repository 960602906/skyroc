import { afterSaleReasonTypeRecord } from '@/constants/business';
import { AFTER_SALE_REASON_TYPE_MAP } from '@/constants/common';

interface AfterSaleReasonTypeBadgeProps {
  /** 售后原因分类；为空时不渲染。 */
  reasonType: Api.AfterSale.ReasonType | null | undefined;
}

/** 售后原因分类徽标。 */
function AfterSaleReasonTypeBadge({ reasonType }: AfterSaleReasonTypeBadgeProps) {
  const { t } = useTranslation();

  if (reasonType === null || reasonType === undefined) {
    return null;
  }

  return (
    <ABadge
      status={AFTER_SALE_REASON_TYPE_MAP[reasonType]}
      text={t(afterSaleReasonTypeRecord[reasonType])}
    />
  );
}

export default AfterSaleReasonTypeBadge;
