import { type LoaderFunctionArgs, redirect, useLoaderData } from 'react-router-dom';

import { OperatePageLayout } from '@/features/crud';
import { useCloseTabAndNavigate } from '@/features/tab';
import { fetchGetCustomerDetail, fetchUpdateCustomer } from '@/service/api';

import CustomerOperateForm from './modules/CustomerOperateForm';
import { normalizeCustomerPayload, toCustomerFormValues } from './modules/customer-form-utils';

const LIST_PATH = '/master/customer/list';

/** 编辑页首屏：按路由 id 拉取客户详情，失败回列表 */
export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) {
    return redirect(LIST_PATH);
  }

  try {
    const detail = await fetchGetCustomerDetail(id);
    if (!detail) {
      return redirect(LIST_PATH);
    }
    return detail;
  } catch {
    return redirect(LIST_PATH);
  }
}

const CustomerEditPage = () => {
  const { t } = useTranslation();
  const detail = useLoaderData() as Api.Customer.Entity;
  const closeTabAndNavigate = useCloseTabAndNavigate();
  const [form] = AForm.useForm<Api.Customer.UpdateParams>();
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    form.setFieldsValue(toCustomerFormValues(detail) as never);
  }, [detail, form]);

  async function handleSubmit() {
    const values = await form.validateFields();
    setSubmitting(true);
    try {
      await fetchUpdateCustomer({
        ...normalizeCustomerPayload(values),
        id: detail.id
      });
      window.$message?.success(t('common.updateSuccess'));
      closeTabAndNavigate(LIST_PATH);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <OperatePageLayout
      listPath={LIST_PATH}
      loading={submitting}
      title={t('page.customer.operate.editTitle')}
      onSave={handleSubmit}
    >
      <CustomerOperateForm form={form} />
    </OperatePageLayout>
  );
};

export default CustomerEditPage;
