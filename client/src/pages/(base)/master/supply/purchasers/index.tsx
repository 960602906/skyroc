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
  fetchAddPurchaser,
  fetchBatchDeletePurchaser,
  fetchDeletePurchaser,
  fetchGetPurchaserDetail,
  fetchGetPurchaserList,
  fetchTogglePurchaserStatus,
  fetchUpdatePurchaser
} from '@/service/api';

import PurchaserSearch from './modules/PurchaserSearch';

const PurchaserOperateDrawer = lazy(() => import('./modules/PurchaserOperateDrawer'));

const PurchaserManage = () => {
  const { t } = useTranslation();

  const { scrollConfig, tableWrapperRef } = useTableScroll();

  const { columnChecks, data, run, searchProps, setColumnChecks, tableProps } = useTable({
    apiFn: fetchGetPurchaserList,
    apiParams: createDefaultSearchParams(),
    columns: () => [
      createIndexColumn(t),
      {
        align: 'center',
        dataIndex: 'name',
        key: 'name',
        minWidth: 140,
        title: t('page.purchase.purchaser.name')
      },
      {
        align: 'center',
        dataIndex: 'code',
        key: 'code',
        minWidth: 120,
        title: t('page.purchase.purchaser.code')
      },
      {
        align: 'center',
        dataIndex: 'phone',
        key: 'phone',
        title: t('page.purchase.purchaser.phone'),
        width: 130
      },
      {
        align: 'center',
        dataIndex: 'departmentId',
        ellipsis: true,
        key: 'departmentId',
        minWidth: 120,
        title: t('page.purchase.purchaser.departmentId')
      },
      {
        align: 'center',
        dataIndex: 'status',
        key: 'status',
        render: (_, record) => renderEnableStatus(record.status, t),
        title: t('page.purchase.purchaser.status'),
        width: 90
      },
      {
        align: 'center',
        dataIndex: 'createTime',
        key: 'createTime',
        title: t('page.purchase.purchaser.createTime'),
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
      if (type === 'add') {
        await fetchAddPurchaser(res);
      } else {
        await fetchUpdatePurchaser(res);
      }
    });

  async function handleBatchDelete() {
    await fetchBatchDeletePurchaser(checkedRowKeys.map(key => key as string));
    onBatchDeleted();
  }

  async function handleDelete(id: string) {
    await fetchDeletePurchaser(id);
    onDeleted();
  }

  async function edit(id: string) {
    const detail = await fetchGetPurchaserDetail(id);
    handleEdit({ ...(detail ?? {}), index: 0 });
  }

  async function handleToggleStatus(record: Api.Purchaser.Entity) {
    const nextStatus: Api.Common.EnableStatus = record.status === 1 ? 2 : 1;
    await toggleEntityStatus({
      params: { id: record.id, status: nextStatus },
      refresh: run,
      t,
      toggleFn: fetchTogglePurchaserStatus
    });
  }

  return (
    <CrudPageLayout
      search={<PurchaserSearch {...searchProps} />}
      tableWrapperRef={tableWrapperRef}
      title={t('page.purchase.purchaser.title')}
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
            <PurchaserOperateDrawer {...generalPopupOperation} />
          </Suspense>
        </>
      }
    />
  );
};

export default PurchaserManage;
