import type { DescriptionsProps, TableColumnsType } from 'antd';
import { useState } from 'react';
import { type LoaderFunctionArgs, redirect, useLoaderData } from 'react-router-dom';

import {
  displayDate,
  displayDateTime,
  displayText,
  renderPurchasePattern,
  renderPurchasePlanStatus
} from '@/features/crud';
import { useCloseTabAndNavigate } from '@/features/tab';
import {
  fetchGetPurchasePlanDetail,
  fetchGetPurchasePlanSplitOrders,
  fetchSplitOrdersPurchasePlan,
  fetchSplitQuantityPurchasePlan
} from '@/service/api';
import { PurchasePlanStatus } from '@/service/enums';

const LIST_PATH = '/purchase/plans';

type SplitMode = 'splitOrders' | 'splitQuantity' | null;

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
  const [form] = AForm.useForm();
  const [mode, setMode] = useState<SplitMode>(null);
  const [splitOrders, setSplitOrders] = useState<Api.PurchasePlan.SplittableOrder[]>([]);

  if (!detail) return null;

  const canSplit = detail.purchaseStatus === PurchasePlanStatus.UNPUBLISHED;

  async function openSplit(nextMode: 'splitOrders' | 'splitQuantity') {
    form.resetFields();
    if (nextMode === 'splitOrders') {
      setSplitOrders(await fetchGetPurchasePlanSplitOrders(detail.id));
    }
    if (nextMode === 'splitQuantity') {
      form.setFieldsValue({ details: detail.details.map(d => ({ detailId: d.id })) });
    }
    setMode(nextMode);
  }

  async function submitSplit() {
    const values = await form.validateFields();
    if (mode === 'splitOrders') {
      await fetchSplitOrdersPurchasePlan({
        planId: detail.id,
        remark: values.remark,
        saleOrderIds: values.saleOrderIds
      });
    }
    if (mode === 'splitQuantity') {
      await fetchSplitQuantityPurchasePlan({
        details: values.details,
        planId: detail.id,
        remark: values.remark
      });
    }
    window.$message?.success(t('common.updateSuccess'));
    setMode(null);
    closeTabAndNavigate(LIST_PATH);
  }

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
        variant="borderless"
        extra={
          <AFlex gap={8}>
            {canSplit && (
              <>
                <AButton
                  type="primary"
                  onClick={() => openSplit('splitOrders')}
                >
                  {t('page.purchase.plan.splitByOrders')}
                </AButton>
                <AButton onClick={() => openSplit('splitQuantity')}>{t('page.purchase.plan.splitByQuantity')}</AButton>
              </>
            )}
            <AButton onClick={() => closeTabAndNavigate(LIST_PATH)}>{t('page.purchase.plan.back')}</AButton>
          </AFlex>
        }
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
      <AModal
        destroyOnClose
        open={mode !== null}
        title={mode ? t(`page.purchase.plan.${mode}`) : ''}
        onCancel={() => setMode(null)}
        onOk={submitSplit}
      >
        <AForm
          form={form}
          layout="vertical"
        >
          {mode === 'splitOrders' && (
            <AForm.Item
              label={t('page.purchase.plan.splitByOrders')}
              name="saleOrderIds"
              rules={[{ required: true }]}
            >
              <ASelect
                mode="multiple"
                placeholder={t('page.purchase.plan.form.saleOrderIds')}
                options={splitOrders.map(item => ({
                  label: `${item.saleOrderNo} (${item.requiredQuantity})`,
                  value: item.saleOrderId
                }))}
              />
            </AForm.Item>
          )}
          {mode === 'splitQuantity' && (
            <AForm.List name="details">
              {() =>
                detail.details.map((d, index) => (
                  <ARow
                    gutter={8}
                    key={d.id}
                  >
                    <ACol span={14}>
                      {d.goodsName} ({d.purchaseUnitName})
                    </ACol>
                    <ACol span={10}>
                      <AForm.Item
                        hidden
                        initialValue={d.id}
                        name={[index, 'detailId']}
                      >
                        <AInput />
                      </AForm.Item>
                      <AForm.Item name={[index, 'quantity']}>
                        <AInputNumber
                          className="w-full"
                          max={d.plannedQuantity}
                          min={0.0001}
                        />
                      </AForm.Item>
                    </ACol>
                  </ARow>
                ))
              }
            </AForm.List>
          )}
          <AForm.Item
            label={t('page.purchase.plan.remark')}
            name="remark"
          >
            <AInput.TextArea />
          </AForm.Item>
        </AForm>
      </AModal>
    </div>
  );
};

export default PurchasePlanDetail;
