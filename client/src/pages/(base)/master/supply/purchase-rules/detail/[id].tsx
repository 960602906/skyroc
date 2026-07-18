import { Suspense, lazy } from 'react';
import { type LoaderFunctionArgs, redirect, useLoaderData, useRevalidator } from 'react-router-dom';

import { useCloseTabAndNavigate } from '@/features/tab';
import { fetchGetPurchaseRuleDetail, fetchUpdatePurchaseRule } from '@/service/api';

import RuleDetailView from './modules/RuleDetailView';

const RuleOperateDrawer = lazy(() => import('../modules/RuleOperateDrawer'));

const LIST_PATH = '/master/supply/purchase-rules';

export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) {
    return redirect(LIST_PATH);
  }

  try {
    const detail = await fetchGetPurchaseRuleDetail(id);
    if (!detail) {
      return redirect(LIST_PATH);
    }
    return detail;
  } catch {
    return redirect(LIST_PATH);
  }
}

const RuleDetail = () => {
  const { t } = useTranslation();
  const detail = useLoaderData() as Api.PurchaseRule.Entity;
  const closeTabAndNavigate = useCloseTabAndNavigate();
  const revalidator = useRevalidator();
  const [form] = AForm.useForm<Api.PurchaseRule.UpdateParams>();
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  function openEdit() {
    form.setFieldsValue({
      ...detail,
      customerIds: detail.customerIds ?? undefined,
      goodsIds: detail.goodsIds ?? undefined
    });
    setDrawerOpen(true);
  }

  async function handleSubmit() {
    const values = await form.validateFields();
    setSubmitting(true);
    try {
      await fetchUpdatePurchaseRule({ ...values, id: detail.id });
      window.$message?.success(t('common.modifySuccess'));
      setDrawerOpen(false);
      revalidator.revalidate();
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="h-full min-h-500px flex-col-stretch gap-16px overflow-auto">
      <ACard
        className="card-wrapper"
        title={detail.name}
        variant="borderless"
        extra={
          <ASpace>
            <AButton onClick={() => closeTabAndNavigate(LIST_PATH)}>{t('page.purchase.rule.detail.back')}</AButton>
            <AButton
              type="primary"
              onClick={openEdit}
            >
              {t('common.edit')}
            </AButton>
          </ASpace>
        }
      >
        <span className="opacity-60">
          {t('page.purchase.rule.code')}：{detail.code}
        </span>
      </ACard>

      <div className="flex-col-stretch gap-16px">
        <RuleDetailView detail={detail} />
      </div>

      <Suspense>
        <RuleOperateDrawer
          form={form}
          handleSubmit={handleSubmit}
          open={drawerOpen}
          operateType="edit"
          onClose={() => !submitting && setDrawerOpen(false)}
        />
      </Suspense>
    </div>
  );
};

export default RuleDetail;
