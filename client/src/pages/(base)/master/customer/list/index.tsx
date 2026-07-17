import { useState } from 'react';

import {
  CrudPageLayout,
  createDefaultPagination,
  createDefaultSearchParams,
  createIndexColumn,
  renderEnableStatus,
  toggleEntityStatus
} from '@/features/crud';
import { TableHeaderOperation, useTable, useTableScroll } from '@/features/table';
import {
  fetchBatchDeleteCustomer,
  fetchDeleteCustomer,
  fetchGetCustomerList,
  fetchToggleCustomerStatus
} from '@/service/api';
import { toOptions, useCompanyOptions } from '@/service/hooks/useBaseDataOptions';

import CustomerListSearch from './modules/CustomerListSearch';

const customerSearchParams = {
  ...createDefaultSearchParams(),
  companyId: null,
  defaultWareId: null,
  quotationId: null,
  taxpayerIdentificationNumber: null,
  unifiedSocialCreditCode: null
} satisfies Api.Customer.SearchParams;

const CustomerListManage = () => {
  const { t } = useTranslation();

  const nav = useNavigate();

  const { scrollConfig, tableWrapperRef } = useTableScroll();

  const { data: companies } = useCompanyOptions();

  const companyNameMap = new Map(toOptions(companies).map(item => [item.value, item.label]));

  const [checkedRowKeys, setCheckedRowKeys] = useState<React.Key[]>([]);

  const { columnChecks, run, searchProps, setColumnChecks, tableProps } = useTable({
    apiFn: fetchGetCustomerList,
    apiParams: customerSearchParams,
    columns: () => [
      createIndexColumn(t),
      {
        align: 'center',
        dataIndex: 'name',
        key: 'name',
        minWidth: 140,
        title: t('page.customer.list.name')
      },
      {
        align: 'center',
        dataIndex: 'code',
        key: 'code',
        minWidth: 120,
        title: t('page.customer.list.code')
      },
      {
        align: 'center',
        dataIndex: 'companyId',
        key: 'companyId',
        minWidth: 140,
        render: (_, record) => (record.companyId ? companyNameMap.get(record.companyId) : null),
        title: t('page.customer.list.companyId')
      },
      {
        align: 'center',
        dataIndex: 'contactName',
        key: 'contactName',
        minWidth: 100,
        title: t('page.customer.list.contactName')
      },
      {
        align: 'center',
        dataIndex: 'contactPhone',
        key: 'contactPhone',
        title: t('page.customer.list.contactPhone'),
        width: 130
      },
      {
        align: 'center',
        dataIndex: 'status',
        key: 'status',
        render: (_, record) => renderEnableStatus(record.status, t),
        title: t('page.customer.list.status'),
        width: 90
      },
      {
        align: 'center',
        dataIndex: 'createTime',
        key: 'createTime',
        title: t('page.customer.list.createTime'),
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
              onClick={() => nav(`/master/customer/operate/${record.id}`)}
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

  const rowSelection = {
    columnWidth: 48,
    fixed: true,
    onChange: (keys: React.Key[]) => setCheckedRowKeys(keys),
    selectedRowKeys: checkedRowKeys,
    type: 'checkbox' as const
  };

  function handleAdd() {
    nav('/master/customer/operate');
  }

  async function handleBatchDelete() {
    await fetchBatchDeleteCustomer(checkedRowKeys.map(key => key as string));
    window.$message?.success(t('common.deleteSuccess'));
    setCheckedRowKeys([]);
    await run(false);
  }

  async function handleDelete(id: string) {
    await fetchDeleteCustomer(id);
    window.$message?.success(t('common.deleteSuccess'));
    await run(false);
  }

  async function handleToggleStatus(record: Api.Customer.Entity) {
    const nextStatus: Api.Common.EnableStatus = record.status === 1 ? 2 : 1;
    await toggleEntityStatus({
      params: { id: record.id, status: nextStatus },
      refresh: run,
      t,
      toggleFn: fetchToggleCustomerStatus
    });
  }

  return (
    <CrudPageLayout
      search={<CustomerListSearch {...searchProps} />}
      tableWrapperRef={tableWrapperRef}
      title={t('page.customer.list.title')}
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
        <ATable
          rowSelection={rowSelection}
          scroll={scrollConfig}
          size="small"
          {...tableProps}
        />
      }
    />
  );
};

export default CustomerListManage;
