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
  fetchAddCustomerProtocolGoods,
  fetchBatchDeleteCustomerProtocolGoods,
  fetchDeleteCustomerProtocolGoods,
  fetchGetCustomerProtocolGoodsDetail,
  fetchGetCustomerProtocolGoodsList,
  fetchToggleCustomerProtocolGoodsStatus,
  fetchUpdateCustomerProtocolGoods
} from '@/service/api';

import ProtocolGoodsSearch from './modules/ProtocolGoodsSearch';

const ProtocolGoodsOperateModal = lazy(() => import('./modules/ProtocolGoodsOperateModal'));

const defaultSearchParams: Api.CustomerProtocolGoods.SearchParams = {
  current: 1,
  customerProtocolId: null,
  goodsId: null,
  size: 10,
  status: null
};

const ProtocolGoodsManage = () => {
  const { t } = useTranslation();

  const { scrollConfig, tableWrapperRef } = useTableScroll();

  const { columnChecks, data, run, searchProps, setColumnChecks, tableProps } = useTable({
    apiFn: fetchGetCustomerProtocolGoodsList,
    apiParams: defaultSearchParams,
    columns: () => [
      createIndexColumn(t),
      {
        align: 'center',
        dataIndex: 'customerProtocolId',
        key: 'customerProtocolId',
        minWidth: 140,
        title: t('page.customer.protocolGoods.customerProtocolId')
      },
      {
        align: 'center',
        dataIndex: 'goodsId',
        key: 'goodsId',
        minWidth: 120,
        title: t('page.customer.protocolGoods.goodsId')
      },
      {
        align: 'center',
        dataIndex: 'goodsUnitId',
        key: 'goodsUnitId',
        minWidth: 120,
        title: t('page.customer.protocolGoods.goodsUnitId')
      },
      {
        align: 'center',
        dataIndex: 'protocolPrice',
        key: 'protocolPrice',
        title: t('page.customer.protocolGoods.protocolPrice'),
        width: 110
      },
      {
        align: 'center',
        dataIndex: 'minOrderQuantity',
        key: 'minOrderQuantity',
        title: t('page.customer.protocolGoods.minOrderQuantity'),
        width: 110
      },
      {
        align: 'center',
        dataIndex: 'status',
        key: 'status',
        render: (_, record) => renderEnableStatus(record.status, t),
        title: t('page.customer.protocolGoods.status'),
        width: 90
      },
      {
        align: 'center',
        dataIndex: 'createTime',
        key: 'createTime',
        title: t('page.customer.protocolGoods.createTime'),
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
        await fetchAddCustomerProtocolGoods(res);
      } else {
        await fetchUpdateCustomerProtocolGoods(res);
      }
    });

  async function handleBatchDelete() {
    await fetchBatchDeleteCustomerProtocolGoods(checkedRowKeys.map(key => key as string));
    onBatchDeleted();
  }

  async function handleDelete(id: string) {
    await fetchDeleteCustomerProtocolGoods(id);
    onDeleted();
  }

  async function edit(id: string) {
    const detail = await fetchGetCustomerProtocolGoodsDetail(id);
    handleEdit({ ...(detail ?? {}), index: 0 });
  }

  async function handleToggleStatus(record: Api.CustomerProtocolGoods.Entity) {
    const nextStatus: Api.Common.EnableStatus = record.status === 1 ? 2 : 1;
    await toggleEntityStatus({
      params: { id: record.id, status: nextStatus },
      refresh: run,
      t,
      toggleFn: fetchToggleCustomerProtocolGoodsStatus
    });
  }

  return (
    <CrudPageLayout
      search={<ProtocolGoodsSearch {...searchProps} />}
      tableWrapperRef={tableWrapperRef}
      title={t('page.customer.protocolGoods.title')}
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
            <ProtocolGoodsOperateModal {...generalPopupOperation} />
          </Suspense>
        </>
      }
    />
  );
};

export default ProtocolGoodsManage;
