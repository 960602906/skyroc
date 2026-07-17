import { fetchAddCustomer } from '@/service/api';

import CustomerOperateForm from './modules/CustomerOperateForm';
import { normalizeCustomerPayload } from './modules/customer-form-utils';

const CustomerCreatePage = () => {
  const { t } = useTranslation();

  const nav = useNavigate();

  const [form] = AForm.useForm<Api.Customer.CreateParams>();

  async function handleSubmit() {
    const values = await form.validateFields();
    await fetchAddCustomer(normalizeCustomerPayload(values));
    window.$message?.success(t('common.updateSuccess'));
    nav('/master/customer/list');
  }

  return (
    <ACard
      className="h-full"
      title={t('page.customer.list.addCustomer')}
      extra={
        <AFlex gap={12}>
          <AButton onClick={() => nav('/master/customer/list')}>{t('common.cancel')}</AButton>
          <AButton
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

export default CustomerCreatePage;
