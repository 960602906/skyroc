import dayjs from 'dayjs';
import { Suspense, lazy } from 'react';

import {
  CrudPageLayout,
  createDefaultPagination,
  createDefaultSearchParams,
  createIndexColumn,
  renderEnableStatus,
  toggleEntityStatus
} from '@/features/crud';
import { TableHeaderOperation, useTable, useTableOperate, useTableScroll } from '@/features/table';
import {
  fetchAddCustomerProtocol,
  fetchBatchDeleteCustomerProtocol,
  fetchDeleteCustomerProtocol,
  fetchGetCustomerProtocolDetail,
  fetchGetCustomerProtocolList,
  fetchToggleCustomerProtocolStatus,
  fetchUpdateCustomerProtocol
} from '@/service/api';

import ProtocolSearch from './modules/ProtocolSearch';

const ProtocolOperateDrawer = lazy(() => import('./modules/ProtocolOperateDrawer'));

function formatDateValue(value: unknown) {
  if (!value) return null;
  if (dayjs.isDayjs(value)) {
    return value.format('YYYY-MM-DD');
  }
  return String(value);
}

const protocolSearchParams = {
  ...createDefaultSearchParams(),
  quotationId: null
} satisfies Api.CustomerProtocol.SearchParams;

const ProtocolManage = () => {
  const { t } = useTranslation();

  const { scrollConfig, tableWrapperRef } = useTableScroll();

  const { columnChecks, data, run, searchProps, setColumnChecks, tableProps } = useTable({
    apiFn: fetchGetCustomerProtocolList,
    apiParams: protocolSearchParams,
    columns: () => [
      createIndexColumn(t),
      {
        align: 'center',
        dataIndex: 'name',
        key: 'name',
        minWidth: 140,
        title: t('page.customer.protocol.name')
      },
      {
        align: 'center',
        dataIndex: 'code',
        key: 'code',
        minWidth: 120,
        title: t('page.customer.protocol.code')
      },
      {
        align: 'center',
        dataIndex: 'effectiveStart',
        key: 'effectiveStart',
        title: t('page.customer.protocol.effectiveStart'),
        width: 120
      },
      {
        align: 'center',
        dataIndex: 'effectiveEnd',
        key: 'effectiveEnd',
        title: t('page.customer.protocol.effectiveEnd'),
        width: 120
      },
      {
        align: 'center',
        dataIndex: 'status',
        key: 'status',
        render: (_, record) => renderEnableStatus(record.status, t),
        title: t('page.customer.protocol.status'),
        width: 90
      },
      {
        align: 'center',
        dataIndex: 'createTime',
        key: 'createTime',
        title: t('page.customer.protocol.createTime'),
        width: 170
      },
      {
        align: 'center',
        fixed: 'right',
        key: 'operate',
        render: (_, record) => (
          <div className="flex-center gap-8px">
            <AButton
              ghost
              size="small"
              type="primary"
              onClick={() => edit(record.id)}
            >
              {t('common.edit')}
            </AButton>
            <AButton
              size="small"
              onClick={() => handleToggleStatus(record)}
            >
              {record.status === 1 ? t('page.manage.common.status.disable') : t('page.manage.common.status.enable')}
            </AButton>
            <APopconfirm
              title={t('common.confirmDelete')}
              onConfirm={() => handleDelete(record.id)}
            >
              <AButton
                danger
                size="small"
              >
                {t('common.delete')}
              </AButton>
            </APopconfirm>
          </div>
        ),
        title: t('common.operate'),
        width: 210
      }
    ],
    pagination: createDefaultPagination()
  });

  const { checkedRowKeys, generalPopupOperation, handleAdd, handleEdit, onBatchDeleted, onDeleted, rowSelection } =
    useTableOperate(data, run, async (res, type) => {
      const payload = {
        ...res,
        effectiveEnd: formatDateValue(res.effectiveEnd),
        effectiveStart: formatDateValue(res.effectiveStart)
      } as Api.CustomerProtocol.UpdateParams;

      if (type === 'add') {
        await fetchAddCustomerProtocol(payload);
      } else {
        await fetchUpdateCustomerProtocol(payload);
      }
    });

  async function handleBatchDelete() {
    await fetchBatchDeleteCustomerProtocol(checkedRowKeys.map(key => key as string));
    onBatchDeleted();
  }

  async function handleDelete(id: string) {
    await fetchDeleteCustomerProtocol(id);
    onDeleted();
  }

  async function edit(id: string) {
    const detail = await fetchGetCustomerProtocolDetail(id);
    handleEdit({
      ...(detail ?? {}),
      effectiveEnd: detail?.effectiveEnd ? dayjs(detail.effectiveEnd) : null,
      effectiveStart: detail?.effectiveStart ? dayjs(detail.effectiveStart) : null,
      index: 0
    } as never);
  }

  async function handleToggleStatus(record: Api.CustomerProtocol.Entity) {
    const nextStatus: Api.Common.EnableStatus = record.status === 1 ? 2 : 1;
    await toggleEntityStatus({
      params: { id: record.id, status: nextStatus },
      refresh: run,
      t,
      toggleFn: fetchToggleCustomerProtocolStatus
    });
  }

  return (
    <CrudPageLayout
      search={<ProtocolSearch {...searchProps} />}
      tableWrapperRef={tableWrapperRef}
      title={t('page.customer.protocol.title')}
      extra={
        <TableHeaderOperation
          add={handleAdd}
          columns={columnChecks}
          disabledDelete={checkedRowKeys.length === 0}
          loading={tableProps.loading}
          refresh={run}
          setColumnChecks={setColumnChecks}
          onDelete={handleBatchDelete}
        />
      }
      table={
        <>
          <ATable
            rowSelection={rowSelection}
            scroll={scrollConfig}
            size="small"
            {...tableProps}
          />

          <Suspense>
            <ProtocolOperateDrawer {...generalPopupOperation} />
          </Suspense>
        </>
      }
    />
  );
};

export default ProtocolManage;
