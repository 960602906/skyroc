import { OperatePageLayout } from '@/features/crud';
import { useCloseTabAndNavigate } from '@/features/tab';
import { fetchAddCustomer } from '@/service/api';

import CustomerOperateForm from './modules/CustomerOperateForm';
import { createDefaultCustomerFormValues } from './modules/create-default-customer-form-values';
import { normalizeCustomerPayload } from './modules/customer-form-utils';

const LIST_PATH = '/master/customer/list';

const CustomerCreatePage = () => {
  const { t } = useTranslation();
  const closeTabAndNavigate = useCloseTabAndNavigate();
  const [form] = AForm.useForm<Api.Customer.CreateParams>();
  const [submitting, setSubmitting] = useState(false);

  useMount(() => {
    form.setFieldsValue(createDefaultCustomerFormValues());
  });

  async function handleSubmit() {
    const values = await form.validateFields();
    setSubmitting(true);
    try {
      await fetchAddCustomer(normalizeCustomerPayload(values));
      window.$message?.success(t('common.addSuccess'));
      closeTabAndNavigate(LIST_PATH);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <OperatePageLayout
      listPath={LIST_PATH}
      loading={submitting}
      title={t('page.customer.operate.addTitle')}
      onSave={handleSubmit}
    >
      <CustomerOperateForm form={form} />
    </OperatePageLayout>
  );
};

export default CustomerCreatePage;
