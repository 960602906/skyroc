import { Suspense, lazy } from 'react';

import {
  CrudPageLayout,
  createDefaultPagination,
  createDefaultSearchParams,
  createIndexColumn,
  renderEnableStatus
} from '@/features/crud';
import { TableHeaderOperation, useTable, useTableOperate } from '@/features/table';
import {
  fetchAddSupplier,
  fetchBatchDeleteSupplier,
  fetchDeleteSupplier,
  fetchGetSupplierDetail,
  fetchGetSupplierList,
  fetchToggleSupplierStatus,
  fetchUpdateSupplier
} from '@/service/api';

import SupplierSearch from './modules/SupplierSearch';

const SupplierOperateDrawer = lazy(() => import('./modules/SupplierOperateDrawer'));

const SupplierManage = () => {
  const { t } = useTranslation();
  const nav = useNavigate();

  const { columnChecks, data, run, searchProps, setColumnChecks, tableProps, tableWrapperRef } = useTable({
    apiFn: fetchGetSupplierList,
    apiParams: {
      ...createDefaultSearchParams(),
      code: null,
      name: null,
      status: null
    } satisfies Api.Supplier.SearchParams,
    columns: () => [
      createIndexColumn(t),
      {
        align: 'center',
        dataIndex: 'name',
        fixed: 'left',
        key: 'name',
        render: (name: string, record) => (
          <AButton
            className="p-0"
            type="link"
            onClick={() => nav(`/master/supply/suppliers/detail/${record.id}`)}
          >
            {name}
          </AButton>
        ),
        title: t('page.purchase.supplier.name')
      },
      {
        align: 'center',
        dataIndex: 'code',
        key: 'code',
        title: t('page.purchase.supplier.code')
      },
      {
        align: 'center',
        dataIndex: 'contactName',
        key: 'contactName',
        minWidth: 100,
        title: t('page.purchase.supplier.contactName')
      },
      {
        align: 'center',
        dataIndex: 'contactPhone',
        key: 'contactPhone',
        title: t('page.purchase.supplier.contactPhone'),
        width: 130
      },
      {
        dataIndex: 'address',
        ellipsis: true,
        key: 'address',
        minWidth: 160,
        title: t('page.purchase.supplier.address')
      },
      {
        align: 'center',
        dataIndex: 'status',
        key: 'status',
        render: (_, record) => renderEnableStatus(record.status),
        title: t('page.purchase.supplier.status'),
        width: 90
      },
      {
        align: 'center',
        dataIndex: 'createTime',
        key: 'createTime',
        title: t('page.purchase.supplier.createTime'),
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
    pagination: createDefaultPagination(),
    scroll: { x: 'max-content' }
  });

  const { checkedRowKeys, generalPopupOperation, handleAdd, handleEdit, onBatchDeleted, onDeleted, rowSelection } =
    useTableOperate(data, run, async (res, type) => {
      if (type === 'add') {
        await fetchAddSupplier(res);
      } else {
        await fetchUpdateSupplier(res);
      }
    });

  async function handleBatchDelete() {
    await fetchBatchDeleteSupplier(checkedRowKeys.map(key => key as string));
    onBatchDeleted();
  }

  async function handleDelete(id: string) {
    await fetchDeleteSupplier(id);
    onDeleted();
  }

  async function edit(id: string) {
    const detail = await fetchGetSupplierDetail(id);
    handleEdit({ ...(detail ?? {}), index: 0 });
  }

  async function handleToggleStatus(record: Api.Supplier.Entity) {
    const nextStatus: Api.Common.EnableStatus = record.status === 1 ? 2 : 1;
    await fetchToggleSupplierStatus({ id: record.id, status: nextStatus });
    window.$message?.success(t('common.updateSuccess'));
    await run(false);
  }

  return (
    <CrudPageLayout
      search={<SupplierSearch {...searchProps} />}
      tableWrapperRef={tableWrapperRef}
      title={t('page.purchase.supplier.title')}
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
            <SupplierOperateDrawer {...generalPopupOperation} />
          </Suspense>
        </>
      }
    />
  );
};

export default SupplierManage;
