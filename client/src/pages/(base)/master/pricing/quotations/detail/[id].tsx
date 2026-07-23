import { Suspense, lazy } from 'react';
import { type LoaderFunctionArgs, redirect, useLoaderData, useRevalidator } from 'react-router-dom';

import { DetailPageLayout } from '@/features/crud';
import { fetchGetQuotationDetail, fetchUpdateQuotation } from '@/service/api';

import QuotationDetailView from './modules/QuotationDetailView';

const QuotationOperateDrawer = lazy(() => import('../modules/QuotationOperateDrawer'));

const LIST_PATH = '/master/pricing/quotations';

export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) {
    return redirect(LIST_PATH);
  }

  try {
    const detail = await fetchGetQuotationDetail(id);
    if (!detail) {
      return redirect(LIST_PATH);
    }
    return detail;
  } catch {
    return redirect(LIST_PATH);
  }
}

const QuotationDetail = () => {
  const { t } = useTranslation();
  const detail = useLoaderData() as Api.Quotation.Entity;
  const revalidator = useRevalidator();
  const [form] = AForm.useForm<Api.Quotation.UpdateParams>();
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  function openEdit() {
    form.setFieldsValue({
      ...detail,
      customerIds: detail.customerIds ?? undefined
    });
    setDrawerOpen(true);
  }

  async function handleSubmit() {
    const values = await form.validateFields();
    setSubmitting(true);
    try {
      await fetchUpdateQuotation({ ...values, id: detail.id });
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
        backLabel={t('page.goods.quotation.detail.back')}
        listPath={LIST_PATH}
        title={detail.name}
        banner={
          <span>
            {t('page.goods.quotation.code')}：{detail.code}
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
        <QuotationDetailView
          detail={detail}
          onGoodsChanged={() => revalidator.revalidate()}
        />
      </DetailPageLayout>

      <Suspense>
        <QuotationOperateDrawer
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

export default QuotationDetail;
