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
    <div className="h-full min-h-500px flex-col-stretch gap-16px overflow-auto">
      <ACard
        className="card-wrapper"
        styles={{ body: { display: 'none' } }}
        title={t('page.purchase.order.add')}
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
      <PurchaseOrderOperateForm
        form={form}
        initialValues={defaultValues}
      />
    </div>
  );
};

export default PurchaseOrderCreatePage;
