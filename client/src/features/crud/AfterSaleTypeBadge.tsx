import { afterSaleTypeRecord } from '@/constants/business';
import { AFTER_SALE_TYPE_MAP } from '@/constants/common';

interface AfterSaleTypeBadgeProps {
  /** 售后申请类型；为空时不渲染。 */
  afterSaleType: Api.AfterSale.AfterSaleType | null | undefined;
}

/** 售后申请类型徽标。 */
function AfterSaleTypeBadge({ afterSaleType }: AfterSaleTypeBadgeProps) {
  const { t } = useTranslation();

  if (afterSaleType === null || afterSaleType === undefined) {
    return null;
  }

  return (
    <ABadge
      status={AFTER_SALE_TYPE_MAP[afterSaleType]}
      text={t(afterSaleTypeRecord[afterSaleType])}
    />
  );
}

export default AfterSaleTypeBadge;
