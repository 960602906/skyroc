import {
  CrudPageLayout,
  createDefaultPagination,
  createDefaultSearchParams,
  createIndexColumn,
  displayDateTime,
  formatUtcBoundary,
  renderPickupTaskStatus,
  transformDateRange,
  transformEnumFields
} from '@/features/crud';
import { TableHeaderOperation, useTable } from '@/features/table';
import {
  fetchGetAfterSalePickupTasks,
  fetchPickupTasksCompleteAfterSale,
  fetchPickupTasksStartAfterSale
} from '@/service/api';
import { PickupTaskStatus } from '@/service/enums';
import { useDriverOptions } from '@/service/hooks';
import { formatField } from '@/utils/common';

import PickupTaskSearch from './modules/PickupTaskSearch';

const pickupTaskSearchParams = {
  ...createDefaultSearchParams(),
  afterSaleId: null,
  driverId: null,
  keyword: null,
  pickupStatus: null,
  plannedEnd: null,
  plannedRange: null,
  plannedStart: null
} satisfies Api.AfterSale.PickupTaskSearchParams;

/** 售后取货任务分页、调度和履约页面。 */
const PickupTaskList = () => {
  const { t } = useTranslation();
  const nav = useNavigate();
  const { data: drivers = [] } = useDriverOptions();

  const { columnChecks, run, searchProps, setColumnChecks, tableProps, tableWrapperRef } = useTable({
    apiFn: fetchGetAfterSalePickupTasks,
    apiParams: pickupTaskSearchParams,
    columns: () => [
      createIndexColumn(t),
      {
        align: 'center',
        dataIndex: 'taskNo',
        ellipsis: true,
        fixed: 'left',
        key: 'taskNo',
        render: (value: string, record) => (
          <AButton
            className="h-auto p-0 leading-normal"
            size="small"
            type="link"
            onClick={() => nav(`/orders/pickup-tasks/detail/${record.id}`)}
          >
            {value}
          </AButton>
        ),
        title: t('page.pickupTask.taskNo'),
        width: 155
      },
      {
        align: 'center',
        dataIndex: 'afterSaleNo',
        ellipsis: true,
        key: 'afterSaleNo',
        render: (value: string, record) => (
          <AButton
            className="h-auto p-0 leading-normal"
            size="small"
            type="link"
            onClick={() => nav(`/orders/after-sales/detail/${record.afterSaleId}`)}
          >
            {value}
          </AButton>
        ),
        title: t('page.pickupTask.afterSaleNo'),
        width: 150
      },
      {
        align: 'center',
        dataIndex: 'customerName',
        ellipsis: true,
        key: 'customerName',
        title: t('page.pickupTask.customerName'),
        width: 150
      },
      {
        align: 'left',
        dataIndex: 'goodsName',
        ellipsis: true,
        key: 'goodsName',
        render: (value: string, record) => (
          <div className="flex flex-col gap-2px">
            <span>{value}</span>
            <span className="text-12px text-[var(--ant-color-text-secondary)]">
              {formatField(record, ['quantity', 'goodsUnitName'], ' ')}
            </span>
          </div>
        ),
        title: t('page.pickupTask.goods'),
        width: 180
      },
      {
        align: 'left',
        dataIndex: 'pickupAddress',
        ellipsis: true,
        key: 'pickupAddress',
        render: (value: string, record) => (
          <div className="flex flex-col gap-2px">
            <span className="truncate">{value}</span>
            <span className="text-12px text-[var(--ant-color-text-secondary)]">
              {formatField(record, ['contactName', 'contactPhone'], ' · ') || '-'}
            </span>
          </div>
        ),
        title: t('page.pickupTask.pickupAddress'),
        width: 240
      },
      {
        align: 'center',
        dataIndex: 'driverName',
        ellipsis: true,
        key: 'driverName',
        render: (value: string | null, record) => (
          <div className="flex flex-col gap-2px">
            <span>{value || '-'}</span>
            <span className="text-12px text-[var(--ant-color-text-secondary)]">{record.driverPhone || ''}</span>
          </div>
        ),
        title: t('page.pickupTask.driver'),
        width: 140
      },
      {
        align: 'center',
        dataIndex: 'pickupStatus',
        key: 'pickupStatus',
        render: value => renderPickupTaskStatus(value),
        title: t('page.pickupTask.pickupStatus'),
        width: 120
      },
      {
        align: 'center',
        className: 'whitespace-nowrap',
        dataIndex: 'plannedPickupTime',
        key: 'plannedPickupTime',
        render: (value: string | null) => displayDateTime(value),
        title: t('page.pickupTask.plannedPickupTime'),
        width: 170
      },
      {
        align: 'center',
        dataIndex: 'stockInOrderId',
        key: 'stockInOrderId',
        render: (value: string | null) =>
          value ? (
            <ATag color="success">{t('page.pickupTask.stockInGenerated')}</ATag>
          ) : (
            t('page.pickupTask.stockInPending')
          ),
        title: t('page.pickupTask.stockInStatus'),
        width: 130
      },
      {
        align: 'center',
        fixed: 'right',
        key: 'operate',
        render: (_, record) => {
          const canAssign =
            record.pickupStatus === PickupTaskStatus.PENDING_ASSIGN ||
            record.pickupStatus === PickupTaskStatus.PENDING_PICKUP;
          const canStart = record.pickupStatus === PickupTaskStatus.PENDING_PICKUP;
          const canComplete = record.pickupStatus === PickupTaskStatus.PICKING_UP;

          return (
            <div className="flex-center flex-wrap gap-8px">
              {canAssign && (
                <AButton
                  size="small"
                  type="primary"
                  onClick={() => nav(`/orders/pickup-tasks/operate/${record.id}`)}
                >
                  {t('page.pickupTask.schedule')}
                </AButton>
              )}
              {canStart && (
                <APopconfirm
                  title={t('page.pickupTask.startConfirm', { taskNo: record.taskNo })}
                  onConfirm={() => handleStart(record.id)}
                >
                  <AButton size="small">{t('page.pickupTask.start')}</AButton>
                </APopconfirm>
              )}
              {canComplete && (
                <APopconfirm
                  title={t('page.pickupTask.completeConfirm', { taskNo: record.taskNo })}
                  onConfirm={() => handleComplete(record.id)}
                >
                  <AButton
                    ghost
                    size="small"
                    type="primary"
                  >
                    {t('page.pickupTask.complete')}
                  </AButton>
                </APopconfirm>
              )}
            </div>
          );
        },
        title: t('common.operate'),
        width: 210
      }
    ],
    pagination: createDefaultPagination(),
    scroll: { x: 'max-content' },
    transformParams: params => {
      if (!params) return params;
      let next = transformDateRange(params, {
        endKey: 'plannedEnd',
        formatter: formatUtcBoundary,
        rangeKey: 'plannedRange',
        startKey: 'plannedStart'
      });
      next = transformEnumFields(next, ['pickupStatus']);
      return next;
    }
  });

  async function handleStart(id: string) {
    await fetchPickupTasksStartAfterSale(id);
    window.$message?.success(t('common.updateSuccess'));
    await run(false);
  }

  async function handleComplete(id: string) {
    await fetchPickupTasksCompleteAfterSale(id);
    window.$message?.success(t('common.updateSuccess'));
    await run(false);
  }

  return (
    <CrudPageLayout
      tableWrapperRef={tableWrapperRef}
      title={t('page.pickupTask.title')}
      extra={
        <TableHeaderOperation
          disabledDelete
          add={() => nav('/orders/after-sales/operate')}
          columns={columnChecks}
          loading={tableProps.loading}
          refresh={run}
          setColumnChecks={setColumnChecks}
          onDelete={() => undefined}
        >
          <AButton
            ghost
            icon={<IconIcRoundPlus className="text-icon" />}
            size="small"
            type="primary"
            onClick={() => nav('/orders/after-sales/operate')}
          >
            {t('page.pickupTask.addAfterSale')}
          </AButton>
        </TableHeaderOperation>
      }
      search={
        <PickupTaskSearch
          {...searchProps}
          drivers={drivers}
        />
      }
      table={
        <ATable
          size="small"
          {...tableProps}
        />
      }
    />
  );
};

export default PickupTaskList;
