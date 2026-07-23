import { Suspense, lazy } from 'react';
import { type LoaderFunctionArgs, redirect, useLoaderData, useRevalidator } from 'react-router-dom';

import { DetailPageLayout } from '@/features/crud';
import { fetchGetSupplierDetail, fetchUpdateSupplier } from '@/service/api';

import SupplierDetailView from './modules/SupplierDetailView';

const SupplierOperateDrawer = lazy(() => import('../modules/SupplierOperateDrawer'));

const LIST_PATH = '/master/supply/suppliers';

export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) {
    return redirect(LIST_PATH);
  }

  try {
    const detail = await fetchGetSupplierDetail(id);
    if (!detail) {
      return redirect(LIST_PATH);
    }
    return detail;
  } catch {
    return redirect(LIST_PATH);
  }
}

const SupplierDetail = () => {
  const { t } = useTranslation();
  const detail = useLoaderData() as Api.Supplier.Entity;
  const revalidator = useRevalidator();
  const [form] = AForm.useForm<Api.Supplier.UpdateParams>();
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  if (!detail) {
    return null;
  }

  function openEdit() {
    form.setFieldsValue({ ...detail });
    setDrawerOpen(true);
  }

  async function handleSubmit() {
    const values = await form.validateFields();
    setSubmitting(true);
    try {
      await fetchUpdateSupplier({ ...values, id: detail.id });
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
        backLabel={t('page.purchase.supplier.detail.back')}
        listPath={LIST_PATH}
        title={detail.name}
        banner={
          <span>
            {t('page.purchase.supplier.code')}：{detail.code}
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
        <SupplierDetailView detail={detail} />
      </DetailPageLayout>

      <Suspense>
        <SupplierOperateDrawer
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

export default SupplierDetail;
