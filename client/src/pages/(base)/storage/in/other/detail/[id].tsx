import { type LoaderFunctionArgs, redirect, useLoaderData, useNavigate } from 'react-router-dom';

import { DetailPageLayout } from '@/features/crud';
import { renderStockDocumentStatus } from '@/features/crud/render-status';
import { useCloseTabAndNavigate } from '@/features/tab';
import {
  fetchAuditStockInOther,
  fetchDeleteStockInOther,
  fetchGetStockInOtherDetail,
  fetchReverseAuditStockInOther
} from '@/service/api';
import { StockDocumentStatus } from '@/service/enums';

import OtherStockInDetailView from './modules/OtherStockInDetailView';

const LIST_PATH = '/storage/in/other';

/** 路由切换前加载其他入库详情，入库单不存在或加载失败时返回列表。 */
export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) return redirect(LIST_PATH);

  try {
    const detail = await fetchGetStockInOtherDetail(id);
    return detail ?? redirect(LIST_PATH);
  } catch {
    return redirect(LIST_PATH);
  }
}

/** 其他入库基础信息、商品明细和审核轨迹详情页。 */
const OtherStockInDetail = () => {
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
      await fetchDeleteStockInOther(detail.id);
      window.$message?.success(t('common.deleteSuccess'));
      closeTabAndNavigate(LIST_PATH);
    } finally {
      setActing(false);
    }
  }

  async function handleAudit() {
    setActing(true);
    try {
      await fetchAuditStockInOther(detail.id, {});
      window.$message?.success(t('common.updateSuccess'));
      closeTabAndNavigate(LIST_PATH);
    } finally {
      setActing(false);
    }
  }

  async function handleReverseAudit() {
    setActing(true);
    try {
      await fetchReverseAuditStockInOther(detail.id, {});
      window.$message?.success(t('common.updateSuccess'));
      closeTabAndNavigate(LIST_PATH);
    } finally {
      setActing(false);
    }
  }

  return (
    <DetailPageLayout
      backLabel={t('page.storage.in.other.back')}
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
                onClick={() => nav(`/storage/in/other/operate/${detail.id}`)}
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
                title={t('page.storage.in.other.audit')}
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
              title={t('page.storage.in.other.reverseAudit')}
              onConfirm={handleReverseAudit}
            >
              <AButton
                danger
                disabled={acting}
              >
                {t('page.storage.in.other.reverseAuditBtn')}
              </AButton>
            </APopconfirm>
          )}
        </>
      }
    >
      <OtherStockInDetailView detail={detail} />
    </DetailPageLayout>
  );
};

export default OtherStockInDetail;
