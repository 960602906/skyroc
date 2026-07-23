import { stockDocumentStatusRecord } from '@/constants/business';
import { STOCK_DOCUMENT_STATUS_MAP } from '@/constants/common';

interface StockDocumentStatusBadgeProps {
  /** 库存单据业务状态：已删除、草稿、待审核、已审核或已反审核；为空时不渲染。 */
  businessStatus: Api.StockIn.StockDocumentStatus | null | undefined;
}

/** 库存单据业务状态徽标 */
function StockDocumentStatusBadge({ businessStatus }: StockDocumentStatusBadgeProps) {
  const { t } = useTranslation();

  if (businessStatus === null || businessStatus === undefined) return null;

  return (
    <ABadge
      status={STOCK_DOCUMENT_STATUS_MAP[businessStatus]}
      text={t(stockDocumentStatusRecord[businessStatus])}
    />
  );
}

export default StockDocumentStatusBadge;
