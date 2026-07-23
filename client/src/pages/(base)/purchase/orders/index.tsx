import {
  CrudPageLayout,
  createDefaultPagination,
  createIndexColumn,
  displayDateTime,
  renderPurchaseOrderStatus,
  renderPurchasePattern
} from '@/features/crud';
import { TableHeaderOperation, useTable } from '@/features/table';
import {
  fetchCancelPurchaseOrder,
  fetchCompletePurchaseOrder,
  fetchDeletePurchaseOrder,
  fetchGetPurchaseOrderList
} from '@/service/api';
import { PurchaseOrderStatus } from '@/service/enums';

import PurchaseOrderSearch from './modules/PurchaseOrderSearch';

/** 采购单分页、草稿维护、完成与取消页面。 */
const PurchaseOrderList = () => {
  const { t } = useTranslation();
  const nav = useNavigate();

  const searchParams = {
    businessStatus: null,
    current: 1,
    goodsId: null,
    keyword: null,
    purchasePattern: null,
    purchaserId: null,
    receiveTimeEnd: null,
    receiveTimeStart: null,
    size: 10,
    supplierId: null
  };

  const { columnChecks, run, searchProps, setColumnChecks, tableProps, tableWrapperRef } = useTable({
    apiFn: fetchGetPurchaseOrderList,
    apiParams: searchParams,
    columns: () => [
      createIndexColumn(t),
      {
        align: 'center',
        dataIndex: 'purchaseNo',
        fixed: 'left',
        key: 'purchaseNo',
        render: (value: string, record) => (
          <AButton
            className="p-0"
            type="link"
            onClick={() => nav(`/purchase/orders/detail/${record.id}`)}
          >
            {value}
          </AButton>
        ),
        title: t('page.purchase.order.purchaseNo'),
        width: 170
      },
      {
        align: 'center',
        dataIndex: 'purchasePattern',
        key: 'purchasePattern',
        render: renderPurchasePattern,
        title: t('page.purchase.order.purchasePattern'),
        width: 130
      },
      {
        align: 'center',
        dataIndex: 'supplierName',
        key: 'supplierName',
        title: t('page.purchase.order.supplier'),
        width: 150
      },
      {
        align: 'center',
        dataIndex: 'purchaserName',
        key: 'purchaserName',
        title: t('page.purchase.order.purchaser'),
        width: 130
      },
      {
        align: 'center',
        dataIndex: 'receiveTime',
        key: 'receiveTime',
        render: displayDateTime,
        title: t('page.purchase.order.receiveTime'),
        width: 180
      },
      {
        align: 'center',
        dataIndex: 'businessStatus',
        key: 'businessStatus',
        render: renderPurchaseOrderStatus,
        title: t('page.purchase.order.businessStatus'),
        width: 120
      },
      {
        align: 'center',
        dataIndex: 'details',
        key: 'details',
        render: (details: Api.PurchaseOrder.Detail[]) => details.map(item => item.goodsName).join('、'),
        title: t('page.purchase.order.goods'),
        width: 220
      },
      {
        align: 'center',
        fixed: 'right',
        key: 'operate',
        render: (_, record) => (
          <div className="flex-center flex-wrap gap-8px">
            {record.businessStatus === PurchaseOrderStatus.DRAFT && (
              <>
                <AButton
                  ghost
                  size="small"
                  type="primary"
                  onClick={() => nav(`/purchase/orders/operate/${record.id}`)}
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
                <APopconfirm
                  title={t('page.purchase.order.complete')}
                  onConfirm={() => handleComplete(record.id)}
                >
                  <AButton
                    size="small"
                    type="primary"
                  >
                    {t('page.purchase.order.complete')}
                  </AButton>
                </APopconfirm>
                <APopconfirm
                  title={t('page.purchase.order.cancel')}
                  onConfirm={() => handleCancel(record.id)}
                >
                  <AButton
                    danger
                    size="small"
                  >
                    {t('page.purchase.order.cancel')}
                  </AButton>
                </APopconfirm>
              </>
            )}
          </div>
        ),
        title: t('common.operate'),
        width: 320
      }
    ],
    pagination: createDefaultPagination(),
    scroll: { x: 'max-content' },
    transformParams: params => {
      const next = { ...params } as Api.PurchaseOrder.SearchParams;
      if (next.businessStatus === null || next.businessStatus === undefined) {
        delete next.businessStatus;
      }
      return next;
    }
  });

  async function handleDelete(id: string) {
    await fetchDeletePurchaseOrder(id);
    run(false);
  }

  async function handleComplete(id: string) {
    await fetchCompletePurchaseOrder(id);
    window.$message?.success(t('common.updateSuccess'));
    await run(false);
  }

  async function handleCancel(id: string) {
    await fetchCancelPurchaseOrder(id);
    window.$message?.success(t('common.updateSuccess'));
    await run(false);
  }

  return (
    <CrudPageLayout
      search={<PurchaseOrderSearch {...searchProps} />}
      tableWrapperRef={tableWrapperRef}
      title={t('page.purchase.order.title')}
      extra={
        <TableHeaderOperation
          disabledDelete
          add={() => nav('/purchase/orders/operate')}
          columns={columnChecks}
          loading={tableProps.loading}
          refresh={run}
          setColumnChecks={setColumnChecks}
          onDelete={() => undefined}
        />
      }
      table={
        <ATable
          size="small"
          {...tableProps}
        />
      }
    />
  );
};

export default PurchaseOrderList;
