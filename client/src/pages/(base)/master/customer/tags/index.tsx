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
  fetchAddCustomerTag,
  fetchBatchDeleteCustomerTag,
  fetchDeleteCustomerTag,
  fetchGetCustomerTagDetail,
  fetchGetCustomerTagList,
  fetchToggleCustomerTagStatus,
  fetchUpdateCustomerTag
} from '@/service/api';

import CustomerTagSearch from './modules/CustomerTagSearch';

const CustomerTagOperateModal = lazy(() => import('./modules/CustomerTagOperateModal'));

const CustomerTagManage = () => {
  const { t } = useTranslation();

  const { scrollConfig, tableWrapperRef } = useTableScroll();

  const { columnChecks, data, run, searchProps, setColumnChecks, tableProps } = useTable({
    apiFn: fetchGetCustomerTagList,
    apiParams: createDefaultSearchParams(),
    columns: () => [
      createIndexColumn(t),
      {
        align: 'center',
        dataIndex: 'name',
        key: 'name',
        minWidth: 140,
        title: t('page.customer.tag.name')
      },
      {
        align: 'center',
        dataIndex: 'code',
        key: 'code',
        minWidth: 120,
        title: t('page.customer.tag.code')
      },
      {
        align: 'center',
        dataIndex: 'sort',
        key: 'sort',
        title: t('page.customer.tag.sort'),
        width: 80
      },
      {
        align: 'center',
        dataIndex: 'status',
        key: 'status',
        render: (_, record) => renderEnableStatus(record.status, t),
        title: t('page.customer.tag.status'),
        width: 90
      },
      {
        align: 'center',
        dataIndex: 'createTime',
        key: 'createTime',
        title: t('page.customer.tag.createTime'),
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
        await fetchAddCustomerTag(res);
      } else {
        await fetchUpdateCustomerTag(res);
      }
    });

  async function handleBatchDelete() {
    await fetchBatchDeleteCustomerTag(checkedRowKeys.map(key => key as string));
    onBatchDeleted();
  }

  async function handleDelete(id: string) {
    await fetchDeleteCustomerTag(id);
    onDeleted();
  }

  async function edit(id: string) {
    const detail = await fetchGetCustomerTagDetail(id);
    handleEdit({ ...(detail ?? {}), index: 0 });
  }

  async function handleToggleStatus(record: Api.CustomerTag.Entity) {
    const nextStatus: Api.Common.EnableStatus = record.status === 1 ? 2 : 1;
    await toggleEntityStatus({
      params: { id: record.id, status: nextStatus },
      refresh: run,
      t,
      toggleFn: fetchToggleCustomerTagStatus
    });
  }

  return (
    <CrudPageLayout
      search={<CustomerTagSearch {...searchProps} />}
      tableWrapperRef={tableWrapperRef}
      title={t('page.customer.tag.title')}
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
            <CustomerTagOperateModal {...generalPopupOperation} />
          </Suspense>
        </>
      }
    />
  );
};

export default CustomerTagManage;
