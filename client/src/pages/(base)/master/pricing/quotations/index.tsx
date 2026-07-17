import { Suspense, lazy } from 'react';

import {
  CrudPageLayout,
  createDefaultPagination,
  createDefaultSearchParams,
  createIndexColumn,
  renderBooleanTag,
  renderEnableStatus,
  toggleEntityStatus
} from '@/features/crud';
import { TableHeaderOperation, useTable, useTableOperate, useTableScroll } from '@/features/table';
import {
  fetchAddQuotation,
  fetchAuditQuotation,
  fetchBatchDeleteQuotation,
  fetchDeleteQuotation,
  fetchGetQuotationDetail,
  fetchGetQuotationList,
  fetchToggleQuotationStatus,
  fetchUpdateQuotation
} from '@/service/api';

import QuotationSearch from './modules/QuotationSearch';

const QuotationOperateDrawer = lazy(() => import('./modules/QuotationOperateDrawer'));

const QuotationManage = () => {
  const { t } = useTranslation();
  const { scrollConfig, tableWrapperRef } = useTableScroll();

  const { columnChecks, data, run, searchProps, setColumnChecks, tableProps } = useTable({
    apiFn: fetchGetQuotationList,
    apiParams: {
      ...createDefaultSearchParams(),
      isAudited: null
    },
    columns: () => [
      createIndexColumn(t),
      {
        align: 'center',
        dataIndex: 'name',
        key: 'name',
        minWidth: 140,
        title: t('page.goods.quotation.name')
      },
      {
        align: 'center',
        dataIndex: 'code',
        key: 'code',
        minWidth: 120,
        title: t('page.goods.quotation.code')
      },
      {
        align: 'center',
        dataIndex: 'effectiveStart',
        key: 'effectiveStart',
        title: t('page.goods.quotation.effectiveStart'),
        width: 120
      },
      {
        align: 'center',
        dataIndex: 'effectiveEnd',
        key: 'effectiveEnd',
        title: t('page.goods.quotation.effectiveEnd'),
        width: 120
      },
      {
        align: 'center',
        dataIndex: 'isAudited',
        key: 'isAudited',
        render: (_, record) =>
          renderBooleanTag(record.isAudited, t('page.goods.quotation.audited'), t('page.goods.quotation.unaudited')),
        title: t('page.goods.quotation.isAudited'),
        width: 100
      },
      {
        align: 'center',
        dataIndex: 'status',
        key: 'status',
        render: (_, record) => renderEnableStatus(record.status, t),
        title: t('page.goods.quotation.status'),
        width: 90
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
              onClick={() => handleAudit(record)}
            >
              {record.isAudited ? t('page.goods.quotation.unaudit') : t('page.goods.quotation.audit')}
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
      if (type === 'add') {
        await fetchAddQuotation(res);
      } else {
        await fetchUpdateQuotation(res);
      }
    });

  async function handleBatchDelete() {
    await fetchBatchDeleteQuotation(checkedRowKeys.map(key => key as string));
    onBatchDeleted();
  }

  async function handleDelete(id: string) {
    await fetchDeleteQuotation(id);
    onDeleted();
  }

  async function edit(id: string) {
    const detail = await fetchGetQuotationDetail(id);
    handleEdit({ ...(detail ?? {}), index: 0 });
  }

  function handleToggleStatus(record: Api.Quotation.Entity) {
    const nextStatus: Api.Common.EnableStatus = record.status === 1 ? 2 : 1;
    return toggleEntityStatus({
      params: { id: record.id, status: nextStatus },
      refresh: run,
      t,
      toggleFn: fetchToggleQuotationStatus
    });
  }

  async function handleAudit(record: Api.Quotation.Entity) {
    await fetchAuditQuotation(record.id, !record.isAudited);
    window.$message?.success(t('common.updateSuccess'));
    await run(false);
  }

  return (
    <CrudPageLayout
      search={<QuotationSearch {...searchProps} />}
      tableWrapperRef={tableWrapperRef}
      title={t('page.goods.quotation.title')}
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
            <QuotationOperateDrawer {...generalPopupOperation} />
          </Suspense>
        </>
      }
    />
  );
};

export default QuotationManage;
