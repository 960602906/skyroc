import type { DescriptionsProps, TableColumnsType } from 'antd';
import { useTranslation } from 'react-i18next';

import {
  displayDate,
  displayDateTime,
  displayText,
  renderPurchaseOrderStatus,
  renderPurchasePattern
} from '@/features/crud';

interface PurchaseOrderDetailViewProps {
  detail: Api.PurchaseOrder.Entity;
}

/** 采购单基础信息和商品明细展示。 */
const PurchaseOrderDetailView = ({ detail }: PurchaseOrderDetailViewProps) => {
  const { t } = useTranslation();

  const basicItems: DescriptionsProps['items'] = [
    { children: displayText(detail.purchaseNo), key: 'purchaseNo', label: t('page.purchase.order.purchaseNo') },
    {
      children: renderPurchaseOrderStatus(detail.businessStatus),
      key: 'businessStatus',
      label: t('page.purchase.order.businessStatus')
    },
    {
      children: renderPurchasePattern(detail.purchasePattern),
      key: 'purchasePattern',
      label: t('page.purchase.order.purchasePattern')
    },
    { children: displayText(detail.supplierName), key: 'supplierName', label: t('page.purchase.order.supplier') },
    { children: displayText(detail.purchaserName), key: 'purchaserName', label: t('page.purchase.order.purchaser') },
    {
      children: displayDateTime(detail.receiveTime),
      key: 'receiveTime',
      label: t('page.purchase.order.receiveTime')
    },
    {
      children: displayText(detail.supplierContactName),
      key: 'supplierContactName',
      label: t('page.purchase.order.supplierContactName')
    },
    {
      children: displayText(detail.supplierContactPhone),
      key: 'supplierContactPhone',
      label: t('page.purchase.order.supplierContactPhone')
    },
    { children: displayDateTime(detail.createTime), key: 'createTime', label: t('page.purchase.order.createTime') },
    { children: displayDateTime(detail.updateTime), key: 'updateTime', label: t('page.purchase.order.updateTime') },
    {
      children: displayText(detail.remark),
      key: 'remark',
      label: t('page.purchase.order.remark'),
      span: 'filled'
    }
  ];

  const detailColumns: TableColumnsType<Api.PurchaseOrder.Detail> = [
    {
      dataIndex: 'goodsName',
      ellipsis: true,
      key: 'goodsName',
      render: value => displayText(value),
      title: t('page.purchase.order.goodsName'),
      width: 170
    },
    {
      dataIndex: 'goodsCode',
      ellipsis: true,
      key: 'goodsCode',
      render: value => displayText(value),
      title: t('page.purchase.order.goodsCode'),
      width: 160
    },
    {
      align: 'center',
      dataIndex: 'purchaseUnitName',
      key: 'purchaseUnitName',
      render: value => displayText(value),
      title: t('page.purchase.order.unit'),
      width: 110
    },
    {
      align: 'right',
      dataIndex: 'requiredQuantity',
      key: 'requiredQuantity',
      title: t('page.purchase.order.requiredQuantity'),
      width: 120
    },
    {
      align: 'right',
      dataIndex: 'purchaseQuantity',
      key: 'purchaseQuantity',
      title: t('page.purchase.order.purchaseQuantity'),
      width: 120
    },
    {
      align: 'right',
      dataIndex: 'purchasePrice',
      key: 'purchasePrice',
      render: value => (value as number).toFixed(4),
      title: t('page.purchase.order.purchasePrice'),
      width: 120
    },
    {
      align: 'right',
      dataIndex: 'purchaseTotalPrice',
      key: 'purchaseTotalPrice',
      render: value => (value as number).toFixed(4),
      title: t('page.purchase.order.purchaseTotalPrice'),
      width: 120
    },
    {
      align: 'center',
      dataIndex: 'productDate',
      key: 'productDate',
      render: value => displayDate(value),
      title: t('page.purchase.order.productDate'),
      width: 120
    },
    {
      dataIndex: 'remark',
      ellipsis: { showTitle: false },
      key: 'remark',
      render: value => {
        const remark = displayText(value);
        return (
          <ATooltip title={remark}>
            <span className="block truncate">{remark}</span>
          </ATooltip>
        );
      },
      title: t('page.purchase.order.remark'),
      width: 220
    }
  ];

  return (
    <>
      <ACard
        className="card-wrapper"
        title={t('page.purchase.order.basicInfo')}
        variant="borderless"
      >
        <ADescriptions
          column={{ lg: 3, md: 2, sm: 1, xs: 1 }}
          items={basicItems}
          size="middle"
        />
      </ACard>
      <ACard
        className="card-wrapper"
        title={t('page.purchase.order.details')}
        variant="borderless"
      >
        <ATable
          columns={detailColumns}
          dataSource={detail.details}
          pagination={false}
          rowKey="id"
          scroll={{ x: 1300 }}
          size="small"
          tableLayout="fixed"
        />
      </ACard>
    </>
  );
};

export default PurchaseOrderDetailView;
