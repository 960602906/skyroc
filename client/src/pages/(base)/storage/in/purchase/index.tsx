import dayjs from 'dayjs';
import { Suspense, lazy } from 'react';

import {
  CrudPageLayout,
  createDefaultPagination,
  createIndexColumn,
  displayDateTime,
  renderPurchasePattern,
  renderStockDocumentStatus
} from '@/features/crud';
import { TableHeaderOperation, useTable, useTableOperate } from '@/features/table';
import {
  fetchAddStockInPurchase,
  fetchAuditStockInPurchase,
  fetchDeleteStockInPurchase,
  fetchGetStockInPurchaseDetail,
  fetchGetStockInPurchaseList,
  fetchReverseAuditStockInPurchase,
  fetchUpdateStockInPurchase
} from '@/service/api';
import { StockDocumentStatus } from '@/service/enums';

import type { PurchaseStockInDetailFormValue, PurchaseStockInFormValue } from './modules/PurchaseStockInOperateDrawer';
import PurchaseStockInSearch from './modules/PurchaseStockInSearch';

const PurchaseStockInOperateDrawer = lazy(() => import('./modules/PurchaseStockInOperateDrawer'));

/** 采购入库分页、草稿维护、审核/反审核页面 */
const PurchaseStockInList = () => {
  const { t } = useTranslation();
  const nav = useNavigate();

  const searchParams = {
    businessStatus: null,
    current: 1,
    goodsId: null,
    inTimeEnd: null,
    inTimeStart: null,
    keyword: null,
    size: 10,
    supplierId: null,
    wareId: null
  };

  const { columnChecks, data, run, searchProps, setColumnChecks, tableProps } = useTable({
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
                  {t('page.storage.in.purchase.reverseAudit')}
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

  const { generalPopupOperation, handleAdd, handleEdit, onDeleted } = useTableOperate(data, run, async (res, type) => {
    const values = res as unknown as PurchaseStockInFormValue;
    const payload = {
      departmentId: values.departmentId || null,
      details: values.details.map(detail => toDetailPayload(detail, type === 'edit')),
      expectedArrivalTime: values.expectedArrivalTime ? dayjs(values.expectedArrivalTime).toISOString() : null,
      inTime: values.inTime ? dayjs(values.inTime).toISOString() : '',
      purchaseOrderId: values.purchaseOrderId || null,
      purchasePattern: values.purchasePattern,
      purchaserId: values.purchaserId || null,
      remark: values.remark || null,
      supplierId: values.supplierId || null,
      wareId: values.wareId
    };

    if (type === 'add') {
      await fetchAddStockInPurchase(payload);
    } else {
      await fetchUpdateStockInPurchase({ ...payload, id: values.id! });
    }
  });

  /** 编辑态把既有明细行的 id 透传给后端，便于识别既有商品行 */
  function toDetailPayload(detail: PurchaseStockInDetailFormValue, isEdit: boolean) {
    const base = {
      batchNo: detail.batchNo,
      expireDate: detail.expireDate ? dayjs(detail.expireDate).format('YYYY-MM-DD') : null,
      goodsId: detail.goodsId,
      goodsUnitId: detail.goodsUnitId,
      productDate: detail.productDate ? dayjs(detail.productDate).format('YYYY-MM-DD') : null,
      purchaseOrderDetailId: detail.purchaseOrderDetailId || null,
      quantity: detail.quantity,
      remark: detail.remark || null,
      unitPrice: detail.unitPrice
    };
    return isEdit && detail.id ? { id: detail.id, ...base } : base;
  }

  async function edit(id: string) {
    const detail = await fetchGetStockInPurchaseDetail(id);
    // 日期字段需转换为 Dayjs 供表单控件回显
    const formValue = {
      ...detail,
      details: detail.details.map(item => ({
        ...item,
        expireDate: item.expireDate ? dayjs(item.expireDate) : undefined,
        id: item.id,
        productDate: item.productDate ? dayjs(item.productDate) : undefined
      })),
      expectedArrivalTime: detail.expectedArrivalTime ? dayjs(detail.expectedArrivalTime) : undefined,
      id: detail.id,
      index: 0,
      inTime: detail.inTime ? dayjs(detail.inTime) : undefined
    };
    handleEdit(formValue as unknown as AntDesign.TableDataWithIndex<Api.StockIn.Entity>);
  }

  async function handleDelete(id: string) {
    await fetchDeleteStockInPurchase(id);
    onDeleted();
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
      title={t('page.storage.in.purchase.title')}
      extra={
        <TableHeaderOperation
          disabledDelete
          add={handleAdd}
          columns={columnChecks}
          loading={tableProps.loading}
          refresh={() => run(false)}
          setColumnChecks={setColumnChecks}
          onDelete={() => undefined}
        />
      }
      table={
        <>
          <ATable
            {...tableProps}
            className="mt-16px"
            size="small"
          />
          <Suspense fallback={null}>
            <PurchaseStockInOperateDrawer {...generalPopupOperation} />
          </Suspense>
        </>
      }
    />
  );
};

export default PurchaseStockInList;
