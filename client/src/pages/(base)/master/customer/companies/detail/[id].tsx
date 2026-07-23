import { Suspense, lazy } from 'react';
import { type LoaderFunctionArgs, redirect, useLoaderData, useRevalidator } from 'react-router-dom';

import { DetailPageLayout } from '@/features/crud';
import { fetchGetCompanyDetail, fetchUpdateCompany } from '@/service/api';

import CompanyDetailView from './modules/CompanyDetailView';

const CompanyOperateDrawer = lazy(() => import('../modules/CompanyOperateDrawer'));

const LIST_PATH = '/master/customer/companies';

export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) {
    return redirect(LIST_PATH);
  }

  try {
    const detail = await fetchGetCompanyDetail(id);
    if (!detail) {
      return redirect(LIST_PATH);
    }
    return detail;
  } catch {
    return redirect(LIST_PATH);
  }
}

const CompanyDetail = () => {
  const { t } = useTranslation();
  const detail = useLoaderData() as Api.Company.Entity;
  const revalidator = useRevalidator();
  const [form] = AForm.useForm<Api.Company.UpdateParams>();
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  function openEdit() {
    form.setFieldsValue({ ...detail });
    setDrawerOpen(true);
  }

  async function handleSubmit() {
    const values = await form.validateFields();
    setSubmitting(true);
    try {
      await fetchUpdateCompany({ ...values, id: detail.id });
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
        backLabel={t('page.customer.company.detail.back')}
        listPath={LIST_PATH}
        title={detail.name}
        banner={
          <span>
            {t('page.customer.company.code')}：{detail.code}
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
        <CompanyDetailView detail={detail} />
      </DetailPageLayout>

      <Suspense>
        <CompanyOperateDrawer
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

export default CompanyDetail;
