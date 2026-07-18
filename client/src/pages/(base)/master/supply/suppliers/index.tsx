import { Suspense, lazy } from 'react';

import { renderEnableStatus } from '@/features/crud';
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

  const isMobile = useMobile();

  const { columnChecks, data, run, searchProps, setColumnChecks, tableProps, tableWrapperRef } = useTable({
    apiFn: fetchGetSupplierList,
    apiParams: {
      code: null,
      current: 1,
      name: null,
      size: 10,
      status: null
    },
    columns: () => [
      {
        align: 'center',
        dataIndex: 'index',
        key: 'index',
        title: t('common.index'),
        width: 64
      },
      {
        align: 'center',
        dataIndex: 'name',
        key: 'name',
        minWidth: 140,
        title: t('page.purchase.supplier.name')
      },
      {
        align: 'center',
        dataIndex: 'code',
        key: 'code',
        minWidth: 120,
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
    pagination: {
      showQuickJumper: true
    }
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
    <div className="h-full min-h-500px flex-col-stretch gap-16px overflow-hidden lt-sm:overflow-auto">
      <ACollapse
        bordered={false}
        className="card-wrapper"
        defaultActiveKey={isMobile ? undefined : '1'}
        items={[
          {
            children: <SupplierSearch {...searchProps} />,
            key: '1',
            label: t('common.search')
          }
        ]}
      />

      <ACard
        className="flex-col-stretch card-wrapper sm:flex-1-hidden"
        ref={tableWrapperRef}
        title={t('page.purchase.supplier.title')}
        variant="borderless"
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
      >
        <ATable
          rowSelection={rowSelection}
          size="small"
          {...tableProps}
        />

        <Suspense>
          <SupplierOperateDrawer {...generalPopupOperation} />
        </Suspense>
      </ACard>
    </div>
  );
};

export default SupplierManage;
