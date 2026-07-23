import { Suspense, lazy } from 'react';
import { type LoaderFunctionArgs, redirect, useLoaderData, useRevalidator } from 'react-router-dom';

import { DetailPageLayout } from '@/features/crud';
import { fetchGetCustomerSubAccountDetail, fetchUpdateCustomerSubAccount } from '@/service/api';

import SubAccountDetailView from './modules/SubAccountDetailView';

const SubAccountOperateDrawer = lazy(() => import('../modules/SubAccountOperateDrawer'));

const LIST_PATH = '/master/customer/sub-accounts';

export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) {
    return redirect(LIST_PATH);
  }

  try {
    const detail = await fetchGetCustomerSubAccountDetail(id);
    if (!detail) {
      return redirect(LIST_PATH);
    }
    return detail;
  } catch {
    return redirect(LIST_PATH);
  }
}

const SubAccountDetail = () => {
  const { t } = useTranslation();
  const detail = useLoaderData() as Api.CustomerSubAccount.Entity;
  const revalidator = useRevalidator();
  const [form] = AForm.useForm<Api.CustomerSubAccount.UpdateParams>();
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  const title = detail.nickName || detail.username || t('page.customer.subAccount.detail.title');

  function openEdit() {
    form.setFieldsValue({ ...detail });
    setDrawerOpen(true);
  }

  async function handleSubmit() {
    const values = await form.validateFields();
    setSubmitting(true);
    try {
      await fetchUpdateCustomerSubAccount({ ...values, id: detail.id });
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
        backLabel={t('page.customer.subAccount.detail.back')}
        listPath={LIST_PATH}
        title={title}
        banner={
          <span>
            {t('page.customer.subAccount.username')}：{detail.username}
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
        <SubAccountDetailView detail={detail} />
      </DetailPageLayout>

      <Suspense>
        <SubAccountOperateDrawer
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

export default SubAccountDetail;
