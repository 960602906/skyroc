import {
  CrudPageLayout,
  createDefaultPagination,
  createDefaultSearchParams,
  createIndexColumn,
  displayDateTime,
  renderPurchasePattern,
  renderStockDocumentStatus
} from '@/features/crud';
import { TableHeaderOperation, useTable } from '@/features/table';
import {
  fetchAuditStockInPurchase,
  fetchDeleteStockInPurchase,
  fetchGetStockInPurchaseList,
  fetchReverseAuditStockInPurchase
} from '@/service/api';
import { StockDocumentStatus } from '@/service/enums';

import PurchaseStockInSearch from './modules/PurchaseStockInSearch';

/** 采购入库分页、草稿维护、审核/反审核页面 */
const PurchaseStockInList = () => {
  const { t } = useTranslation();
  const nav = useNavigate();

  const searchParams = {
    ...createDefaultSearchParams(),
    businessStatus: null,
    goodsId: null,
    inTimeEnd: null,
    inTimeStart: null,
    keyword: null,
    supplierId: null,
    wareId: null
  };

  const { columnChecks, run, searchProps, setColumnChecks, tableProps, tableWrapperRef } = useTable({
    apiFn: fetchGetStockInPurchaseList,
    apiParams: searchParams,
    columns: () => [
      createIndexColumn(t),
      {
        align: 'center',
        dataIndex: 'inNo',
        fixed: 'left',
        key: 'inNo',
        render: (value: string, record) => (
          <AButton
            className="p-0"
            type="link"
            onClick={() => nav(`/storage/in/purchase/detail/${record.id}`)}
          >
            {value}
          </AButton>
        ),
        title: t('page.storage.in.purchase.inNo'),
        width: 170
      },
      {
        align: 'center',
        dataIndex: 'purchasePattern',
        key: 'purchasePattern',
        render: renderPurchasePattern,
        title: t('page.storage.in.purchase.purchasePattern'),
        width: 130
      },
      {
        align: 'center',
        dataIndex: 'wareName',
        key: 'wareName',
        title: t('page.storage.in.purchase.ware'),
        width: 150
      },
      {
        align: 'center',
        dataIndex: 'supplierName',
        key: 'supplierName',
        title: t('page.storage.in.purchase.supplier'),
        width: 150
      },
      {
        align: 'center',
        dataIndex: 'purchaserName',
        key: 'purchaserName',
        title: t('page.storage.in.purchase.purchaser'),
        width: 130
      },
      {
        align: 'center',
        dataIndex: 'inTime',
        key: 'inTime',
        render: displayDateTime,
        title: t('page.storage.in.purchase.inTime'),
        width: 180
      },
      {
        align: 'center',
        dataIndex: 'totalAmount',
        key: 'totalAmount',
        render: (value: number) => value.toFixed(2),
        title: t('page.storage.in.purchase.totalAmount'),
        width: 120
      },
      {
        align: 'center',
        dataIndex: 'businessStatus',
        key: 'businessStatus',
        render: renderStockDocumentStatus,
        title: t('page.storage.in.purchase.businessStatus'),
        width: 120
      },
      {
        align: 'center',
        dataIndex: 'details',
        key: 'details',
        render: (details: Api.StockIn.StockInDetail[] | undefined) => {
          if (!details || details.length === 0) return '-';
          return details.map(item => item.goodsName).join('、');
        },
        title: t('page.storage.in.purchase.goods'),
        width: 220
      },
      {
        align: 'center',
        fixed: 'right',
        key: 'operate',
        render: (_, record) => (
          <div className="flex-center flex-wrap gap-8px">
            {record.businessStatus === StockDocumentStatus.DRAFT && (
              <>
                <AButton
                  ghost
                  size="small"
                  type="primary"
                  onClick={() => nav(`/storage/in/purchase/operate/${record.id}`)}
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
                  title={t('page.storage.in.purchase.audit')}
                  onConfirm={() => handleAudit(record.id)}
                >
                  <AButton
                    size="small"
                    type="primary"
                  >
                    {t('page.storage.in.purchase.audit')}
                  </AButton>
                </APopconfirm>
              </>
            )}
            {record.businessStatus === StockDocumentStatus.AUDITED && (
              <APopconfirm
                title={t('page.storage.in.purchase.reverseAudit')}
                onConfirm={() => handleReverseAudit(record.id)}
              >
                <AButton
                  danger
                  size="small"
                >
                  {t('page.storage.in.purchase.reverseAuditBtn')}
                </AButton>
              </APopconfirm>
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
      const next = { ...params } as Api.StockIn.SearchParams;
      if (next.businessStatus === null || next.businessStatus === undefined) {
        delete next.businessStatus;
      }
      return next;
    }
  });

  async function handleDelete(id: string) {
    await fetchDeleteStockInPurchase(id);
    await run(false);
  }

  async function handleAudit(id: string) {
    await fetchAuditStockInPurchase(id, {});
    window.$message?.success(t('common.updateSuccess'));
    await run(false);
  }

  async function handleReverseAudit(id: string) {
    await fetchReverseAuditStockInPurchase(id, {});
    window.$message?.success(t('common.updateSuccess'));
    await run(false);
  }

  return (
    <CrudPageLayout
      search={<PurchaseStockInSearch {...searchProps} />}
      tableWrapperRef={tableWrapperRef}
      title={t('page.storage.in.purchase.title')}
      extra={
        <TableHeaderOperation
          disabledDelete
          add={() => nav('/storage/in/purchase/operate')}
          columns={columnChecks}
          loading={tableProps.loading}
          refresh={() => run(false)}
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

export default PurchaseStockInList;
