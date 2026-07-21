import { type LoaderFunctionArgs, redirect, useLoaderData } from 'react-router-dom';

import { PickupTaskStatusBadge, displayDateTime, displayText } from '@/features/crud';
import { useCloseTabAndNavigate } from '@/features/tab';
import {
  fetchGetAfterSalePickupTaskDetail,
  fetchPickupTasksCompleteAfterSale,
  fetchPickupTasksStartAfterSale
} from '@/service/api';
import { PickupTaskStatus } from '@/service/enums';
import { formatField } from '@/utils/common';

const LIST_PATH = '/orders/pickup-tasks';

export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) {
    return redirect(LIST_PATH);
  }

  try {
    const detail = await fetchGetAfterSalePickupTaskDetail(id);
    if (!detail) {
      return redirect(LIST_PATH);
    }
    return detail;
  } catch {
    return redirect(LIST_PATH);
  }
}

/** 展示取货任务的售后来源、调度信息和履约节点。 */
const PickupTaskDetailPage = () => {
  const { t } = useTranslation();
  const nav = useNavigate();
  const detail = useLoaderData() as Api.AfterSale.PickupTask;
  const closeTabAndNavigate = useCloseTabAndNavigate();
  const canSchedule =
    detail.pickupStatus === PickupTaskStatus.PENDING_ASSIGN || detail.pickupStatus === PickupTaskStatus.PENDING_PICKUP;
  const canStart = detail.pickupStatus === PickupTaskStatus.PENDING_PICKUP;
  const canComplete = detail.pickupStatus === PickupTaskStatus.PICKING_UP;

  async function handleStart() {
    AModal.confirm({
      onOk: async () => {
        await fetchPickupTasksStartAfterSale(detail.id);
        window.$message?.success(t('common.updateSuccess'));
        closeTabAndNavigate(LIST_PATH);
      },
      title: t('page.pickupTask.startConfirm', { taskNo: detail.taskNo })
    });
  }

  async function handleComplete() {
    AModal.confirm({
      onOk: async () => {
        await fetchPickupTasksCompleteAfterSale(detail.id);
        window.$message?.success(t('common.updateSuccess'));
        closeTabAndNavigate(LIST_PATH);
      },
      title: t('page.pickupTask.completeConfirm', { taskNo: detail.taskNo })
    });
  }

  return (
    <div className="h-full min-h-500px flex-col-stretch gap-16px overflow-auto">
      <ACard
        className="card-wrapper"
        title={detail.taskNo}
        variant="borderless"
        extra={
          <ASpace>
            <AButton onClick={() => closeTabAndNavigate(LIST_PATH)}>{t('page.pickupTask.detail.back')}</AButton>
            {canSchedule && (
              <AButton
                type="primary"
                onClick={() => nav(`/orders/pickup-tasks/operate/${detail.id}`)}
              >
                {t('page.pickupTask.schedule')}
              </AButton>
            )}
            {canStart && <AButton onClick={handleStart}>{t('page.pickupTask.start')}</AButton>}
            {canComplete && (
              <AButton
                type="primary"
                onClick={handleComplete}
              >
                {t('page.pickupTask.complete')}
              </AButton>
            )}
          </ASpace>
        }
      >
        <ASpace size="middle">
          <span className="opacity-60">
            {t('page.pickupTask.customerName')}：{detail.customerName}
          </span>
          <PickupTaskStatusBadge pickupStatus={detail.pickupStatus} />
        </ASpace>
      </ACard>

      <ACard
        className="card-wrapper"
        title={t('page.pickupTask.detail.basicInfo')}
        variant="borderless"
      >
        <ADescriptions
          column={{ lg: 2, md: 2, sm: 1, xs: 1 }}
          items={[
            { children: detail.taskNo, key: 'taskNo', label: t('page.pickupTask.taskNo') },
            {
              children: (
                <AButton
                  className="h-auto p-0 leading-normal"
                  size="small"
                  type="link"
                  onClick={() => nav(`/orders/after-sales/detail/${detail.afterSaleId}`)}
                >
                  {detail.afterSaleNo}
                </AButton>
              ),
              key: 'afterSaleNo',
              label: t('page.pickupTask.afterSaleNo')
            },
            { children: detail.customerName, key: 'customerName', label: t('page.pickupTask.customerName') },
            {
              children: displayText(detail.contactName),
              key: 'contactName',
              label: t('page.pickupTask.detail.contactName')
            },
            { children: detail.goodsName, key: 'goodsName', label: t('page.pickupTask.goods') },
            {
              children: formatField(detail, ['quantity', 'goodsUnitName'], ' '),
              key: 'quantity',
              label: t('page.pickupTask.detail.quantity')
            },
            {
              children: detail.pickupAddress,
              key: 'pickupAddress',
              label: t('page.pickupTask.pickupAddress'),
              span: 2
            },
            {
              children: displayText(detail.contactPhone),
              key: 'contactPhone',
              label: t('page.pickupTask.detail.contactPhone')
            }
          ]}
        />
      </ACard>

      <ACard
        className="card-wrapper"
        title={t('page.pickupTask.detail.scheduleInfo')}
        variant="borderless"
      >
        <ADescriptions
          column={{ lg: 2, md: 2, sm: 1, xs: 1 }}
          items={[
            { children: displayText(detail.driverName), key: 'driverName', label: t('page.pickupTask.driver') },
            {
              children: displayText(detail.driverPhone),
              key: 'driverPhone',
              label: t('page.pickupTask.detail.driverPhone')
            },
            {
              children: displayDateTime(detail.plannedPickupTime),
              key: 'plannedPickupTime',
              label: t('page.pickupTask.plannedPickupTime')
            },
            {
              children: displayDateTime(detail.assignedTime),
              key: 'assignedTime',
              label: t('page.pickupTask.detail.assignedTime')
            },
            { children: displayText(detail.remark), key: 'remark', label: t('page.pickupTask.remark'), span: 2 }
          ]}
        />
      </ACard>

      <ACard
        className="card-wrapper"
        title={t('page.pickupTask.detail.executionInfo')}
        variant="borderless"
      >
        <ADescriptions
          column={{ lg: 2, md: 2, sm: 1, xs: 1 }}
          items={[
            {
              children: <PickupTaskStatusBadge pickupStatus={detail.pickupStatus} />,
              key: 'pickupStatus',
              label: t('page.pickupTask.pickupStatus')
            },
            {
              children: detail.stockInOrderId
                ? t('page.pickupTask.stockInGenerated')
                : t('page.pickupTask.stockInPending'),
              key: 'stockInStatus',
              label: t('page.pickupTask.stockInStatus')
            },
            {
              children: displayDateTime(detail.startedTime),
              key: 'startedTime',
              label: t('page.pickupTask.detail.startedTime')
            },
            {
              children: displayDateTime(detail.completedTime),
              key: 'completedTime',
              label: t('page.pickupTask.detail.completedTime')
            }
          ]}
        />
      </ACard>
    </div>
  );
};

export default PickupTaskDetailPage;
