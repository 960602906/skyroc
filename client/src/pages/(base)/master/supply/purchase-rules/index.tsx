import { Suspense, lazy, useMemo } from 'react';

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
  fetchAddPurchaseRule,
  fetchBatchDeletePurchaseRule,
  fetchDeletePurchaseRule,
  fetchGetPurchaseRuleDetail,
  fetchGetPurchaseRuleList,
  fetchTogglePurchaseRuleStatus,
  fetchUpdatePurchaseRule
} from '@/service/api';
import { useGoodsTypeOptions, usePurchaserOptions, useSupplierOptions, useWareOptions } from '@/service/hooks';

import RuleSearch from './modules/RuleSearch';

const RuleOperateDrawer = lazy(() => import('./modules/RuleOperateDrawer'));

const purchasePatternRecord: Record<number, App.I18n.I18nKey> = {
  1: 'page.purchase.rule.purchasePatternDirect',
  2: 'page.purchase.rule.purchasePatternMarket'
};

function resolveOptionLabel(id: string | null, options?: { id: string; name: string }[]) {
  if (!id) {
    return null;
  }

  return options?.find(item => item.id === id)?.name ?? id;
}

const RuleManage = () => {
  const { t } = useTranslation();

  const { data: goodsTypes } = useGoodsTypeOptions();
  const { data: suppliers } = useSupplierOptions();
  const { data: purchasers } = usePurchaserOptions();
  const { data: wares } = useWareOptions();

  const searchParams = useMemo(
    () => ({
      ...createDefaultSearchParams(),
      goodsTypeId: null,
      purchasePattern: null,
      purchaserId: null,
      supplierId: null,
      wareId: null
    }),
    []
  );

  const { columnChecks, data, run, searchProps, setColumnChecks, tableProps, tableWrapperRef } = useTable({
    apiFn: fetchGetPurchaseRuleList,
    apiParams: searchParams,
    columns: () => [
      createIndexColumn(t),
      {
        align: 'center',
        dataIndex: 'name',
        key: 'name',
        title: t('page.purchase.rule.name'),
        width: 240
      },
      {
        align: 'center',
        dataIndex: 'code',
        key: 'code',
        minWidth: 120,
        title: t('page.purchase.rule.code')
      },
      {
        align: 'center',
        dataIndex: 'purchasePattern',
        key: 'purchasePattern',
        render: (_, record) => {
          const key = purchasePatternRecord[record.purchasePattern];
          return key ? t(key) : record.purchasePattern;
        },
        title: t('page.purchase.rule.purchasePattern'),
        width: 120
      },
      {
        align: 'center',
        dataIndex: 'goodsTypeId',
        ellipsis: true,
        key: 'goodsTypeId',
        minWidth: 120,
        render: (_, record) => resolveOptionLabel(record.goodsTypeId, goodsTypes),
        title: t('page.purchase.rule.goodsTypeId')
      },
      {
        align: 'center',
        dataIndex: 'supplierId',
        ellipsis: true,
        key: 'supplierId',
        minWidth: 120,
        render: (_, record) => resolveOptionLabel(record.supplierId, suppliers),
        title: t('page.purchase.rule.supplierId')
      },
      {
        align: 'center',
        dataIndex: 'purchaserId',
        ellipsis: true,
        key: 'purchaserId',
        minWidth: 120,
        render: (_, record) => resolveOptionLabel(record.purchaserId, purchasers),
        title: t('page.purchase.rule.purchaserId')
      },
      {
        align: 'center',
        dataIndex: 'wareId',
        ellipsis: true,
        key: 'wareId',
        minWidth: 120,
        render: (_, record) => resolveOptionLabel(record.wareId, wares),
        title: t('page.purchase.rule.wareId')
      },
      {
        align: 'center',
        dataIndex: 'status',
        key: 'status',
        render: (_, record) => renderEnableStatus(record.status),
        title: t('page.purchase.rule.status'),
        width: 90
      },
      {
        align: 'center',
        dataIndex: 'createTime',
        key: 'createTime',
        title: t('page.purchase.rule.createTime'),
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
        await fetchAddPurchaseRule(res);
      } else {
        await fetchUpdatePurchaseRule(res);
      }
    });

  async function handleBatchDelete() {
    await fetchBatchDeletePurchaseRule(checkedRowKeys.map(key => key as string));
    onBatchDeleted();
  }

  async function handleDelete(id: string) {
    await fetchDeletePurchaseRule(id);
    onDeleted();
  }

  async function edit(id: string) {
    const detail = await fetchGetPurchaseRuleDetail(id);
    handleEdit({ ...(detail ?? {}), index: 0 });
  }

  async function handleToggleStatus(record: Api.PurchaseRule.Entity) {
    const nextStatus: Api.Common.EnableStatus = record.status === 1 ? 2 : 1;
    await toggleEntityStatus({
      params: { id: record.id, status: nextStatus },
      refresh: run,
      t,
      toggleFn: fetchTogglePurchaseRuleStatus
    });
  }

  return (
    <CrudPageLayout
      search={<RuleSearch {...searchProps} />}
      tableWrapperRef={tableWrapperRef}
      title={t('page.purchase.rule.title')}
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
            <RuleOperateDrawer {...generalPopupOperation} />
          </Suspense>
        </>
      }
    />
  );
};

export default RuleManage;
