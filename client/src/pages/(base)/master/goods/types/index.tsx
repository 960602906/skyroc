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
  fetchAddGoodsType,
  fetchBatchDeleteGoodsType,
  fetchDeleteGoodsType,
  fetchGetGoodsTypeDetail,
  fetchGetGoodsTypeList,
  fetchToggleGoodsTypeStatus,
  fetchUpdateGoodsType
} from '@/service/api';

import GoodsTypeSearch from './modules/GoodsTypeSearch';

const GoodsTypeOperateModal = lazy(() => import('./modules/GoodsTypeOperateModal'));

const GoodsTypeManage = () => {
  const { t } = useTranslation();
  const { scrollConfig, tableWrapperRef } = useTableScroll();

  const { columnChecks, data, run, searchProps, setColumnChecks, tableProps } = useTable({
    apiFn: fetchGetGoodsTypeList,
    apiParams: {
      ...createDefaultSearchParams(),
      parentId: null,
      taxCategoryCode: null
    },
    columns: () => [
      createIndexColumn(t),
      {
        align: 'center',
        dataIndex: 'name',
        key: 'name',
        minWidth: 140,
        title: t('page.goods.type.name')
      },
      {
        align: 'center',
        dataIndex: 'code',
        key: 'code',
        minWidth: 120,
        title: t('page.goods.type.code')
      },
      {
        align: 'center',
        dataIndex: 'sort',
        key: 'sort',
        title: t('page.goods.type.sort'),
        width: 80
      },
      {
        align: 'center',
        dataIndex: 'defaultTaxRate',
        key: 'defaultTaxRate',
        title: t('page.goods.type.defaultTaxRate'),
        width: 100
      },
      {
        align: 'center',
        dataIndex: 'isTaxExempt',
        key: 'isTaxExempt',
        render: (_, record) => renderBooleanTag(record.isTaxExempt, t('common.yesOrNo.yes'), t('common.yesOrNo.no')),
        title: t('page.goods.type.isTaxExempt'),
        width: 100
      },
      {
        align: 'center',
        dataIndex: 'status',
        key: 'status',
        render: (_, record) => renderEnableStatus(record.status, t),
        title: t('page.goods.type.status'),
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
        await fetchAddGoodsType(res);
      } else {
        await fetchUpdateGoodsType(res);
      }
    });

  async function handleBatchDelete() {
    await fetchBatchDeleteGoodsType(checkedRowKeys.map(key => key as string));
    onBatchDeleted();
  }

  async function handleDelete(id: string) {
    await fetchDeleteGoodsType(id);
    onDeleted();
  }

  async function edit(id: string) {
    const detail = await fetchGetGoodsTypeDetail(id);
    handleEdit({ ...(detail ?? {}), index: 0 });
  }

  function handleToggleStatus(record: Api.GoodsType.Entity) {
    const nextStatus: Api.Common.EnableStatus = record.status === 1 ? 2 : 1;
    return toggleEntityStatus({
      params: { id: record.id, status: nextStatus },
      refresh: run,
      t,
      toggleFn: fetchToggleGoodsTypeStatus
    });
  }

  return (
    <CrudPageLayout
      search={<GoodsTypeSearch {...searchProps} />}
      tableWrapperRef={tableWrapperRef}
      title={t('page.goods.type.title')}
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
            <GoodsTypeOperateModal {...generalPopupOperation} />
          </Suspense>
        </>
      }
    />
  );
};

export default GoodsTypeManage;
