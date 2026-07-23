import { useState } from 'react';
import { type LoaderFunctionArgs, redirect, useLoaderData } from 'react-router-dom';

import { DetailPageLayout, renderPurchasePlanStatus } from '@/features/crud';
import {
  fetchGetPurchasePlanDetail,
  fetchGetPurchasePlanSplitOrders,
  fetchSplitOrdersPurchasePlan,
  fetchSplitQuantityPurchasePlan
} from '@/service/api';
import { PurchasePlanStatus } from '@/service/enums';

import PurchasePlanDetailView from './modules/PurchasePlanDetailView';

const LIST_PATH = '/purchase/plans';

type SplitMode = 'splitOrders' | 'splitQuantity' | null;

const modalTitleKeyMap: Record<Exclude<SplitMode, null>, App.I18n.I18nKey> = {
  splitOrders: 'page.purchase.plan.splitByOrders',
  splitQuantity: 'page.purchase.plan.splitByQuantity'
};

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
  }

  return (
    <DetailPageLayout
      backLabel={t('page.purchase.plan.back')}
      banner={renderPurchasePlanStatus(detail.purchaseStatus)}
      listPath={LIST_PATH}
      title={detail.planNo}
      extra={
        canSplit && (
          <>
            <AButton
              type="primary"
              onClick={() => openSplit('splitOrders')}
            >
              {t('page.purchase.plan.splitByOrders')}
            </AButton>
            <AButton onClick={() => openSplit('splitQuantity')}>{t('page.purchase.plan.splitByQuantity')}</AButton>
          </>
        )
      }
    >
      <PurchasePlanDetailView detail={detail} />
      <AModal
        destroyOnClose
        open={mode !== null}
        title={mode ? t(modalTitleKeyMap[mode]) : ''}
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
    </DetailPageLayout>
  );
};

export default PurchasePlanDetail;
