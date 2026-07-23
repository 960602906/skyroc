import { Modal } from 'antd';
import { type LoaderFunctionArgs, redirect, useLoaderData } from 'react-router-dom';

import { DetailPageLayout, renderSaleOrderStatus } from '@/features/crud';
import {
  fetchApproveOrder,
  fetchDeleteOrder,
  fetchGetOrderDetail,
  fetchRejectOrder,
  fetchResubmitOrder
} from '@/service/api';
import { SaleOrderStatus } from '@/service/enums';

import OrderDetailView from './modules/OrderDetailView';

const LIST_PATH = '/orders/list';

export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) {
    return redirect(LIST_PATH);
  }

  try {
    const detail = await fetchGetOrderDetail(id);
    if (!detail) {
      return redirect(LIST_PATH);
    }
    return detail;
  } catch {
    return redirect(LIST_PATH);
  }
}

const OrderDetailPage = () => {
  const { t } = useTranslation();
  const detail = useLoaderData() as Api.Order.Entity;
  const nav = useNavigate();

  const isPendingAudit = detail.orderStatus === SaleOrderStatus.PENDING_AUDIT;
  const isRejected = detail.orderStatus === SaleOrderStatus.REJECTED;
  const canEdit = isPendingAudit || isRejected;

  function promptAuditRemark(title: string) {
    return new Promise<string | undefined>((resolve, reject) => {
      let remark = '';
      Modal.confirm({
        content: (
          <AInput.TextArea
            allowClear
            autoSize={{ maxRows: 4, minRows: 2 }}
            placeholder={t('page.order.list.form.auditRemark')}
            onChange={e => {
              remark = e.target.value;
            }}
          />
        ),
        onCancel: () => reject(new Error('cancelled')),
        onOk: () => resolve(remark.trim() || undefined),
        title
      });
    });
  }

  async function handleApprove() {
    try {
      const remark = await promptAuditRemark(t('page.order.list.approveConfirm', { orderNo: detail.orderNo }));
      await fetchApproveOrder(detail.id, { remark });
      window.$message?.success(t('common.updateSuccess'));
    } catch {
      /* 取消 */
    }
  }

  async function handleReject() {
    try {
      const remark = await promptAuditRemark(t('page.order.list.rejectConfirm', { orderNo: detail.orderNo }));
      await fetchRejectOrder(detail.id, { remark });
      window.$message?.success(t('common.updateSuccess'));
    } catch {
      /* 取消 */
    }
  }

  async function handleResubmit() {
    try {
      const remark = await promptAuditRemark(t('page.order.list.resubmitConfirm', { orderNo: detail.orderNo }));
      await fetchResubmitOrder(detail.id, { remark });
      window.$message?.success(t('common.updateSuccess'));
    } catch {
      /* 取消 */
    }
  }

  function handleDelete() {
    Modal.confirm({
      onOk: async () => {
        await fetchDeleteOrder(detail.id);
        window.$message?.success(t('common.deleteSuccess'));
      },
      title: t('common.confirmDelete')
    });
  }

  return (
    <DetailPageLayout
      backLabel={t('page.order.detail.back')}
      listPath={LIST_PATH}
      title={detail.orderNo}
      banner={
        <ASpace size="middle">
          <span>
            {t('page.order.list.customerName')}：{detail.customerName}
          </span>
          {renderSaleOrderStatus(detail.orderStatus)}
        </ASpace>
      }
      extra={
        <>
          {canEdit && (
            <AButton
              type="primary"
              onClick={() => nav(`/orders/edit/${detail.id}`)}
            >
              {t('common.edit')}
            </AButton>
          )}
          {isPendingAudit && (
            <>
              <AButton
                type="primary"
                onClick={handleApprove}
              >
                {t('page.order.list.approve')}
              </AButton>
              <AButton
                danger
                onClick={handleReject}
              >
                {t('page.order.list.reject')}
              </AButton>
            </>
          )}
          {isRejected && (
            <AButton
              type="primary"
              onClick={handleResubmit}
            >
              {t('page.order.list.resubmit')}
            </AButton>
          )}
          <AButton
            danger
            onClick={handleDelete}
          >
            {t('common.delete')}
          </AButton>
        </>
      }
    >
      <OrderDetailView detail={detail} />
    </DetailPageLayout>
  );
};

export default OrderDetailPage;
