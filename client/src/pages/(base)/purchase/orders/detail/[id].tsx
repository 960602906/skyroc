import { useState } from 'react';
import { type LoaderFunctionArgs, redirect, useLoaderData } from 'react-router-dom';

import { DetailPageLayout, renderPurchaseOrderStatus } from '@/features/crud';
import { fetchCancelPurchaseOrder, fetchCompletePurchaseOrder, fetchGetPurchaseOrderDetail } from '@/service/api';
import { PurchaseOrderStatus } from '@/service/enums';

import PurchaseOrderDetailView from './modules/PurchaseOrderDetailView';

const LIST_PATH = '/purchase/orders';

/** 路由切换前加载采购单详情，采购单不存在或加载失败时返回列表。 */
export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) return redirect(LIST_PATH);

  try {
    const detail = await fetchGetPurchaseOrderDetail(id);
    return detail ?? redirect(LIST_PATH);
  } catch {
    return redirect(LIST_PATH);
  }
}

/** 采购单基础信息、商品明细和采购计划来源详情页。 */
const PurchaseOrderDetail = () => {
  const { t } = useTranslation();
  const detail = useLoaderData() as Api.PurchaseOrder.Entity;
  const [confirmModalOpen, setConfirmModalOpen] = useState<'complete' | 'cancel' | null>(null);

  if (!detail) return null;

  const canOperate = detail.businessStatus === PurchaseOrderStatus.DRAFT;

  async function handleComplete() {
    await fetchCompletePurchaseOrder(detail.id);
    window.$message?.success(t('common.updateSuccess'));
    setConfirmModalOpen(null);
  }

  async function handleCancel() {
    await fetchCancelPurchaseOrder(detail.id);
    window.$message?.success(t('common.updateSuccess'));
    setConfirmModalOpen(null);
  }

  return (
    <DetailPageLayout
      backLabel={t('page.purchase.order.back')}
      banner={renderPurchaseOrderStatus(detail.businessStatus)}
      listPath={LIST_PATH}
      title={detail.purchaseNo}
      extra={
        canOperate && (
          <>
            <AButton
              type="primary"
              onClick={() => setConfirmModalOpen('complete')}
            >
              {t('page.purchase.order.complete')}
            </AButton>
            <AButton
              danger
              onClick={() => setConfirmModalOpen('cancel')}
            >
              {t('page.purchase.order.cancel')}
            </AButton>
          </>
        )
      }
    >
      <PurchaseOrderDetailView detail={detail} />
      <AModal
        destroyOnClose
        open={confirmModalOpen === 'complete'}
        title={t('page.purchase.order.complete')}
        onCancel={() => setConfirmModalOpen(null)}
        onOk={handleComplete}
      >
        <p>{t('page.purchase.order.complete')}?</p>
      </AModal>
      <AModal
        destroyOnClose
        open={confirmModalOpen === 'cancel'}
        title={t('page.purchase.order.cancel')}
        onCancel={() => setConfirmModalOpen(null)}
        onOk={handleCancel}
      >
        <p>{t('page.purchase.order.cancel')}?</p>
      </AModal>
    </DetailPageLayout>
  );
};

export default PurchaseOrderDetail;
