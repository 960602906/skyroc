import { Suspense, lazy } from 'react';
import { type LoaderFunctionArgs, redirect, useLoaderData, useRevalidator } from 'react-router-dom';

import { DetailPageLayout } from '@/features/crud';
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
  const revalidator = useRevalidator();
  const [form] = AForm.useForm<Api.PurchaseRule.UpdateParams>();
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  if (!detail) {
    return null;
  }

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
    <>
      <DetailPageLayout
        backLabel={t('page.purchase.rule.detail.back')}
        listPath={LIST_PATH}
        title={detail.name}
        banner={
          <span>
            {t('page.purchase.rule.code')}：{detail.code}
          </span>
        }
        extra={
          <AButton
            type="primary"
            onClick={openEdit}
          >
            {t('common.edit')}
          </AButton>
        }
      >
        <RuleDetailView detail={detail} />
      </DetailPageLayout>

      <Suspense>
        <RuleOperateDrawer
          form={form}
          handleSubmit={handleSubmit}
          open={drawerOpen}
          operateType="edit"
          onClose={() => !submitting && setDrawerOpen(false)}
        />
      </Suspense>
    </>
  );
};

export default RuleDetail;
