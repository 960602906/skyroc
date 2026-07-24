import dayjs from 'dayjs';

import { OperatePageLayout } from '@/features/crud';
import { useCloseTabAndNavigate } from '@/features/tab';
import { fetchAddStockInPurchase } from '@/service/api';
import { PurchasePattern } from '@/service/enums';
import { toBackendDate, toBackendDateTime } from '@/utils/datetime';

import type { PurchaseStockInDetailFormValue, PurchaseStockInFormValue } from './modules/PurchaseStockInOperateForm';
import PurchaseStockInOperateForm from './modules/PurchaseStockInOperateForm';

const LIST_PATH = '/storage/in/purchase';

/** 把表单明细行转换为接口 payload */
function toDetailPayload(detail: PurchaseStockInDetailFormValue, isEdit: boolean) {
  const base = {
    expireDate: toBackendDate(detail.expireDate),
    goodsId: detail.goodsId,
    goodsUnitId: detail.goodsUnitId,
    productDate: toBackendDate(detail.productDate),
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
        expectedArrivalTime: toBackendDateTime(values.expectedArrivalTime),
        inTime: toBackendDateTime(values.inTime) || '',
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
    <OperatePageLayout
      listPath={LIST_PATH}
      loading={submitting}
      title={t('page.storage.in.purchase.add')}
      onSave={handleSubmit}
    >
      <AForm
        form={form}
        initialValues={{ details: [], inTime: dayjs(), purchasePattern: PurchasePattern.SUPPLIER_DIRECT }}
        layout="vertical"
      >
        <PurchaseStockInOperateForm form={form} />
      </AForm>
    </OperatePageLayout>
  );
};

export default PurchaseStockInCreatePage;
