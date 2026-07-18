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
  fetchAddWare,
  fetchBatchDeleteWare,
  fetchDeleteWare,
  fetchGetWareDetail,
  fetchGetWareList,
  fetchToggleWareStatus,
  fetchUpdateWare
} from '@/service/api';

import WareSearch from './modules/WareSearch';

const WareOperateDrawer = lazy(() => import('./modules/WareOperateDrawer'));

const WareManage = () => {
  const { t } = useTranslation();
  const nav = useNavigate();

  const { columnChecks, data, run, searchProps, setColumnChecks, tableProps, tableWrapperRef } = useTable({
    apiFn: fetchGetWareList,
    apiParams: createDefaultSearchParams(),
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
            onClick={() => nav(`/master/supply/wares/detail/${record.id}`)}
          >
            {name}
          </AButton>
        ),
        title: t('page.storage.ware.name')
      },
      {
        align: 'center',
        dataIndex: 'code',
        key: 'code',
        title: t('page.storage.ware.code')
      },
      {
        align: 'center',
        dataIndex: 'contactName',
        key: 'contactName',
        title: t('page.storage.ware.contactName')
      },
      {
        align: 'center',
        dataIndex: 'contactPhone',
        key: 'contactPhone',
        title: t('page.storage.ware.contactPhone'),
        width: 130
      },
      {
        dataIndex: 'address',
        ellipsis: true,
        key: 'address',
        title: t('page.storage.ware.address')
      },
      {
        align: 'center',
        dataIndex: 'sort',
        key: 'sort',
        title: t('page.storage.ware.sort'),
        width: 80
      },
      {
        align: 'center',
        dataIndex: 'status',
        key: 'status',
        render: (_, record) => renderEnableStatus(record.status),
        title: t('page.storage.ware.status'),
        width: 90
      },
      {
        align: 'center',
        dataIndex: 'createTime',
        key: 'createTime',
        title: t('page.storage.ware.createTime'),
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
        await fetchAddWare(res);
      } else {
        await fetchUpdateWare(res);
      }
    });

  async function handleBatchDelete() {
    await fetchBatchDeleteWare(checkedRowKeys.map(key => key as string));
    onBatchDeleted();
  }

  async function handleDelete(id: string) {
    await fetchDeleteWare(id);
    onDeleted();
  }

  async function edit(id: string) {
    const detail = await fetchGetWareDetail(id);
    handleEdit({ ...(detail ?? {}), index: 0 });
  }

  async function handleToggleStatus(record: Api.Ware.Entity) {
    const nextStatus: Api.Common.EnableStatus = record.status === 1 ? 2 : 1;
    await toggleEntityStatus({
      params: { id: record.id, status: nextStatus },
      refresh: run,
      t,
      toggleFn: fetchToggleWareStatus
    });
  }

  return (
    <CrudPageLayout
      search={<WareSearch {...searchProps} />}
      tableWrapperRef={tableWrapperRef}
      title={t('page.storage.ware.title')}
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
            <WareOperateDrawer {...generalPopupOperation} />
          </Suspense>
        </>
      }
    />
  );
};

export default WareManage;
