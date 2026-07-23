import { Modal } from 'antd';
import { type LoaderFunctionArgs, redirect, useLoaderData } from 'react-router-dom';

import { DetailPageLayout, renderAfterSaleStatus } from '@/features/crud';
import {
  fetchApproveAfterSale,
  fetchCompleteAfterSale,
  fetchDeleteAfterSale,
  fetchGetAfterSaleDetail,
  fetchRejectAfterSale,
  fetchResubmitAfterSale,
  fetchReverseAfterSale,
  fetchSubmitAfterSale
} from '@/service/api';
import { AfterSaleAuditAction, AfterSaleStatus } from '@/service/enums';

import AfterSaleDetailView from './modules/AfterSaleDetailView';

const LIST_PATH = '/orders/after-sales';

export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) {
    return redirect(LIST_PATH);
  }

  try {
    const detail = await fetchGetAfterSaleDetail(id);
    if (!detail) {
      return redirect(LIST_PATH);
    }
    return detail;
  } catch {
    return redirect(LIST_PATH);
  }
}

const AfterSaleDetailPage = () => {
  const { t } = useTranslation();
  const nav = useNavigate();
  const detail = useLoaderData() as Api.AfterSale.Entity;

  const isDraft = detail.afterStatus === AfterSaleStatus.DRAFT;
  const isPendingAudit = detail.afterStatus === AfterSaleStatus.PENDING_AUDIT;
  const isPendingHandle =
    detail.afterStatus === AfterSaleStatus.RETURN_PENDING || detail.afterStatus === AfterSaleStatus.REFUND_PENDING;
  const latestAction = detail.auditLogs?.[0]?.action ?? null;
  const isRejectedDraft = isDraft && latestAction === AfterSaleAuditAction.REJECT;
  const canDelete = isDraft && latestAction === null;
  const canReverse = isPendingHandle && (!detail.pickupTasks || detail.pickupTasks.length === 0);

  function promptRemark(title: string, required: boolean) {
    return new Promise<string | undefined>((resolve, reject) => {
      let remark = '';
      Modal.confirm({
        content: (
          <AInput.TextArea
            allowClear
            autoSize={{ maxRows: 4, minRows: 2 }}
            placeholder={t(required ? 'page.afterSale.list.form.requiredRemark' : 'page.afterSale.list.form.remark')}
            onChange={e => {
              remark = e.target.value;
            }}
          />
        ),
        onCancel: () => reject(new Error('cancelled')),
        onOk: async () => {
          const value = remark.trim();
          if (required && !value) {
            window.$message?.warning(t('page.afterSale.list.remarkRequired'));
            throw new Error('remark-required');
          }
          resolve(value || undefined);
        },
        title
      });
    });
  }

  async function handleAction(action: 'approve' | 'reject' | 'resubmit' | 'reverse' | 'submit') {
    try {
      const required = action === 'reject' || action === 'reverse';
      const remark = await promptRemark(
        t(`page.afterSale.list.${action}Confirm`, { afterSaleNo: detail.afterSaleNo }),
        required
      );
      const handlers = {
        approve: fetchApproveAfterSale,
        reject: fetchRejectAfterSale,
        resubmit: fetchResubmitAfterSale,
        reverse: fetchReverseAfterSale,
        submit: fetchSubmitAfterSale
      };
      await handlers[action](detail.id, { remark });
      window.$message?.success(t('common.updateSuccess'));
    } catch {
      /* 取消 */
    }
  }

  function handleComplete() {
    Modal.confirm({
      content: t('page.afterSale.list.completeTip'),
      onOk: async () => {
        await fetchCompleteAfterSale(detail.id);
        window.$message?.success(t('common.updateSuccess'));
      },
      title: t('page.afterSale.list.completeConfirm', { afterSaleNo: detail.afterSaleNo })
    });
  }

  function handleDelete() {
    Modal.confirm({
      onOk: async () => {
        await fetchDeleteAfterSale(detail.id);
        window.$message?.success(t('common.deleteSuccess'));
      },
      title: t('common.confirmDelete')
    });
  }

  return (
    <DetailPageLayout
      backLabel={t('page.afterSale.detail.back')}
      listPath={LIST_PATH}
      title={detail.afterSaleNo}
      banner={
        <ASpace size="middle">
          <span>
            {t('page.afterSale.list.customerName')}：{detail.customerName}
          </span>
          {renderAfterSaleStatus(detail.afterStatus)}
        </ASpace>
      }
      extra={
        <>
          {isDraft && (
            <>
              <AButton
                type="primary"
                onClick={() => nav(`/orders/after-sales/operate/${detail.id}`)}
              >
                {t('common.edit')}
              </AButton>
              <AButton
                type="primary"
                onClick={() => handleAction(isRejectedDraft ? 'resubmit' : 'submit')}
              >
                {t(isRejectedDraft ? 'page.afterSale.list.resubmit' : 'page.afterSale.list.submit')}
              </AButton>
            </>
          )}
          {isPendingAudit && (
            <>
              <AButton
                type="primary"
                onClick={() => handleAction('approve')}
              >
                {t('page.afterSale.list.approve')}
              </AButton>
              <AButton
                danger
                onClick={() => handleAction('reject')}
              >
                {t('page.afterSale.list.reject')}
              </AButton>
            </>
          )}
          {isPendingHandle && (
            <>
              <AButton
                type="primary"
                onClick={handleComplete}
              >
                {t('page.afterSale.list.complete')}
              </AButton>
              {canReverse && (
                <AButton
                  danger
                  onClick={() => handleAction('reverse')}
                >
                  {t('page.afterSale.list.reverse')}
                </AButton>
              )}
            </>
          )}
          {canDelete && (
            <AButton
              danger
              onClick={handleDelete}
            >
              {t('common.delete')}
            </AButton>
          )}
        </>
      }
    >
      <AfterSaleDetailView detail={detail} />
    </DetailPageLayout>
  );
};

export default AfterSaleDetailPage;
