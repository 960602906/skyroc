import { type LoaderFunctionArgs, redirect, useLoaderData, useNavigate } from 'react-router-dom';

import { DetailPageLayout } from '@/features/crud';
import { renderStockDocumentStatus } from '@/features/crud/render-status';
import { useCloseTabAndNavigate } from '@/features/tab';
import {
  fetchAuditStockInPurchase,
  fetchDeleteStockInPurchase,
  fetchGetStockInPurchaseDetail,
  fetchReverseAuditStockInPurchase
} from '@/service/api';
import { StockDocumentStatus } from '@/service/enums';

import PurchaseStockInDetailView from './modules/PurchaseStockInDetailView';

const LIST_PATH = '/storage/in/purchase';

/** 路由切换前加载采购入库详情，入库单不存在或加载失败时返回列表。 */
export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) return redirect(LIST_PATH);

  try {
    const detail = await fetchGetStockInPurchaseDetail(id);
    return detail ?? redirect(LIST_PATH);
  } catch {
    return redirect(LIST_PATH);
  }
}

/** 采购入库基础信息、商品明细和审核轨迹详情页。 */
const PurchaseStockInDetail = () => {
  const { t } = useTranslation();
  const nav = useNavigate();
  const detail = useLoaderData() as Api.StockIn.Entity;
  const closeTabAndNavigate = useCloseTabAndNavigate();
  const [acting, setActing] = useState(false);

  if (!detail) return null;

  const isDraft = detail.businessStatus === StockDocumentStatus.DRAFT;
  const isAudited = detail.businessStatus === StockDocumentStatus.AUDITED;

  async function handleDelete() {
    setActing(true);
    try {
      await fetchDeleteStockInPurchase(detail.id);
      window.$message?.success(t('common.deleteSuccess'));
      closeTabAndNavigate(LIST_PATH);
    } finally {
      setActing(false);
    }
  }

  async function handleAudit() {
    setActing(true);
    try {
      await fetchAuditStockInPurchase(detail.id, {});
      window.$message?.success(t('common.updateSuccess'));
      closeTabAndNavigate(LIST_PATH);
    } finally {
      setActing(false);
    }
  }

  async function handleReverseAudit() {
    setActing(true);
    try {
      await fetchReverseAuditStockInPurchase(detail.id, {});
      window.$message?.success(t('common.updateSuccess'));
      closeTabAndNavigate(LIST_PATH);
    } finally {
      setActing(false);
    }
  }

  return (
    <DetailPageLayout
      backLabel={t('page.storage.in.purchase.back')}
      banner={renderStockDocumentStatus(detail.businessStatus)}
      listPath={LIST_PATH}
      title={detail.inNo}
      extra={
        <>
          {isDraft && (
            <>
              <AButton
                disabled={acting}
                type="primary"
                onClick={() => nav(`/storage/in/purchase/operate/${detail.id}`)}
              >
                {t('common.edit')}
              </AButton>
              <APopconfirm
                title={t('common.confirmDelete')}
                onConfirm={handleDelete}
              >
                <AButton
                  danger
                  disabled={acting}
                >
                  {t('common.delete')}
                </AButton>
              </APopconfirm>
              <APopconfirm
                title={t('page.storage.in.purchase.audit')}
                onConfirm={handleAudit}
              >
                <AButton
                  disabled={acting}
                  type="primary"
                >
                  {t('page.storage.in.audit')}
                </AButton>
              </APopconfirm>
            </>
          )}
          {isAudited && (
            <APopconfirm
              title={t('page.storage.in.purchase.reverseAudit')}
              onConfirm={handleReverseAudit}
            >
              <AButton
                danger
                disabled={acting}
              >
                {t('page.storage.in.purchase.reverseAuditBtn')}
              </AButton>
            </APopconfirm>
          )}
        </>
      }
    >
      <PurchaseStockInDetailView detail={detail} />
    </DetailPageLayout>
  );
};

export default PurchaseStockInDetail;
