import {
  CrudPageLayout,
  createDefaultPagination,
  createDefaultSearchParams,
  createIndexColumn,
  renderBooleanTag,
  renderEnableStatus,
  toggleEntityStatus
} from '@/features/crud';
import { TableHeaderOperation, useTable, useTableScroll } from '@/features/table';
import { fetchBatchDeleteGoods, fetchDeleteGoods, fetchGetGoodsList, fetchToggleGoodsStatus } from '@/service/api';

import GoodsSearch from './modules/GoodsSearch';

const GoodsList = () => {
  const { t } = useTranslation();
  const nav = useNavigate();
  const { scrollConfig, tableWrapperRef } = useTableScroll();

  const { columnChecks, run, searchProps, setColumnChecks, tableProps } = useTable({
    apiFn: fetchGetGoodsList,
    apiParams: {
      ...createDefaultSearchParams(),
      defaultSupplierId: null,
      defaultWareId: null,
      goodsTypeId: null,
      isOnSale: null
    },
    columns: () => [
      createIndexColumn(t),
      {
        align: 'center',
        dataIndex: 'name',
        key: 'name',
        minWidth: 140,
        title: t('page.goods.list.name')
      },
      {
        align: 'center',
        dataIndex: 'code',
        key: 'code',
        minWidth: 120,
        title: t('page.goods.list.code')
      },
      {
        align: 'center',
        dataIndex: 'spec',
        key: 'spec',
        minWidth: 100,
        title: t('page.goods.list.spec')
      },
      {
        align: 'center',
        dataIndex: 'brand',
        key: 'brand',
        minWidth: 100,
        title: t('page.goods.list.brand')
      },
      {
        align: 'center',
        dataIndex: 'isOnSale',
        key: 'isOnSale',
        render: (_, record) =>
          renderBooleanTag(record.isOnSale, t('page.goods.list.onSale'), t('page.goods.list.offSale')),
        title: t('page.goods.list.isOnSale'),
        width: 100
      },
      {
        align: 'center',
        dataIndex: 'status',
        key: 'status',
        render: (_, record) => renderEnableStatus(record.status),
        title: t('page.goods.list.status'),
        width: 90
      },
      {
        align: 'center',
        dataIndex: 'createTime',
        key: 'createTime',
        title: t('page.goods.list.createTime'),
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
              onClick={() => nav(`/master/goods/operate/${record.id}`)}
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

  const [checkedRowKeys, setCheckedRowKeys] = useState<React.Key[]>([]);

  const rowSelection = {
    columnWidth: 48,
    fixed: true as const,
    onChange: (keys: React.Key[]) => setCheckedRowKeys(keys),
    selectedRowKeys: checkedRowKeys,
    type: 'checkbox' as const
  };

  async function handleBatchDelete() {
    await fetchBatchDeleteGoods(checkedRowKeys.map(key => key as string));
    window.$message?.success(t('common.deleteSuccess'));
    setCheckedRowKeys([]);
    await run(false);
  }

  async function handleDelete(id: string) {
    await fetchDeleteGoods(id);
    window.$message?.success(t('common.deleteSuccess'));
    await run(false);
  }

  function handleToggleStatus(record: Api.Goods.Entity) {
    const nextStatus: Api.Common.EnableStatus = record.status === 1 ? 2 : 1;
    return toggleEntityStatus({
      params: { id: record.id, status: nextStatus },
      refresh: run,
      t,
      toggleFn: fetchToggleGoodsStatus
    });
  }

  return (
    <CrudPageLayout
      search={<GoodsSearch {...searchProps} />}
      tableWrapperRef={tableWrapperRef}
      title={t('page.goods.list.title')}
      extra={
        <TableHeaderOperation
          add={() => nav('/master/goods/operate')}
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

export default GoodsList;
