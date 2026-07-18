import { Button, Input, Modal } from 'antd';
import dayjs from 'dayjs';

import {
  CrudPageLayout,
  createDefaultPagination,
  createIndexColumn,
  renderBooleanTag,
  renderOrderOutStorageStatus,
  renderOrderPrintStatus,
  renderOrderReturnStatus,
  renderSaleOrderStatus
} from '@/features/crud';
import { TableHeaderOperation, useTable } from '@/features/table';
import {
  fetchApproveOrder,
  fetchDeleteOrder,
  fetchGetOrderList,
  fetchRejectOrder,
  fetchResubmitOrder
} from '@/service/api';
import { OrderDateType, SaleOrderStatus } from '@/service/enums';

import OrderSearch from './modules/OrderSearch';

/** 后端 FixedDateTime 支持 yyyy-MM-dd / yyyy-MM-dd HH:mm:ss */
function formatDateValue(value: unknown) {
  if (!value) return null;
  if (dayjs.isDayjs(value)) {
    return value.format('YYYY-MM-DD');
  }
  const text = String(value).trim();
  if (!text) return null;
  const parsed = dayjs(text);
  return parsed.isValid() ? parsed.format('YYYY-MM-DD') : text;
}

function formatMoney(value: number | null | undefined) {
  if (value === null || value === undefined || Number.isNaN(Number(value))) {
    return '-';
  }
  return Number(value).toFixed(2);
}

const orderSearchParams = {
  current: 1,
  customerId: null,
  customerTagIds: null,
  dateEnd: null,
  dateRange: null,
  dateStart: null,
  dateType: OrderDateType.ORDER_DATE,
  goodsIds: null,
  goodsKey: null,
  goodsTypeIds: null,
  hasOutSale: null,
  hasPurchasePlan: null,
  keyword: null,
  orderStatus: null,
  returnStatus: null,
  size: 10,
  status: null,
  supplierId: null,
  updateStatus: null
} satisfies Api.Order.SearchParams;

