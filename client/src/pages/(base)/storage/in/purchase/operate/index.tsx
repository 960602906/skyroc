import dayjs from 'dayjs';

import { useCloseTabAndNavigate } from '@/features/tab';
import { fetchAddStockInPurchase } from '@/service/api';
import { PurchasePattern } from '@/service/enums';

import type { PurchaseStockInDetailFormValue, PurchaseStockInFormValue } from './modules/PurchaseStockInOperateForm';
import PurchaseStockInOperateForm from './modules/PurchaseStockInOperateForm';

const LIST_PATH = '/storage/in/purchase';

/** 把表单明细行转换为接口 payload */
function toDetailPayload(detail: PurchaseStockInDetailFormValue, isEdit: boolean) {
  const base = {
    expireDate: detail.expireDate ? dayjs(detail.expireDate).format('YYYY-MM-DD') : null,
    goodsId: detail.goodsId,
    goodsUnitId: detail.goodsUnitId,
    productDate: detail.productDate ? dayjs(detail.productDate).format('YYYY-MM-DD') : null,
    purchaseOrderDetailId: detail.purchaseOrderDetailId || null,
    quantity: detail.quantity,
    remark: detail.remark || null,
    unitPrice: detail.unitPrice
  };
  return isEdit && detail.id ? { id: detail.id, ...base } : base;
}

/** 采购入库新增页 */
const PurchaseStockInCreatePage = () => {
  const { t } = useTranslation();
  const closeTabAndNavigate = useCloseTabAndNavigate();
  const [form] = AForm.useForm<PurchaseStockInFormValue>();
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit() {
    try {
      const values = await form.validateFields();
      const payload = {
        departmentId: values.departmentId || null,
        details: values.details.map(d => toDetailPayload(d, false)),
        expectedArrivalTime: values.expectedArrivalTime ? dayjs(values.expectedArrivalTime).toISOString() : null,
        inTime: values.inTime ? dayjs(values.inTime).toISOString() : '',
        purchaseOrderId: values.purchaseOrderId || null,
        purchasePattern: values.purchasePattern,
        purchaserId: values.purchaserId || null,
        remark: values.remark || null,
        supplierId: values.supplierId || null,
        wareId: values.wareId
      };
      setSubmitting(true);
      try {
        await fetchAddStockInPurchase(payload);
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
        title={t('page.storage.in.purchase.add')}
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
      <AForm
        form={form}
        initialValues={{ details: [], inTime: dayjs(), purchasePattern: PurchasePattern.SUPPLIER_DIRECT }}
        layout="vertical"
      >
        <PurchaseStockInOperateForm form={form} />
      </AForm>
    </div>
  );
};

export default PurchaseStockInCreatePage;
