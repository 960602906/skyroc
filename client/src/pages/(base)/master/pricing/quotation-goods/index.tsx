import { Suspense, lazy } from 'react';

import { CrudPageLayout, createDefaultPagination, createIndexColumn, renderBooleanTag } from '@/features/crud';
import { parseQuery } from '@/features/router/query';
import { TableHeaderOperation, useTable, useTableOperate } from '@/features/table';
import {
  fetchAddQuotationGoods,
  fetchBatchDeleteQuotationGoods,
  fetchDeleteQuotationGoods,
  fetchGetQuotationGoodsDetail,
  fetchGetQuotationGoodsList,
  fetchUpdateQuotationGoods
} from '@/service/api';

import QuotationGoodsSearch from './modules/QuotationGoodsSearch';

const QuotationGoodsOperateModal = lazy(() => import('./modules/QuotationGoodsOperateModal'));

const defaultSearchParams = {
  current: 1,
  goodsId: null,
  isOnSale: null,
  quotationId: null,
  size: 10
} satisfies Api.QuotationGoods.SearchParams;

const QuotationGoodsManage = () => {
  const { t } = useTranslation();
  const { search } = useLocation();
  const query = parseQuery(search) as { quotationId?: string };
  const quotationIdFromQuery = query.quotationId || null;

  const { columnChecks, data, run, searchProps, setColumnChecks, tableProps, tableWrapperRef } = useTable({
    apiFn: fetchGetQuotationGoodsList,
    apiParams: defaultSearchParams,
    columns: () => [
      createIndexColumn(t),
      {
        align: 'center',
        dataIndex: 'quotationId',
        key: 'quotationId',
        minWidth: 120,
        title: t('page.goods.quotationGoods.quotationId')
      },
      {
        align: 'center',
        dataIndex: 'goodsId',
        key: 'goodsId',
        minWidth: 120,
        title: t('page.goods.quotationGoods.goodsId')
      },
      {
        align: 'center',
        dataIndex: 'goodsUnitId',
        key: 'goodsUnitId',
        minWidth: 120,
        title: t('page.goods.quotationGoods.goodsUnitId')
      },
      {
        align: 'center',
        dataIndex: 'unitPrice',
        key: 'unitPrice',
        title: t('page.goods.quotationGoods.unitPrice'),
        width: 100
      },
      {
        align: 'center',
        dataIndex: 'minOrderQuantity',
        key: 'minOrderQuantity',
        title: t('page.goods.quotationGoods.minOrderQuantity'),
        width: 100
      },
      {
        align: 'center',
        dataIndex: 'isOnSale',
        key: 'isOnSale',
        render: (_, record) =>
          renderBooleanTag(record.isOnSale, t('page.goods.list.onSale'), t('page.goods.list.offSale')),
        title: t('page.goods.quotationGoods.isOnSale'),
        width: 100
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
        width: 150
      }
    ],
    pagination: createDefaultPagination()
  });

  const { checkedRowKeys, generalPopupOperation, handleAdd, handleEdit, onBatchDeleted, onDeleted, rowSelection } =
    useTableOperate(data, run, async (res, type) => {
      if (type === 'add') {
        await fetchAddQuotationGoods({ ...res, isOnSale: res.isOnSale ?? true });
      } else {
        await fetchUpdateQuotationGoods(res);
      }
    });

  function handleAddGoods() {
    handleAdd();
    generalPopupOperation.form.setFieldsValue({
      isOnSale: true,
      quotationId: quotationIdFromQuery ?? undefined
    } as never);
  }

  async function handleBatchDelete() {
    await fetchBatchDeleteQuotationGoods(checkedRowKeys.map(key => key as string));
    onBatchDeleted();
  }

  async function handleDelete(id: string) {
    await fetchDeleteQuotationGoods(id);
    onDeleted();
  }

  async function edit(id: string) {
    const detail = await fetchGetQuotationGoodsDetail(id);
    handleEdit({ ...(detail ?? {}), index: 0 });
  }

  return (
    <CrudPageLayout
      search={<QuotationGoodsSearch {...searchProps} />}
      tableWrapperRef={tableWrapperRef}
      title={t('page.goods.quotationGoods.title')}
      extra={
        <TableHeaderOperation
          add={handleAddGoods}
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
            <QuotationGoodsOperateModal {...generalPopupOperation} />
          </Suspense>
        </>
      }
    />
  );
};

export default QuotationGoodsManage;
