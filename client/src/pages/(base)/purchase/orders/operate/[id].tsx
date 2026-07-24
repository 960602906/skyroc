import { type LoaderFunctionArgs, redirect, useLoaderData } from 'react-router-dom';

import { OperatePageLayout } from '@/features/crud';
import { useCloseTabAndNavigate } from '@/features/tab';
import { fetchGetPurchaseOrderDetail, fetchUpdatePurchaseOrder } from '@/service/api';
import { PurchaseOrderStatus } from '@/service/enums';

import PurchaseOrderOperateForm from './modules/PurchaseOrderOperateForm';
import { normalizePurchaseOrderUpdatePayload, toPurchaseOrderFormValues } from './modules/purchase-order-form-utils';
import type { PurchaseOrderFormValues } from './modules/purchase-order-form-utils';

const LIST_PATH = '/purchase/orders';

/** 编辑页首屏：按路由 id 加载采购单，非草稿重定向详情页 */
export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) return redirect(LIST_PATH);

  try {
    const detail = await fetchGetPurchaseOrderDetail(id);
    if (!detail) return redirect(LIST_PATH);
    if (detail.businessStatus !== PurchaseOrderStatus.DRAFT) {
      return redirect(`/purchase/orders/detail/${id}`);
    }
    return detail;
  } catch {
    return redirect(LIST_PATH);
  }
}

const PurchaseOrderEditPage = () => {
  const { t } = useTranslation();
  const detail = useLoaderData() as Api.PurchaseOrder.Entity;
  const closeTabAndNavigate = useCloseTabAndNavigate();
  const [form] = AForm.useForm<PurchaseOrderFormValues>();
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    form.setFieldsValue(toPurchaseOrderFormValues(detail) as never);
  }, [detail, form]);

  async function handleSubmit() {
    try {
      const values = await form.validateFields();
      setSubmitting(true);
      try {
        await fetchUpdatePurchaseOrder(normalizePurchaseOrderUpdatePayload(values));
        window.$message?.success(t('common.updateSuccess'));
        closeTabAndNavigate(LIST_PATH);
      } finally {
        setSubmitting(false);
      }
    } catch {
      // 表单校验失败
    }
  }

  return (
    <OperatePageLayout
      listPath={LIST_PATH}
      loading={submitting}
      title={t('page.purchase.order.edit')}
      onSave={handleSubmit}
    >
      <PurchaseOrderOperateForm form={form} />
    </OperatePageLayout>
  );
};

export default PurchaseOrderEditPage;
