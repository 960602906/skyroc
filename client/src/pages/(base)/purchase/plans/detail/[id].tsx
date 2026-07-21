import type { DescriptionsProps, TableColumnsType } from 'antd';
import { type LoaderFunctionArgs, redirect, useLoaderData } from 'react-router-dom';

import {
  displayDate,
  displayDateTime,
  displayText,
  renderPurchasePattern,
  renderPurchasePlanStatus
} from '@/features/crud';
import { useCloseTabAndNavigate } from '@/features/tab';
import { fetchGetPurchasePlanDetail } from '@/service/api';

const LIST_PATH = '/purchase/plans';

/** 路由切换前加载采购计划详情，计划不存在或加载失败时返回列表。 */
export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) return redirect(LIST_PATH);

  try {
    const detail = await fetchGetPurchasePlanDetail(id);
    return detail ?? redirect(LIST_PATH);
  } catch {
    return redirect(LIST_PATH);
  }
}

/** 采购计划主单、商品明细和采购执行进度详情页。 */
const PurchasePlanDetail = () => {
  const { t } = useTranslation();
  const closeTabAndNavigate = useCloseTabAndNavigate();
  const detail = useLoaderData() as Api.PurchasePlan.Entity;

  if (!detail) return null;

  const summaryItems: DescriptionsProps['items'] = [
    { children: displayDate(detail.planDate), key: 'planDate', label: t('page.purchase.plan.planDate') },
    {
      children: renderPurchasePattern(detail.purchasePattern),
      key: 'purchasePattern',
      label: t('page.purchase.plan.purchasePattern')
    },
    { children: displayText(detail.supplierName), key: 'supplier', label: t('page.purchase.plan.supplier') },
    { children: displayText(detail.purchaserName), key: 'purchaser', label: t('page.purchase.plan.purchaser') },
    { children: displayDateTime(detail.createTime), key: 'createTime', label: t('page.purchase.plan.createTime') },
    { children: displayDateTime(detail.updateTime), key: 'updateTime', label: t('page.purchase.plan.updateTime') },
    {
      children: displayText(detail.remark),
      key: 'remark',
      label: t('page.purchase.plan.remark'),
      span: 'filled'
    }
  ];

  const detailColumns: TableColumnsType<Api.PurchasePlan.Detail> = [
    {
      dataIndex: 'goodsName',
      ellipsis: true,
      key: 'goodsName',
      render: value => displayText(value),
      title: t('page.purchase.plan.goods'),
      width: 170
    },
    {
      dataIndex: 'goodsCode',
      ellipsis: true,
      key: 'goodsCode',
      render: value => displayText(value),
      title: t('page.purchase.plan.goodsCode'),
      width: 160
    },
    {
      align: 'center',
      dataIndex: 'purchaseUnitName',
      key: 'purchaseUnitName',
      render: value => displayText(value),
      title: t('page.purchase.plan.unit'),
      width: 110
    },
    {
      align: 'right',
      dataIndex: 'requiredQuantity',
      key: 'requiredQuantity',
      title: t('page.purchase.plan.requiredQuantity'),
      width: 120
    },
    {
      align: 'right',
      dataIndex: 'plannedQuantity',
      key: 'plannedQuantity',
      title: t('page.purchase.plan.plannedQuantity'),
      width: 120
    },
    {
      align: 'right',
      dataIndex: 'purchasedQuantity',
      key: 'purchasedQuantity',
      title: t('page.purchase.plan.purchasedQuantity'),
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
      title: t('page.purchase.plan.remark'),
      width: 220
    }
  ];

  return (
    <div className="h-full min-h-500px flex-col-stretch gap-16px overflow-auto">
      <ACard
        className="card-wrapper"
        extra={<AButton onClick={() => closeTabAndNavigate(LIST_PATH)}>{t('page.purchase.plan.back')}</AButton>}
        variant="borderless"
        title={
          <AFlex
            wrap
            align="center"
            gap={12}
          >
            <span>{detail.planNo}</span>
            {renderPurchasePlanStatus(detail.purchaseStatus)}
          </AFlex>
        }
      >
        <ADescriptions
          column={{ lg: 3, md: 2, sm: 1, xs: 1 }}
          items={summaryItems}
          size="middle"
        />
      </ACard>
      <ACard
        className="card-wrapper"
        title={t('page.purchase.plan.details')}
        variant="borderless"
      >
        <ATable
          columns={detailColumns}
          dataSource={detail.details}
          pagination={false}
          rowKey="id"
          scroll={{ x: 980 }}
          size="small"
          tableLayout="fixed"
        />
      </ACard>
    </div>
  );
};

export default PurchasePlanDetail;