const OrderListManage = () => {
  const { t } = useTranslation();
  const nav = useNavigate();

  const { columnChecks, run, searchProps, setColumnChecks, tableProps, tableWrapperRef } = useTable({
    apiFn: fetchGetOrderList,
    apiParams: orderSearchParams,
    columns: () => [
      createIndexColumn(t),
      {
        align: 'center',
        dataIndex: 'orderNo',
        ellipsis: true,
        fixed: 'left',
        key: 'orderNo',
        render: (orderNo: string, record) => (
          <AButton
            className="h-auto p-0 leading-normal"
            size="small"
            type="link"
            onClick={() => nav(`/orders/detail/${record.id}`)}
          >
            {orderNo}
          </AButton>
        ),
        title: t('page.order.list.orderNo'),
        width: 170
      },
      {
        align: 'center',
        dataIndex: 'customerName',
        ellipsis: true,
        key: 'customerName',
        title: t('page.order.list.customerName'),
        width: 160
      },
      {
        align: 'center',
        dataIndex: 'customerCode',
        ellipsis: true,
        key: 'customerCode',
        title: t('page.order.list.customerCode'),
        width: 120
      },
      {
        align: 'center',
        className: 'whitespace-nowrap',
        dataIndex: 'orderDate',
        ellipsis: true,
        key: 'orderDate',
        title: t('page.order.list.orderDate'),
        width: 170
      },
      {
        align: 'center',
        className: 'whitespace-nowrap',
        dataIndex: 'receiveDate',
        ellipsis: true,
        key: 'receiveDate',
        title: t('page.order.list.receiveDate'),
        width: 170
      },
      {
        align: 'center',
        dataIndex: 'orderStatus',
        key: 'orderStatus',
        render: (_, record) => renderSaleOrderStatus(record.orderStatus),
        title: t('page.order.list.orderStatus'),
        width: 110
      },
      {
        align: 'center',
        dataIndex: 'returnStatus',
        key: 'returnStatus',
        render: (_, record) => renderOrderReturnStatus(record.returnStatus),
        title: t('page.order.list.returnStatus'),
        width: 100
      },
      {
        align: 'center',
        dataIndex: 'printStatus',
        key: 'printStatus',
        render: (_, record) => renderOrderPrintStatus(record.printStatus),
        title: t('page.order.list.printStatus'),
        width: 100
      },
      {
        align: 'center',
        dataIndex: 'outStorageStatus',
        key: 'outStorageStatus',
        render: (_, record) => renderOrderOutStorageStatus(record.outStorageStatus),
        title: t('page.order.list.outStorageStatus'),
        width: 120
      },
      {
        align: 'right',
        dataIndex: 'orderPrice',
        key: 'orderPrice',
        render: (value: number) => formatMoney(value),
        title: t('page.order.list.orderPrice'),
        width: 110
      },
      {
        align: 'right',
        dataIndex: 'settlementPrice',
        key: 'settlementPrice',
        render: (value: number) => formatMoney(value),
        title: t('page.order.list.settlementPrice'),
        width: 110
      },
      {
        align: 'center',
        dataIndex: 'hasOutSale',
        key: 'hasOutSale',
        render: (value: boolean) => renderBooleanTag(value, t('common.yesOrNo.yes'), t('common.yesOrNo.no')),
        title: t('page.order.list.hasOutSale'),
        width: 110
      },
      {
        align: 'center',
        dataIndex: 'hasPurchasePlan',
        key: 'hasPurchasePlan',
        render: (value: boolean) => renderBooleanTag(value, t('common.yesOrNo.yes'), t('common.yesOrNo.no')),
        title: t('page.order.list.hasPurchasePlan'),
        width: 120
      },
      {
        align: 'center',
        dataIndex: 'updateStatus',
        key: 'updateStatus',
        render: (value: boolean) => renderBooleanTag(value, t('common.yesOrNo.yes'), t('common.yesOrNo.no')),
        title: t('page.order.list.updateStatus'),
        width: 100
      },
      {
        align: 'center',
        dataIndex: 'wareName',
        ellipsis: true,
        key: 'wareName',
        title: t('page.order.list.wareName'),
        width: 120
      },
      {
        align: 'center',
        dataIndex: 'contactName',
        ellipsis: true,
        key: 'contactName',
        title: t('page.order.list.contactName'),
        width: 100
      },
      {
        align: 'center',
        dataIndex: 'contactPhone',
        ellipsis: true,
        key: 'contactPhone',
        title: t('page.order.list.contactPhone'),
        width: 130
      },
      {
        align: 'center',
        className: 'whitespace-nowrap',
        dataIndex: 'createTime',
        ellipsis: true,
        key: 'createTime',
        title: t('page.order.list.createTime'),
        width: 170
      },
      {
        align: 'center',
        fixed: 'right',
        key: 'operate',
        render: (_, record) => {
          const isPendingAudit = record.orderStatus === SaleOrderStatus.PENDING_AUDIT;
          const isRejected = record.orderStatus === SaleOrderStatus.REJECTED;
          return (
            <div className="flex-center flex-wrap gap-8px">
              <AButton
                size="small"
                onClick={() => nav(`/orders/detail/${record.id}`)}
              >
                {t('common.detail')}
              </AButton>
              {(isPendingAudit || isRejected) && (
                <AButton
                  ghost
                  size="small"
                  type="primary"
                  onClick={() => nav(`/orders/edit/${record.id}`)}
                >
                  {t('common.edit')}
                </AButton>
              )}
              {isPendingAudit && (
                <>
                  <AButton
                    size="small"
                    type="primary"
                    onClick={() => handleApprove(record)}
                  >
                    {t('page.order.list.approve')}
                  </AButton>
                  <AButton
                    danger
                    size="small"
                    onClick={() => handleReject(record)}
                  >
                    {t('page.order.list.reject')}
                  </AButton>
                </>
              )}
              {isRejected && (
                <AButton
                  size="small"
                  type="primary"
                  onClick={() => handleResubmit(record)}
                >
                  {t('page.order.list.resubmit')}
                </AButton>
              )}
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
          );
        },
        title: t('common.operate'),
        width: 320
      }
    ],
    pagination: createDefaultPagination(),
    scroll: { x: 'max-content' },
    transformParams: params => {
      const next = { ...params } as Api.Order.SearchParams & {
        dateRange?: [unknown, unknown] | null;
      };
      const range = next.dateRange;

      if (Array.isArray(range) && range.length === 2) {
        const dateStart = formatDateValue(range[0]);
        const dateEnd = formatDateValue(range[1]);
        if (dateStart) next.dateStart = dateStart;
        else delete next.dateStart;
        if (dateEnd) next.dateEnd = dateEnd;
        else delete next.dateEnd;
      } else {
        delete next.dateStart;
        delete next.dateEnd;
      }

      delete next.dateRange;
      return next;
    }
  });

  function handleAdd() {
    nav('/orders/edit');
  }

  async function handleDelete(id: string) {
    await fetchDeleteOrder(id);
    window.$message?.success(t('common.deleteSuccess'));
    await run(false);
  }

  function promptAuditRemark(title: string) {
    return new Promise<string | undefined>((resolve, reject) => {
      let remark = '';

      Modal.confirm({
        content: (
          <Input.TextArea
            allowClear
            autoSize={{ maxRows: 4, minRows: 2 }}
            placeholder={t('page.order.list.form.auditRemark')}
            onChange={event => {
              remark = event.target.value;
            }}
          />
        ),
        onCancel: () => reject(new Error('cancelled')),
        onOk: () => resolve(remark.trim() || undefined),
        title
      });
    });
  }

  async function handleApprove(record: Api.Order.Entity) {
    try {
      const remark = await promptAuditRemark(t('page.order.list.approveConfirm', { orderNo: record.orderNo }));
      await fetchApproveOrder(record.id, { remark });
      window.$message?.success(t('common.updateSuccess'));
      await run(false);
    } catch {
      // 用户取消
    }
  }

  async function handleReject(record: Api.Order.Entity) {
    try {
      const remark = await promptAuditRemark(t('page.order.list.rejectConfirm', { orderNo: record.orderNo }));
      await fetchRejectOrder(record.id, { remark });
      window.$message?.success(t('common.updateSuccess'));
      await run(false);
    } catch {
      // 用户取消
    }
  }

  async function handleResubmit(record: Api.Order.Entity) {
    try {
      const remark = await promptAuditRemark(t('page.order.list.resubmitConfirm', { orderNo: record.orderNo }));
      await fetchResubmitOrder(record.id, { remark });
      window.$message?.success(t('common.updateSuccess'));
      await run(false);
    } catch {
      // 用户取消
    }
  }

  return (
    <CrudPageLayout
      search={<OrderSearch {...searchProps} />}
      tableWrapperRef={tableWrapperRef}
      title={t('page.order.list.title')}
      extra={
        <TableHeaderOperation
          disabledDelete
          add={handleAdd}
          columns={columnChecks}
          loading={tableProps.loading}
          refresh={run}
          setColumnChecks={setColumnChecks}
          onDelete={() => undefined}
        >
          <Button
            ghost
            icon={<IconIcRoundPlus className="text-icon" />}
            size="small"
            type="primary"
            onClick={handleAdd}
          >
            {t('common.add')}
          </Button>
        </TableHeaderOperation>
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

export default OrderListManage;
