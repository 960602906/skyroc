import type { DescriptionsProps, TableColumnsType } from 'antd';

import { orderAuditActionRecord } from '@/constants/business';
import {
  DETAIL_EMPTY,
  displayDate,
  displayDateTime,
  displayText,
  renderBooleanYesNo,
  renderOrderOutStorageStatus,
  renderOrderPrintStatus,
  renderOrderReturnStatus,
  renderSaleOrderStatus
} from '@/features/crud';
import { useQuotationOptions } from '@/service/hooks';

function formatMoney(value: number | null | undefined) {
  if (value === null || value === undefined || Number.isNaN(Number(value))) {
    return DETAIL_EMPTY;
  }
  return Number(value).toFixed(2);
}

interface OrderDetailViewProps {
  detail: Api.Order.Entity;
}

function OrderDetailView({ detail }: OrderDetailViewProps) {
  const { t } = useTranslation();
  const nav = useNavigate();
  const { data: quotationOptions = [] } = useQuotationOptions();

  const quotationName = quotationOptions.find(item => item.value === detail.quotationId)?.label;

  const basicItems: DescriptionsProps['items'] = [
    {
      children: displayText(detail.orderNo),
      key: 'orderNo',
      label: t('page.order.list.orderNo')
    },
    {
      children: renderSaleOrderStatus(detail.orderStatus),
      key: 'orderStatus',
      label: t('page.order.list.orderStatus')
    },
    {
      children: detail.customerId ? (
        <AButton
          className="h-auto p-0 leading-normal"
          size="small"
          type="link"
          onClick={() => nav(`/master/customer/detail/${detail.customerId}`)}
        >
          {displayText(detail.customerName)}
        </AButton>
      ) : (
        displayText(detail.customerName)
      ),
      key: 'customerName',
      label: t('page.order.list.customerName')
    },
    {
      children: displayText(detail.customerCode),
      key: 'customerCode',
      label: t('page.order.list.customerCode')
    },
    {
      children: displayDate(detail.orderDate),
      key: 'orderDate',
      label: t('page.order.list.orderDate')
    },
    {
      children: displayDate(detail.receiveDate),
      key: 'receiveDate',
      label: t('page.order.list.receiveDate')
    },
    {
      children: displayDateTime(detail.outDate),
      key: 'outDate',
      label: t('page.order.detail.outDate')
    },
    {
      children: displayText(detail.wareName),
      key: 'wareName',
      label: t('page.order.list.wareName')
    },
    {
      children: displayText(quotationName || detail.quotationId),
      key: 'quotationId',
      label: t('page.order.detail.quotationId')
    },
    {
      children: displayText(detail.contactName),
      key: 'contactName',
      label: t('page.order.list.contactName')
    },
    {
      children: displayText(detail.contactPhone),
      key: 'contactPhone',
      label: t('page.order.list.contactPhone')
    },
    {
      // filled：占满当前行剩余列，适配任意 column，不会触发 span 合计告警
      children: displayText(detail.deliveryAddress),
      key: 'deliveryAddress',
      label: t('page.order.detail.deliveryAddress'),
      span: 'filled'
    }
  ];

  const amountItems: DescriptionsProps['items'] = [
    {
      children: formatMoney(detail.orderPrice),
      key: 'orderPrice',
      label: t('page.order.list.orderPrice')
    },
    {
      children: formatMoney(detail.settlementPrice),
      key: 'settlementPrice',
      label: t('page.order.list.settlementPrice')
    },
    {
      children: renderOrderReturnStatus(detail.returnStatus),
      key: 'returnStatus',
      label: t('page.order.list.returnStatus')
    },
    {
      children: renderOrderPrintStatus(detail.printStatus),
      key: 'printStatus',
      label: t('page.order.list.printStatus')
    },
    {
      children: renderOrderOutStorageStatus(detail.outStorageStatus),
      key: 'outStorageStatus',
      label: t('page.order.list.outStorageStatus')
    },
    {
      children: renderBooleanYesNo(detail.hasOutSale),
      key: 'hasOutSale',
      label: t('page.order.list.hasOutSale')
    },
    {
      children: renderBooleanYesNo(detail.hasPurchasePlan),
      key: 'hasPurchasePlan',
      label: t('page.order.list.hasPurchasePlan')
    },
    {
      children: renderBooleanYesNo(detail.updateStatus),
      key: 'updateStatus',
      label: t('page.order.list.updateStatus')
    }
  ];

  const remarkItems: DescriptionsProps['items'] = [
    {
      children: displayText(detail.remark),
      key: 'remark',
      label: t('page.order.detail.remark'),
      span: 'filled'
    },
    {
      children: displayText(detail.innerRemark),
      key: 'innerRemark',
      label: t('page.order.detail.innerRemark'),
      span: 'filled'
    },
    {
      children: displayDateTime(detail.createTime),
      key: 'createTime',
      label: t('page.order.list.createTime')
    },
    {
      children: displayDateTime(detail.updateTime),
      key: 'updateTime',
      label: t('page.order.detail.updateTime')
    }
  ];

  const detailColumns: TableColumnsType<Api.Order.Detail> = [
    {
      align: 'center',
      dataIndex: 'goodsName',
      ellipsis: true,
      key: 'goodsName',
      render: (_, record) =>
        record.goodsId ? (
          <AButton
            className="h-auto p-0 leading-normal"
            size="small"
            type="link"
            onClick={() => nav(`/master/goods/detail/${record.goodsId}`)}
          >
            {displayText(record.goodsName)}
          </AButton>
        ) : (
          displayText(record.goodsName)
        ),
      title: t('page.order.detail.goodsName'),
      width: 180
    },
    {
      align: 'center',
      dataIndex: 'goodsCode',
      ellipsis: true,
      key: 'goodsCode',
      render: value => displayText(value),
      title: t('page.order.detail.goodsCode'),
      width: 120
    },
    {
      align: 'center',
      dataIndex: 'goodsUnitName',
      key: 'goodsUnitName',
      render: value => displayText(value),
      title: t('page.order.detail.goodsUnitName'),
      width: 100
    },
    {
      align: 'right',
      dataIndex: 'quantity',
      key: 'quantity',
      render: value => displayText(value),
      title: t('page.order.detail.quantity'),
      width: 100
    },
    {
      align: 'right',
      dataIndex: 'baseQuantity',
      key: 'baseQuantity',
      render: (value, record) => {
        const qty = displayText(value);
        const unit = record.baseUnitName ? ` ${record.baseUnitName}` : '';
        return value === null || value === undefined ? DETAIL_EMPTY : `${qty}${unit}`;
      },
      title: t('page.order.detail.baseQuantity'),
      width: 120
    },
    {
      align: 'right',
      dataIndex: 'fixedPrice',
      key: 'fixedPrice',
      render: value => formatMoney(value as number),
      title: t('page.order.detail.fixedPrice'),
      width: 110
    },
    {
      align: 'center',
      dataIndex: 'fixedGoodsUnitName',
      key: 'fixedGoodsUnitName',
      render: value => displayText(value),
      title: t('page.order.detail.fixedGoodsUnitName'),
      width: 100
    },
    {
      align: 'right',
      dataIndex: 'totalPrice',
      key: 'totalPrice',
      render: value => formatMoney(value as number),
      title: t('page.order.detail.totalPrice'),
      width: 110
    },
    {
      align: 'center',
      dataIndex: 'remark',
      ellipsis: true,
      key: 'remark',
      render: value => displayText(value),
      title: t('page.order.detail.remark'),
      width: 140
    },
    {
      align: 'center',
      dataIndex: 'innerRemark',
      ellipsis: true,
      key: 'innerRemark',
      render: value => displayText(value),
      title: t('page.order.detail.innerRemark'),
      width: 140
    }
  ];

  const auditColumns: TableColumnsType<Api.Order.AuditLog> = [
    {
      align: 'center',
      dataIndex: 'action',
      key: 'action',
      render: (action: Api.Order.AuditAction) => {
        const key = orderAuditActionRecord[action];
        return key ? t(key) : displayText(action);
      },
      title: t('page.order.detail.auditAction'),
      width: 120
    },
    {
      align: 'center',
      dataIndex: 'previousStatus',
      key: 'previousStatus',
      render: value => renderSaleOrderStatus(value),
      title: t('page.order.detail.previousStatus'),
      width: 120
    },
    {
      align: 'center',
      dataIndex: 'currentStatus',
      key: 'currentStatus',
      render: value => renderSaleOrderStatus(value),
      title: t('page.order.detail.currentStatus'),
      width: 120
    },
    {
      align: 'center',
      dataIndex: 'auditUserName',
      ellipsis: true,
      key: 'auditUserName',
      render: value => displayText(value),
      title: t('page.order.detail.auditUserName'),
      width: 120
    },
    {
      align: 'center',
      className: 'whitespace-nowrap',
      dataIndex: 'auditTime',
      key: 'auditTime',
      render: value => displayDateTime(value as string),
      title: t('page.order.detail.auditTime'),
      width: 170
    },
    {
      align: 'center',
      dataIndex: 'remark',
      ellipsis: true,
      key: 'remark',
      render: value => displayText(value),
      title: t('page.order.detail.remark')
    }
  ];

  // 必须覆盖 xxl/xl：antd 会与默认 { xxl:3, xl:3, lg:3... } 合并，漏写时大屏仍是 3 列
  const descProps: Pick<DescriptionsProps, 'column' | 'size'> = {
    column: { lg: 2, md: 2, sm: 1, xl: 2, xs: 1, xxl: 2 },
    size: 'middle'
  };

  const details = detail.details ?? [];
  const auditLogs = detail.auditLogs ?? [];

  return (
    <>
      <ACard
        className="card-wrapper"
        title={t('page.order.detail.sectionBasic')}
        variant="borderless"
      >
        <ADescriptions
          {...descProps}
          items={basicItems}
        />
      </ACard>

      <ACard
        className="card-wrapper"
        title={t('page.order.detail.sectionDetails')}
        variant="borderless"
      >
        <ATable<Api.Order.Detail>
          columns={detailColumns}
          dataSource={details}
          pagination={false}
          rowKey="id"
          scroll={{ x: 'max-content' }}
          size="small"
        />
      </ACard>

      <ACard
        className="card-wrapper"
        title={t('page.order.detail.sectionAmounts')}
        variant="borderless"
      >
        <ADescriptions
          {...descProps}
          items={amountItems}
        />
      </ACard>

      <ACard
        className="card-wrapper"
        title={t('page.order.detail.sectionAudit')}
        variant="borderless"
      >
        <ATable<Api.Order.AuditLog>
          columns={auditColumns}
          dataSource={auditLogs}
          pagination={false}
          rowKey="id"
          scroll={{ x: 'max-content' }}
          size="small"
        />
      </ACard>

      <ACard
        className="card-wrapper"
        title={t('page.order.detail.sectionRemark')}
        variant="borderless"
      >
        <ADescriptions
          {...descProps}
          items={remarkItems}
        />
      </ACard>
    </>
  );
}

export default OrderDetailView;
