import { Input, Modal } from 'antd';

import {
  CrudPageLayout,
  createDefaultPagination,
  createDefaultSearchParams,
  createIndexColumn,
  displayDateTime,
  formatUtcBoundary,
  renderAfterSaleStatus,
  transformDateRange,
  transformEnumFields
} from '@/features/crud';
import { TableHeaderOperation, useTable } from '@/features/table';
import {
  fetchApproveAfterSale,
  fetchCompleteAfterSale,
  fetchDeleteAfterSale,
  fetchGetAfterSaleList,
  fetchRejectAfterSale,
  fetchResubmitAfterSale,
  fetchReverseAfterSale,
  fetchSubmitAfterSale
} from '@/service/api';
import { AfterSaleAuditAction, AfterSaleStatus } from '@/service/enums';

import AfterSaleSearch from './modules/AfterSaleSearch';

function formatMoney(value: number | null | undefined) {
  if (value === null || value === undefined || Number.isNaN(Number(value))) {
    return '-';
  }
  return Number(value).toFixed(2);
}

function getDistinctValues<T>(items: T[]) {
  return Array.from(new Set(items));
}

const afterSaleSearchParams = {
  ...createDefaultSearchParams(),
  afterSaleType: null,
  afterStatus: null,
  customerId: null,
  dateEnd: null,
  dateRange: null,
  dateStart: null,
  handleType: null,
  keyword: null,
  saleOrderId: null
} satisfies Api.AfterSale.SearchParams;

