import { useCloseTabAndNavigate } from '@/features/tab';
import { fetchAddOrder } from '@/service/api';

import OrderOperateForm from './modules/OrderOperateForm';
import { createDefaultOrderFormValues } from './modules/create-default-order-form-values';
import { normalizeOrderPayload } from './modules/order-form-utils';

const LIST_PATH = '/orders/list';

const OrderCreatePage = () => {
  const { t } = useTranslation();
  const closeTabAndNavigate = useCloseTabAndNavigate();
  const [form] = AForm.useForm<Api.Order.FormValues>();
  const [submitting, setSubmitting] = useState(false);
  const defaultValues = useMemo(() => createDefaultOrderFormValues(), []);

  async function handleSubmit() {
    try {
      const values = await form.validateFields();
      setSubmitting(true);
      try {
        await fetchAddOrder(normalizeOrderPayload(values) as Api.Order.CreateParams);
        window.$message?.success(t('common.addSuccess'));
        closeTabAndNavigate(LIST_PATH);
      } finally {
        setSubmitting(false);
      }
    } catch {
      // 表单校验失败时不提交
    }
  }

  return (
    <div className="h-full min-h-500px flex-col-stretch gap-16px overflow-auto">
      <ACard
        className="card-wrapper"
        styles={{ body: { display: 'none' } }}
        title={t('page.order.operate.addTitle')}
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
      <OrderOperateForm
        form={form}
        initialValues={defaultValues}
      />
    </div>
  );
};

export default OrderCreatePage;
