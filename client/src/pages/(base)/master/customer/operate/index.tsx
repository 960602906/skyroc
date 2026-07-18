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
    <div className="h-full min-h-500px flex-col-stretch gap-16px overflow-auto">
      <ACard
        className="card-wrapper"
        styles={{ body: { display: 'none' } }}
        title={t('page.customer.operate.addTitle')}
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

export default CustomerCreatePage;