const AfterSaleList = () => {
  const { t } = useTranslation();
  const nav = useNavigate();

  const { columnChecks, run, searchProps, setColumnChecks, tableProps, tableWrapperRef } = useTable({
    apiFn: fetchGetAfterSaleList,
    apiParams: afterSaleSearchParams,
    columns: () => [
      createIndexColumn(t),
      {
        align: 'center',
        dataIndex: 'afterSaleNo',
        ellipsis: true,
        fixed: 'left',
        key: 'afterSaleNo',
        render: (value: string, record) => (
          <AButton
            className="h-auto p-0 leading-normal"
            size="small"
            type="link"
            onClick={() => nav(`/orders/after-sales/detail/${record.id}`)}
          >
            {value}
          </AButton>
        ),
        title: t('page.afterSale.list.afterSaleNo')
      },
      {
        align: 'center',
        dataIndex: 'saleOrderNo',
        ellipsis: true,
        key: 'saleOrderNo',
        render: (value: string | null, record) =>
          value && record.saleOrderId ? (
            <AButton
              className="h-auto p-0 leading-normal"
              size="small"
              type="link"
              onClick={() => nav(`/orders/detail/${record.saleOrderId}`)}
            >
              {value}
            </AButton>
          ) : (
            value || '-'
          ),
        title: t('page.afterSale.list.saleOrderNo')
      },
      {
        align: 'center',
        dataIndex: 'customerName',
        ellipsis: true,
        key: 'customerName',
        title: t('page.afterSale.list.customerName'),
        width: 160
      },
      {
        align: 'left',
        dataIndex: 'goods',
        render: (_, record) => {
          const handleText: Record<Api.AfterSale.HandleType, App.I18n.I18nKey> = {
            1: 'page.afterSale.list.handleGoodsDiscount',
            2: 'page.afterSale.list.handleReplenishment',
            3: 'page.afterSale.list.handleExchange',
            4: 'page.afterSale.list.handleBillAdjustment',
            5: 'page.afterSale.list.handleCustomerCommunication',
            6: 'page.afterSale.list.handleOther'
          };
          const typeValues = getDistinctValues(record.goods.map(item => item.afterSaleType));
          const handleValues = getDistinctValues(record.goods.map(item => item.handleType));
          const typeSummary = typeValues
            .map(value => t(`page.afterSale.list.type${value === 1 ? 'RefundOnly' : 'ReturnAndRefund'}`))
            .join(' / ');
          const handleSummary = handleValues.map(value => t(handleText[value])).join(' / ');
          return (
            <div className="flex flex-col gap-2px">
              <span>{typeSummary || '-'}</span>
              <span className="text-12px text-[var(--ant-color-text-secondary)]">{handleSummary || '-'}</span>
            </div>
          );
        },
        title: t('page.afterSale.list.requestAndHandle'),
        width: 170
      },
      {
        align: 'right',
        dataIndex: 'orderPrice',
        key: 'orderPrice',
        render: (value: number) => formatMoney(value),
        title: t('page.afterSale.list.orderPrice'),
        width: 110
      },
      {
        align: 'right',
        dataIndex: 'totalRefundAmount',
        key: 'totalRefundAmount',
        render: (value: number) => formatMoney(value),
        title: t('page.afterSale.list.totalRefundAmount'),
        width: 120
      },
      {
        align: 'right',
        dataIndex: 'settlementPrice',
        key: 'settlementPrice',
        render: (value: number) => formatMoney(value),
        title: t('page.afterSale.list.settlementPrice'),
        width: 110
      },
      {
        align: 'center',
        dataIndex: 'afterStatus',
        key: 'afterStatus',
        render: (_, record) => renderAfterSaleStatus(record.afterStatus),
        title: t('page.afterSale.list.afterStatus'),
        width: 110
      },
      {
        align: 'center',
        dataIndex: 'contactName',
        ellipsis: true,
        key: 'contactName',
        render: (value: string | null) => value || '-',
        title: t('page.afterSale.list.contactName'),
        width: 100
      },
      {
        align: 'center',
        dataIndex: 'contactPhone',
        ellipsis: true,
        key: 'contactPhone',
        render: (value: string | null) => value || '-',
        title: t('page.afterSale.list.contactPhone'),
        width: 130
      },
      {
        align: 'center',
        className: 'whitespace-nowrap',
        dataIndex: 'createTime',
        key: 'createTime',
        render: (value: string) => displayDateTime(value),
        title: t('page.afterSale.list.createTime'),
        width: 170
      },
      {
        align: 'center',
        fixed: 'right',
        key: 'operate',
        render: (_, record) => {
          const isDraft = record.afterStatus === AfterSaleStatus.DRAFT;
          const isPendingAudit = record.afterStatus === AfterSaleStatus.PENDING_AUDIT;
          const isPendingHandle =
            record.afterStatus === AfterSaleStatus.RETURN_PENDING ||
            record.afterStatus === AfterSaleStatus.REFUND_PENDING;
          const latestAction = record.latestAuditAction;
          const isRejectedDraft = isDraft && latestAction === AfterSaleAuditAction.REJECT;
          const canDelete = isDraft && latestAction === null;
          const canReverse = isPendingHandle && !record.hasPickupTasks;

          return (
            <div className="flex-center flex-wrap gap-8px">
              {isDraft && (
                <AButton
                  ghost
                  size="small"
                  type="primary"
                  onClick={() => nav(`/orders/after-sales/operate/${record.id}`)}
                >
                  {t('common.edit')}
                </AButton>
              )}
              {isDraft && (
                <AButton
                  size="small"
                  type="primary"
                  onClick={() => handleAction(record, isRejectedDraft ? 'resubmit' : 'submit')}
                >
                  {t(isRejectedDraft ? 'page.afterSale.list.resubmit' : 'page.afterSale.list.submit')}
                </AButton>
              )}
              {isPendingAudit && (
                <>
                  <AButton
                    size="small"
                    type="primary"
                    onClick={() => handleAction(record, 'approve')}
                  >
                    {t('page.afterSale.list.approve')}
                  </AButton>
                  <AButton
                    danger
                    size="small"
                    onClick={() => handleAction(record, 'reject')}
                  >
                    {t('page.afterSale.list.reject')}
                  </AButton>
                </>
              )}
              {isPendingHandle && (
                <>
                  <AButton
                    size="small"
                    type="primary"
                    onClick={() => handleComplete(record)}
                  >
                    {t('page.afterSale.list.complete')}
                  </AButton>
                  {canReverse && (
                    <AButton
                      danger
                      size="small"
                      onClick={() => handleAction(record, 'reverse')}
                    >
                      {t('page.afterSale.list.reverse')}
                    </AButton>
                  )}
                </>
              )}
              {canDelete && (
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
              )}
            </div>
          );
        },
        title: t('common.operate'),
        width: 210
      }
    ],
    pagination: createDefaultPagination(),
    scroll: { x: 'max-content' },
    transformParams: params => {
      if (!params) return params;
      let next = transformDateRange(params, {
        endKey: 'dateEnd',
        formatter: formatUtcBoundary,
        rangeKey: 'dateRange',
        startKey: 'dateStart'
      });
      next = transformEnumFields(next, ['afterStatus', 'afterSaleType', 'handleType']);
      return next;
    }
  });

  async function handleDelete(id: string) {
    await fetchDeleteAfterSale(id);
    window.$message?.success(t('common.deleteSuccess'));
    await run(false);
  }

  function promptRemark(title: string, required: boolean) {
    return new Promise<string | undefined>((resolve, reject) => {
      let remark = '';
      Modal.confirm({
        content: (
          <Input.TextArea
            allowClear
            autoSize={{ maxRows: 4, minRows: 2 }}
            placeholder={t(required ? 'page.afterSale.list.form.requiredRemark' : 'page.afterSale.list.form.remark')}
            onChange={event => {
              remark = event.target.value;
            }}
          />
        ),
        onCancel: () => reject(new Error('cancelled')),
        onOk: async () => {
          const value = remark.trim();
          if (required && !value) {
            window.$message?.warning(t('page.afterSale.list.remarkRequired'));
            throw new Error('remark-required');
          }
          resolve(value || undefined);
        },
        title
      });
    });
  }

  async function handleAction(
    record: Api.AfterSale.ListItem,
    action: 'approve' | 'reject' | 'resubmit' | 'reverse' | 'submit'
  ) {
    try {
      const required = action === 'reject' || action === 'reverse';
      const remark = await promptRemark(
        t(`page.afterSale.list.${action}Confirm`, {
          afterSaleNo: record.afterSaleNo
        }),
        required
      );
      const handlers = {
        approve: fetchApproveAfterSale,
        reject: fetchRejectAfterSale,
        resubmit: fetchResubmitAfterSale,
        reverse: fetchReverseAfterSale,
        submit: fetchSubmitAfterSale
      };
      await handlers[action](record.id, { remark });
      window.$message?.success(t('common.updateSuccess'));
      await run(false);
    } catch {
      // 用户取消或必填校验未通过
    }
  }

  function handleComplete(record: Api.AfterSale.ListItem) {
    Modal.confirm({
      content: t('page.afterSale.list.completeTip'),
      onOk: async () => {
        await fetchCompleteAfterSale(record.id);
        window.$message?.success(t('common.updateSuccess'));
        await run(false);
      },
      title: t('page.afterSale.list.completeConfirm', {
        afterSaleNo: record.afterSaleNo
      })
    });
  }

  return (
    <CrudPageLayout
      search={<AfterSaleSearch {...searchProps} />}
      tableWrapperRef={tableWrapperRef}
      title={t('page.afterSale.list.title')}
      extra={
        <TableHeaderOperation
          disabledDelete
          add={() => nav('/orders/after-sales/operate')}
          columns={columnChecks}
          loading={tableProps.loading}
          refresh={run}
          setColumnChecks={setColumnChecks}
          onDelete={() => undefined}
        >
          <AButton
            icon={<IconIcRoundPlus className="text-icon" />}
            size="small"
            type="primary"
            onClick={() => nav('/orders/after-sales/operate')}
          >
            {t('page.afterSale.operate.add')}
          </AButton>
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

export default AfterSaleList;
