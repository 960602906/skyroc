import { useEffect } from 'react';
import { useParams } from 'react-router-dom';

import { fetchGetCustomerDetail, fetchUpdateCustomer } from '@/service/api';

import CustomerOperateForm from './modules/CustomerOperateForm';
import { normalizeCustomerPayload, toCustomerFormValues } from './modules/customer-form-utils';

const CustomerEditPage = () => {
  const { t } = useTranslation();

  const nav = useNavigate();

  const { id } = useParams();

  const [form] = AForm.useForm<Api.Customer.UpdateParams>();

  const [loading, setLoading] = useState(false);

  useEffect(() => {
    async function loadDetail() {
      if (!id) return;

      setLoading(true);

      try {
        const detail = await fetchGetCustomerDetail(id);
        if (detail) {
          form.setFieldsValue(toCustomerFormValues(detail) as never);
        }
      } finally {
        setLoading(false);
      }
    }

    loadDetail();
  }, [form, id]);

  async function handleSubmit() {
    const values = await form.validateFields();
    await fetchUpdateCustomer({
      ...normalizeCustomerPayload(values),
      id: id!
    });
    window.$message?.success(t('common.updateSuccess'));
    nav('/master/customer/list');
  }

  return (
    <ACard
      className="h-full"
      loading={loading}
      title={t('page.customer.list.editCustomer')}
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

export default CustomerEditPage;
