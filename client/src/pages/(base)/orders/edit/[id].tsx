import { type LoaderFunctionArgs, redirect, useLoaderData } from 'react-router-dom';

import { useCloseTabAndNavigate } from '@/features/tab';
import { fetchGetOrderDetail, fetchUpdateOrder } from '@/service/api';
import { SaleOrderStatus } from '@/service/enums';

import OrderOperateForm from './modules/OrderOperateForm';
import { normalizeOrderPayload, toOrderFormValues } from './modules/order-form-utils';

const LIST_PATH = '/orders/list';

/** 编辑页首屏：按路由 id 拉取订单详情，失败回列表 */
export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) {
    return redirect(LIST_PATH);
  }

  try {
    const detail = await fetchGetOrderDetail(id);
    if (!detail) {
      return redirect(LIST_PATH);
    }

    // 仅待审核 / 已驳回允许编辑
    if (detail.orderStatus !== SaleOrderStatus.PENDING_AUDIT && detail.orderStatus !== SaleOrderStatus.REJECTED) {
      return redirect(`/orders/detail/${id}`);
    }

    return detail;
  } catch {
    return redirect(LIST_PATH);
  }
}

const OrderEditPage = () => {
  const { t } = useTranslation();
  const detail = useLoaderData() as Api.Order.Entity;
  const closeTabAndNavigate = useCloseTabAndNavigate();
  const [form] = AForm.useForm<Api.Order.FormValues>();
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    form.setFieldsValue(toOrderFormValues(detail) as never);
  }, [detail, form]);

  async function handleSubmit() {
    try {
      const values = await form.validateFields();
      setSubmitting(true);
      try {
        await fetchUpdateOrder(normalizeOrderPayload(values, { id: detail.id }) as Api.Order.UpdateParams);
        window.$message?.success(t('common.updateSuccess'));
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
        title={t('page.order.operate.editTitle')}
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
      <OrderOperateForm form={form} />
    </div>
  );
};

export default OrderEditPage;
