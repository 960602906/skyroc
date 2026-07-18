import { Suspense, lazy } from 'react';
import { type LoaderFunctionArgs, redirect, useLoaderData, useRevalidator } from 'react-router-dom';

import { useCloseTabAndNavigate } from '@/features/tab';
import { fetchGetCustomerTagDetail, fetchUpdateCustomerTag } from '@/service/api';

import CustomerTagDetailView from './modules/CustomerTagDetailView';

const CustomerTagOperateDrawer = lazy(() => import('../modules/CustomerTagOperateDrawer'));

const LIST_PATH = '/master/customer/tags';

export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) {
    return redirect(LIST_PATH);
  }

  try {
    const detail = await fetchGetCustomerTagDetail(id);
    if (!detail) {
      return redirect(LIST_PATH);
    }
    return detail;
  } catch {
    return redirect(LIST_PATH);
  }
}

const CustomerTagDetail = () => {
  const { t } = useTranslation();
  const detail = useLoaderData() as Api.CustomerTag.Entity;
  const closeTabAndNavigate = useCloseTabAndNavigate();
  const revalidator = useRevalidator();
  const [form] = AForm.useForm<Api.CustomerTag.UpdateParams>();
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
      await fetchUpdateCustomerTag({ ...values, id: detail.id });
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
            <AButton onClick={() => closeTabAndNavigate(LIST_PATH)}>{t('page.customer.tag.detail.back')}</AButton>
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
          {t('page.customer.tag.code')}：{detail.code}
        </span>
      </ACard>

      <div className="flex-col-stretch gap-16px">
        <CustomerTagDetailView detail={detail} />
      </div>

      <Suspense>
        <CustomerTagOperateDrawer
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

export default CustomerTagDetail;
