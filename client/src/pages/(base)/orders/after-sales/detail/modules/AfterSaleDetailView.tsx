import type { DescriptionsProps, TableColumnsType } from 'antd';

import { afterSaleAuditActionRecord, afterSaleHandleTypeRecord, afterSaleTypeRecord } from '@/constants/business';
import { DETAIL_EMPTY, displayDateTime, displayText, renderAfterSaleStatus } from '@/features/crud';

function formatMoney(value: number | null | undefined) {
  if (value === null || value === undefined || Number.isNaN(Number(value))) {
    return DETAIL_EMPTY;
  }
  return Number(value).toFixed(2);
}

function formatQuantity(value: number | null | undefined, unitName?: string | null) {
  if (value === null || value === undefined || Number.isNaN(Number(value))) {
    return DETAIL_EMPTY;
  }
  return unitName ? `${value} ${unitName}` : String(value);
}

interface AfterSaleDetailViewProps {
  detail: Api.AfterSale.Entity;
}

function AfterSaleDetailView({ detail }: AfterSaleDetailViewProps) {
  const { t } = useTranslation();
  const nav = useNavigate();

  const basicItems: DescriptionsProps['items'] = [
    {
      children: displayText(detail.afterSaleNo),
      key: 'afterSaleNo',
      label: t('page.afterSale.list.afterSaleNo')
    },
    {
      children: renderAfterSaleStatus(detail.afterStatus),
      key: 'afterStatus',
      label: t('page.afterSale.list.afterStatus')
    },
    {
      children: detail.saleOrderId ? (
        <AButton
          className="h-auto p-0 leading-normal"
          size="small"
          type="link"
          onClick={() => nav(`/orders/detail/${detail.saleOrderId}`)}
        >
          {displayText(detail.saleOrderNo)}
        </AButton>
      ) : (
        displayText(detail.saleOrderNo)
      ),
      key: 'saleOrderNo',
      label: t('page.afterSale.list.saleOrderNo')
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
      label: t('page.afterSale.list.customerName')
    },
    {
      children: displayText(detail.contactName),
      key: 'contactName',
      label: t('page.afterSale.list.contactName')
    },
    {
      children: displayText(detail.contactPhone),
      key: 'contactPhone',
      label: t('page.afterSale.list.contactPhone')
    },
    {
      children: displayText(detail.pickupAddress),
      key: 'pickupAddress',
      label: t('page.afterSale.detail.pickupAddress'),
      span: 'filled'
    }
  ];

  const amountItems: DescriptionsProps['items'] = [
    {
      children: formatMoney(detail.orderPrice),
      key: 'orderPrice',
      label: t('page.afterSale.list.orderPrice')
    },
    {
      children: formatMoney(detail.totalRefundAmount),
      key: 'totalRefundAmount',
      label: t('page.afterSale.list.totalRefundAmount')
    },
    {
      children: formatMoney(detail.settlementPrice),
      key: 'settlementPrice',
      label: t('page.afterSale.list.settlementPrice')
    }
  ];

  const remarkItems: DescriptionsProps['items'] = [
    {
      children: displayText(detail.remark),
      key: 'remark',
      label: t('page.afterSale.detail.remark'),
      span: 'filled'
    },
    {
      children: displayDateTime(detail.createTime),
      key: 'createTime',
      label: t('page.afterSale.detail.createTime')
    },
    {
      children: displayDateTime(detail.updateTime),
      key: 'updateTime',
      label: t('page.afterSale.detail.updateTime')
    }
  ];

  const goodsColumns: TableColumnsType<Api.AfterSale.Goods> = [
    {
      align: 'center',
      dataIndex: 'goodsName',
      ellipsis: true,
      key: 'goodsName',
      render: (_, record) => (
        <AButton
          className="h-auto p-0 leading-normal"
          size="small"
          type="link"
          onClick={() => nav(`/master/goods/detail/${record.goodsId}`)}
        >
          {displayText(record.goodsName)}
        </AButton>
      ),
      title: t('page.afterSale.detail.goodsName'),
      width: 180
    },
    {
      align: 'center',
      dataIndex: 'goodsCode',
      ellipsis: true,
      key: 'goodsCode',
      render: value => displayText(value),
      title: t('page.afterSale.detail.goodsCode'),
      width: 130
    },
    {
      align: 'center',
      dataIndex: 'afterSaleType',
      key: 'afterSaleType',
      render: value => {
        const key = afterSaleTypeRecord[value as Api.AfterSale.AfterSaleType];
        return key ? t(key) : displayText(value);
      },
      title: t('page.afterSale.detail.afterSaleType'),
      width: 120
    },
    {
      align: 'center',
      dataIndex: 'handleType',
      key: 'handleType',
      render: value => {
        const key = afterSaleHandleTypeRecord[value as Api.AfterSale.HandleType];
        return key ? t(key) : displayText(value);
      },
      title: t('page.afterSale.detail.handleType'),
      width: 130
    },
    {
      align: 'right',
      dataIndex: 'actualRefundQuantity',
      key: 'actualRefundQuantity',
      render: (value, record) => formatQuantity(value, record.goodsUnitName),
      title: t('page.afterSale.detail.refundQuantity'),
      width: 130
    },
    {
      align: 'right',
      dataIndex: 'baseRefundQuantity',
      key: 'baseRefundQuantity',
      render: (value, record) => formatQuantity(value, record.baseUnitName),
      title: t('page.afterSale.detail.baseRefundQuantity'),
      width: 130
    },
    {
      align: 'right',
      dataIndex: 'refundAmount',
      key: 'refundAmount',
      render: value => formatMoney(value),
      title: t('page.afterSale.detail.refundAmount'),
      width: 120
    },
    {
      align: 'center',
      dataIndex: 'remark',
      ellipsis: true,
      key: 'remark',
      render: value => displayText(value),
      title: t('page.afterSale.detail.remark'),
      width: 160
    }
  ];

  const auditColumns: TableColumnsType<Api.AfterSale.AuditLog> = [
    {
      align: 'center',
      dataIndex: 'action',
      key: 'action',
      render: value => {
        const key = afterSaleAuditActionRecord[value as Api.AfterSale.AuditAction];
        return key ? t(key) : displayText(value);
      },
      title: t('page.afterSale.detail.auditAction'),
      width: 120
    },
    {
      align: 'center',
      dataIndex: 'previousStatus',
      key: 'previousStatus',
      render: value => renderAfterSaleStatus(value),
      title: t('page.afterSale.detail.previousStatus'),
      width: 120
    },
    {
      align: 'center',
      dataIndex: 'currentStatus',
      key: 'currentStatus',
      render: value => renderAfterSaleStatus(value),
      title: t('page.afterSale.detail.currentStatus'),
      width: 120
    },
    {
      align: 'center',
      dataIndex: 'auditUserName',
      ellipsis: true,
      key: 'auditUserName',
      render: value => displayText(value),
      title: t('page.afterSale.detail.auditUserName'),
      width: 130
    },
    {
      align: 'center',
      className: 'whitespace-nowrap',
      dataIndex: 'auditTime',
      key: 'auditTime',
      render: value => displayDateTime(value),
      title: t('page.afterSale.detail.auditTime'),
      width: 170
    },
    {
      align: 'center',
      dataIndex: 'remark',
      ellipsis: true,
      key: 'remark',
      render: value => displayText(value),
      title: t('page.afterSale.detail.remark')
    }
  ];

  const descProps: Pick<DescriptionsProps, 'column' | 'size'> = {
    column: { lg: 2, md: 2, sm: 1, xl: 2, xs: 1, xxl: 2 },
    size: 'middle'
  };

  return (
    <>
      <ACard
        className="card-wrapper"
        title={t('page.afterSale.detail.sectionBasic')}
        variant="borderless"
      >
        <ADescriptions
          {...descProps}
          items={basicItems}
        />
      </ACard>

      <ACard
        className="card-wrapper"
        title={t('page.afterSale.detail.sectionGoods')}
        variant="borderless"
      >
        <ATable<Api.AfterSale.Goods>
          columns={goodsColumns}
          dataSource={detail.goods ?? []}
          pagination={false}
          rowKey="id"
          scroll={{ x: 'max-content' }}
          size="small"
        />
      </ACard>

      <ACard
        className="card-wrapper"
        title={t('page.afterSale.detail.sectionAmounts')}
        variant="borderless"
      >
        <ADescriptions
          {...descProps}
          items={amountItems}
        />
      </ACard>

      <ACard
        className="card-wrapper"
        title={t('page.afterSale.detail.sectionAudit')}
        variant="borderless"
      >
        <ATable<Api.AfterSale.AuditLog>
          columns={auditColumns}
          dataSource={detail.auditLogs ?? []}
          pagination={false}
          rowKey="id"
          scroll={{ x: 'max-content' }}
          size="small"
        />
      </ACard>

      <ACard
        className="card-wrapper"
        title={t('page.afterSale.detail.sectionRemark')}
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

export default AfterSaleDetailView;
