import { type LoaderFunctionArgs, redirect, useLoaderData } from 'react-router-dom';

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
    <div className="h-full min-h-500px flex-col-stretch gap-16px overflow-auto">
      <ACard
        className="card-wrapper"
        styles={{ body: { display: 'none' } }}
        title={t('page.customer.operate.editTitle')}
        variant="borderless"
        extra={
          <ASpace>
            <AButton onClick={() => closeTabAndNavigate(LIST_PATH)}>{t('common.cancel')}</AButton>
            <AButton
              loading={submitting}
              type="primary"
              onClick={handleSubmit}
            >
              {t('common.confirm')}
            </AButton>
          </ASpace>
        }
      />
      <CustomerOperateForm form={form} />
    </div>
  );
};

export default CustomerEditPage;
