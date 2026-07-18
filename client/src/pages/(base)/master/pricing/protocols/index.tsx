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
import { TableHeaderOperation, useTable, useTableOperate } from '@/features/table';
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

/** 后端 FixedDateTime 支持 yyyy-MM-dd / yyyy-MM-dd HH:mm:ss；表单统一提交日期部分 */
function formatDateValue(value: unknown) {
  if (!value) return null;
  if (dayjs.isDayjs(value)) {
    return value.format('YYYY-MM-DD');
  }
  const text = String(value).trim();
  if (!text) return null;
  // 接口回显可能带时间，提交只保留日期，避免 dayjs 无效对象被 toString
  const parsed = dayjs(text);
  return parsed.isValid() ? parsed.format('YYYY-MM-DD') : text;
}

const protocolSearchParams = {
  ...createDefaultSearchParams(),
  quotationId: null
} satisfies Api.CustomerProtocol.SearchParams;

const ProtocolManage = () => {
  const { t } = useTranslation();
  const nav = useNavigate();

  const { columnChecks, data, run, searchProps, setColumnChecks, tableProps, tableWrapperRef } = useTable({
    apiFn: fetchGetCustomerProtocolList,
    apiParams: protocolSearchParams,
    columns: () => [
      createIndexColumn(t),
      {
        align: 'center',
        dataIndex: 'name',
        ellipsis: true,
        fixed: 'left',
        key: 'name',
        render: (name: string, record) => (
          <AButton
            className="h-auto p-0 leading-normal"
            size="small"
            type="link"
            onClick={() => nav(`/master/pricing/protocols/detail/${record.id}`)}
          >
            {name}
          </AButton>
        ),
        title: t('page.customer.protocol.name'),
        width: 240
      },
      {
        align: 'center',
        dataIndex: 'code',
        ellipsis: true,
        key: 'code',
        title: t('page.customer.protocol.code')
      },
      {
        align: 'center',
        dataIndex: 'effectiveStart',
        ellipsis: true,
        key: 'effectiveStart',
        title: t('page.customer.protocol.effectiveStart'),
        width: 170
      },
      {
        align: 'center',
        dataIndex: 'effectiveEnd',
        ellipsis: true,
        key: 'effectiveEnd',
        title: t('page.customer.protocol.effectiveEnd'),
        width: 170
      },
      {
        align: 'center',
        dataIndex: 'status',
        key: 'status',
        render: (_, record) => renderEnableStatus(record.status),
        title: t('page.customer.protocol.status')
      },
      {
        align: 'center',
        dataIndex: 'createTime',
        ellipsis: true,
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
              onClick={() => nav(`/master/pricing/protocols/detail/${record.id}`)}
            >
              {t('page.customer.protocol.manageGoods')}
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
        width: 280
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
      // DatePicker 通过 getValueProps 转 dayjs，表单值保持字符串
      effectiveEnd: detail?.effectiveEnd || null,
      effectiveStart: detail?.effectiveStart || null,
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
