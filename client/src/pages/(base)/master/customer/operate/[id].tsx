import { type LoaderFunctionArgs, redirect, useLoaderData } from 'react-router-dom';

import { fetchGetCustomerDetail, fetchUpdateCustomer } from '@/service/api';

import CustomerOperateForm from './modules/CustomerOperateForm';
import { normalizeCustomerPayload, toCustomerFormValues } from './modules/customer-form-utils';

/** 编辑页首屏：按路由 id 拉取客户详情，失败回列表 */
export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) {
    return redirect('/master/customer/list');
  }

  try {
    const detail = await fetchGetCustomerDetail(id);
    if (!detail) {
      return redirect('/master/customer/list');
    }
    return detail;
  } catch {
    return redirect('/master/customer/list');
  }
}

const CustomerEditPage = () => {
  const { t } = useTranslation();
  const detail = useLoaderData() as Api.Customer.Entity;
  const nav = useNavigate();
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
      nav('/master/customer/list');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <ACard
      className="h-full"
      title={t('page.customer.list.editCustomer')}
      extra={
        <AFlex gap={12}>
          <AButton onClick={() => nav('/master/customer/list')}>{t('common.cancel')}</AButton>
          <AButton
            loading={submitting}
            type="primary"
            onClick={handleSubmit}
          >
            {t('common.confirm')}
          </AButton>
        </AFlex>
      }
    >
      <CustomerOperateForm form={form} />
    </ACard>
  );
};

export default CustomerEditPage;
