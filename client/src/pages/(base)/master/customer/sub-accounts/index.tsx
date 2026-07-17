import { Suspense, lazy } from 'react';

import {
  CrudPageLayout,
  createDefaultPagination,
  createIndexColumn,
  renderEnableStatus,
  toggleEntityStatus
} from '@/features/crud';
import { TableHeaderOperation, useTable, useTableOperate, useTableScroll } from '@/features/table';
import {
  fetchAddCustomerSubAccount,
  fetchBatchDeleteCustomerSubAccount,
  fetchDeleteCustomerSubAccount,
  fetchGetCustomerSubAccountDetail,
  fetchGetCustomerSubAccountList,
  fetchToggleCustomerSubAccountStatus,
  fetchUpdateCustomerSubAccount
} from '@/service/api';

import SubAccountSearch from './modules/SubAccountSearch';

const SubAccountOperateDrawer = lazy(() => import('./modules/SubAccountOperateDrawer'));

const defaultSearchParams: Api.CustomerSubAccount.SearchParams = {
  companyId: null,
  current: 1,
  customerId: null,
  nickName: null,
  size: 10,
  status: null,
  username: null
};

const SubAccountManage = () => {
  const { t } = useTranslation();

  const { scrollConfig, tableWrapperRef } = useTableScroll();

  const { columnChecks, data, run, searchProps, setColumnChecks, tableProps } = useTable({
    apiFn: fetchGetCustomerSubAccountList,
    apiParams: defaultSearchParams,
    columns: () => [
      createIndexColumn(t),
      {
        align: 'center',
        dataIndex: 'username',
        key: 'username',
        minWidth: 120,
        title: t('page.customer.subAccount.username')
      },
      {
        align: 'center',
        dataIndex: 'nickName',
        key: 'nickName',
        minWidth: 120,
        title: t('page.customer.subAccount.nickName')
      },
      {
        align: 'center',
        dataIndex: 'phone',
        key: 'phone',
        title: t('page.customer.subAccount.phone'),
        width: 130
      },
      {
        align: 'center',
        dataIndex: 'email',
        key: 'email',
        minWidth: 160,
        title: t('page.customer.subAccount.email')
      },
      {
        align: 'center',
        dataIndex: 'status',
        key: 'status',
        render: (_, record) => renderEnableStatus(record.status, t),
        title: t('page.customer.subAccount.status'),
        width: 90
      },
      {
        align: 'center',
        dataIndex: 'createTime',
        key: 'createTime',
        title: t('page.customer.subAccount.createTime'),
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
        await fetchAddCustomerSubAccount(res);
      } else {
        await fetchUpdateCustomerSubAccount(res);
      }
    });

  async function handleBatchDelete() {
    await fetchBatchDeleteCustomerSubAccount(checkedRowKeys.map(key => key as string));
    onBatchDeleted();
  }

  async function handleDelete(id: string) {
    await fetchDeleteCustomerSubAccount(id);
    onDeleted();
  }

  async function edit(id: string) {
    const detail = await fetchGetCustomerSubAccountDetail(id);
    handleEdit({ ...(detail ?? {}), index: 0 });
  }

  async function handleToggleStatus(record: Api.CustomerSubAccount.Entity) {
    const nextStatus: Api.Common.EnableStatus = record.status === 1 ? 2 : 1;
    await toggleEntityStatus({
      params: { id: record.id, status: nextStatus },
      refresh: run,
      t,
      toggleFn: fetchToggleCustomerSubAccountStatus
    });
  }

  return (
    <CrudPageLayout
      search={<SubAccountSearch {...searchProps} />}
      tableWrapperRef={tableWrapperRef}
      title={t('page.customer.subAccount.title')}
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
            <SubAccountOperateDrawer {...generalPopupOperation} />
          </Suspense>
        </>
      }
    />
  );
};

export default SubAccountManage;
