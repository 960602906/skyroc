import type { DescriptionsProps, TableColumnsType } from 'antd';
import { useState } from 'react';
import { type LoaderFunctionArgs, redirect, useLoaderData } from 'react-router-dom';

import {
  displayDate,
  displayDateTime,
  displayText,
  renderPurchaseOrderStatus,
  renderPurchasePattern
} from '@/features/crud';
import { useCloseTabAndNavigate } from '@/features/tab';
import { fetchCancelPurchaseOrder, fetchCompletePurchaseOrder, fetchGetPurchaseOrderDetail } from '@/service/api';
import { PurchaseOrderStatus } from '@/service/enums';

const LIST_PATH = '/purchase/orders';

/** 路由切换前加载采购单详情，采购单不存在或加载失败时返回列表。 */
export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) return redirect(LIST_PATH);

  try {
    const detail = await fetchGetPurchaseOrderDetail(id);
    return detail ?? redirect(LIST_PATH);
  } catch {
    return redirect(LIST_PATH);
  }
}

/** 采购单基础信息、商品明细和采购计划来源详情页。 */
const PurchaseOrderDetail = () => {
  const { t } = useTranslation();
  const closeTabAndNavigate = useCloseTabAndNavigate();
  const detail = useLoaderData() as Api.PurchaseOrder.Entity;
  const [confirmModalOpen, setConfirmModalOpen] = useState<'complete' | 'cancel' | null>(null);

  if (!detail) return null;

  const canOperate = detail.businessStatus === PurchaseOrderStatus.DRAFT;

  async function handleComplete() {
    await fetchCompletePurchaseOrder(detail.id);
    window.$message?.success(t('common.updateSuccess'));
    setConfirmModalOpen(null);
    closeTabAndNavigate(LIST_PATH);
  }

  async function handleCancel() {
    await fetchCancelPurchaseOrder(detail.id);
    window.$message?.success(t('common.updateSuccess'));
    setConfirmModalOpen(null);
    closeTabAndNavigate(LIST_PATH);
  }

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
    <div className="h-full min-h-500px flex-col-stretch gap-16px overflow-auto">
      <ACard
        className="card-wrapper"
        variant="borderless"
        extra={
          <AFlex gap={8}>
            {canOperate && (
              <>
                <AButton
                  type="primary"
                  onClick={() => setConfirmModalOpen('complete')}
                >
                  {t('page.purchase.order.complete')}
                </AButton>
                <AButton
                  danger
                  onClick={() => setConfirmModalOpen('cancel')}
                >
                  {t('page.purchase.order.cancel')}
                </AButton>
              </>
            )}
            <AButton onClick={() => closeTabAndNavigate(LIST_PATH)}>{t('page.purchase.order.back')}</AButton>
          </AFlex>
        }
        title={
          <AFlex
            wrap
            align="center"
            gap={12}
          >
            <span>{detail.purchaseNo}</span>
            {renderPurchaseOrderStatus(detail.businessStatus)}
          </AFlex>
        }
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
      <AModal
        destroyOnClose
        open={confirmModalOpen === 'complete'}
        title={t('page.purchase.order.complete')}
        onCancel={() => setConfirmModalOpen(null)}
        onOk={handleComplete}
      >
        <p>{t('page.purchase.order.complete')}?</p>
      </AModal>
      <AModal
        destroyOnClose
        open={confirmModalOpen === 'cancel'}
        title={t('page.purchase.order.cancel')}
        onCancel={() => setConfirmModalOpen(null)}
        onOk={handleCancel}
      >
        <p>{t('page.purchase.order.cancel')}?</p>
      </AModal>
    </div>
  );
};

export default PurchaseOrderDetail;
