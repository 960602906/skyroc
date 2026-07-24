import { OperatePageLayout } from '@/features/crud';
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
    <OperatePageLayout
      listPath={LIST_PATH}
      loading={submitting}
      title={t('page.order.operate.addTitle')}
      onSave={handleSubmit}
    >
      <OrderOperateForm
        form={form}
        initialValues={defaultValues}
      />
    </OperatePageLayout>
  );
};

export default OrderCreatePage;
