import { afterSaleHandleTypeRecord } from '@/constants/business';
import { AFTER_SALE_HANDLE_TYPE_MAP } from '@/constants/common';

interface AfterSaleHandleTypeBadgeProps {
  /** 售后处理方式；为空时不渲染。 */
  handleType: Api.AfterSale.HandleType | null | undefined;
}

/** 售后处理方式徽标。 */
function AfterSaleHandleTypeBadge({ handleType }: AfterSaleHandleTypeBadgeProps) {
  const { t } = useTranslation();

  if (handleType === null || handleType === undefined) {
    return null;
  }

  return (
    <ABadge
      status={AFTER_SALE_HANDLE_TYPE_MAP[handleType]}
      text={t(afterSaleHandleTypeRecord[handleType])}
    />
  );
}

export default AfterSaleHandleTypeBadge;
