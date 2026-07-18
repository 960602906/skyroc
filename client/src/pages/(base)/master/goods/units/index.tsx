import { Suspense, lazy } from 'react';

import {
  CrudPageLayout,
  createDefaultPagination,
  createIndexColumn,
  renderBooleanTag,
  renderEnableStatus,
  toggleEntityStatus
} from '@/features/crud';
import { TableHeaderOperation, useTable, useTableOperate, useTableScroll } from '@/features/table';
import {
  fetchAddGoodsUnit,
  fetchBatchDeleteGoodsUnit,
  fetchDeleteGoodsUnit,
  fetchGetGoodsUnitDetail,
  fetchGetGoodsUnitList,
  fetchToggleGoodsUnitStatus,
  fetchUpdateGoodsUnit
} from '@/service/api';

import GoodsUnitSearch from './modules/GoodsUnitSearch';

const GoodsUnitOperateDrawer = lazy(() => import('./modules/GoodsUnitOperateDrawer'));

const defaultSearchParams = {
  current: 1,
  goodsId: null,
  name: null,
  size: 10,
  status: null
} satisfies Api.GoodsUnit.SearchParams;

const GoodsUnitManage = () => {
  const { t } = useTranslation();
  const nav = useNavigate();
  const { scrollConfig, tableWrapperRef } = useTableScroll();

  const { columnChecks, data, run, searchProps, setColumnChecks, tableProps } = useTable({
    apiFn: fetchGetGoodsUnitList,
    apiParams: defaultSearchParams,
    columns: () => [
      createIndexColumn(t),
      {
        align: 'center',
        dataIndex: 'goodsName',
        key: 'goodsName',
        minWidth: 140,
        render: (_, record) => {
          const label = record.goodsName || record.goodsCode || record.goodsId;
          if (!record.goodsId) {
            return label;
          }
          return (
            <AButton
              className="h-auto p-0 leading-normal"
              size="small"
              type="link"
              onClick={() => nav(`/master/goods/detail/${record.goodsId}`)}
            >
              {label}
            </AButton>
          );
        },
        title: t('page.goods.unit.goodsId')
      },
      {
        align: 'center',
        dataIndex: 'goodsCode',
        key: 'goodsCode',
        minWidth: 120,
        title: t('page.goods.unit.goodsCode')
      },
      {
        align: 'center',
        dataIndex: 'name',
        key: 'name',
        minWidth: 120,
        render: (name: string, record) => (
          <AButton
            className="h-auto p-0 leading-normal"
            size="small"
            type="link"
            onClick={() => nav(`/master/goods/units/detail/${record.id}`)}
          >
            {name}
          </AButton>
        ),
        title: t('page.goods.unit.name')
      },
      {
        align: 'center',
        dataIndex: 'code',
        key: 'code',
        minWidth: 100,
        title: t('page.goods.unit.code')
      },
      {
        align: 'center',
        dataIndex: 'conversionRate',
        key: 'conversionRate',
        title: t('page.goods.unit.conversionRate'),
        width: 100
      },
      {
        align: 'center',
        dataIndex: 'isBaseUnit',
        key: 'isBaseUnit',
        render: (_, record) => renderBooleanTag(record.isBaseUnit, t('common.yesOrNo.yes'), t('common.yesOrNo.no')),
        title: t('page.goods.unit.isBaseUnit'),
        width: 100
      },
      {
        align: 'center',
        dataIndex: 'sort',
        key: 'sort',
        title: t('page.goods.unit.sort'),
        width: 80
      },
      {
        align: 'center',
        dataIndex: 'status',
        key: 'status',
        render: (_, record) => renderEnableStatus(record.status),
        title: t('page.goods.unit.status'),
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
        await fetchAddGoodsUnit(res);
      } else {
        await fetchUpdateGoodsUnit(res);
      }
    });

  async function handleBatchDelete() {
    await fetchBatchDeleteGoodsUnit(checkedRowKeys.map(key => key as string));
    onBatchDeleted();
  }

  async function handleDelete(id: string) {
    await fetchDeleteGoodsUnit(id);
    onDeleted();
  }

  async function edit(id: string) {
    const detail = await fetchGetGoodsUnitDetail(id);
    handleEdit({ ...(detail ?? {}), index: 0 });
  }

  function handleToggleStatus(record: Api.GoodsUnit.Entity) {
    const nextStatus: Api.Common.EnableStatus = record.status === 1 ? 2 : 1;
    return toggleEntityStatus({
      params: { id: record.id, status: nextStatus },
      refresh: run,
      t,
      toggleFn: fetchToggleGoodsUnitStatus
    });
  }

  return (
    <CrudPageLayout
      search={<GoodsUnitSearch {...searchProps} />}
      tableWrapperRef={tableWrapperRef}
      title={t('page.goods.unit.title')}
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
            <GoodsUnitOperateDrawer {...generalPopupOperation} />
          </Suspense>
        </>
      }
    />
  );
};

export default GoodsUnitManage;
