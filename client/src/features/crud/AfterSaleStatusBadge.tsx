import { afterSaleStatusRecord } from '@/constants/business';
import { AFTER_SALE_STATUS_MAP } from '@/constants/common';

interface AfterSaleStatusBadgeProps {
  /** 售后业务状态；为空时不渲染 */
  afterStatus: Api.AfterSale.AfterStatus | null | undefined;
}

/** 售后单业务状态徽标 */
function AfterSaleStatusBadge({ afterStatus }: AfterSaleStatusBadgeProps) {
  const { t } = useTranslation();

  if (afterStatus === null || afterStatus === undefined) {
    return null;
  }

  return (
    <ABadge
      status={AFTER_SALE_STATUS_MAP[afterStatus]}
      text={t(afterSaleStatusRecord[afterStatus])}
    />
  );
}

export default AfterSaleStatusBadge;
