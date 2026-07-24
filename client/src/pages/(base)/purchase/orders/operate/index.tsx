import { OperatePageLayout } from '@/features/crud';
import { useCloseTabAndNavigate } from '@/features/tab';
import { fetchAddPurchaseOrder } from '@/service/api';
import { PurchasePattern } from '@/service/enums';

import PurchaseOrderOperateForm from './modules/PurchaseOrderOperateForm';
import { normalizePurchaseOrderCreatePayload } from './modules/purchase-order-form-utils';
import type { PurchaseOrderFormValues } from './modules/purchase-order-form-utils';

const LIST_PATH = '/purchase/orders';

const defaultValues: PurchaseOrderFormValues = {
  details: [],
  purchasePattern: PurchasePattern.SUPPLIER_DIRECT,
  purchaserId: null,
  receiveTime: null,
  remark: null,
  supplierContactName: null,
  supplierContactPhone: null,
  supplierId: null
};

const PurchaseOrderCreatePage = () => {
  const { t } = useTranslation();
  const closeTabAndNavigate = useCloseTabAndNavigate();
  const [form] = AForm.useForm<PurchaseOrderFormValues>();
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit() {
    try {
      const values = await form.validateFields();
      setSubmitting(true);
      try {
        await fetchAddPurchaseOrder(normalizePurchaseOrderCreatePayload(values));
        window.$message?.success(t('common.addSuccess'));
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
      title={t('page.purchase.order.add')}
      onSave={handleSubmit}
    >
      <PurchaseOrderOperateForm
        form={form}
        initialValues={defaultValues}
      />
    </OperatePageLayout>
  );
};

export default PurchaseOrderCreatePage;
