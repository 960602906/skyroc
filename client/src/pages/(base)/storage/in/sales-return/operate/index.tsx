import dayjs from 'dayjs';

import { OperatePageLayout } from '@/features/crud';
import { useCloseTabAndNavigate } from '@/features/tab';
import { fetchAddStockInSalesReturn } from '@/service/api';
import { toBackendDate, toBackendDateTime } from '@/utils/datetime';

import type {
  SalesReturnStockInDetailFormValue,
  SalesReturnStockInFormValue
} from './modules/SalesReturnStockInOperateForm';
import SalesReturnStockInOperateForm from './modules/SalesReturnStockInOperateForm';

const LIST_PATH = '/storage/in/sales-return';

/** 把表单明细行转换为接口 payload */
function toDetailPayload(detail: SalesReturnStockInDetailFormValue) {
  return {
    expireDate: toBackendDate(detail.expireDate),
    goodsId: detail.goodsId,
    goodsUnitId: detail.goodsUnitId,
    pickupTaskId: detail.pickupTaskId || null,
    productDate: toBackendDate(detail.productDate),
    quantity: detail.quantity,
    remark: detail.remark || null,
    unitPrice: detail.unitPrice
  };
}

/** 销售退货入库新增页 */
const SalesReturnStockInCreatePage = () => {
  const { t } = useTranslation();
  const closeTabAndNavigate = useCloseTabAndNavigate();
  const [form] = AForm.useForm<SalesReturnStockInFormValue>();
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit() {
    try {
      const values = await form.validateFields();
      const payload: Api.StockIn.CreateSalesReturnPayload = {
        afterSaleId: values.afterSaleId || null,
        customerId: values.customerId,
        departmentId: values.departmentId || null,
        details: values.details.map(toDetailPayload),
        inTime: toBackendDateTime(values.inTime) || '',
        remark: values.remark || null,
        wareId: values.wareId
      };
      setSubmitting(true);
      try {
        await fetchAddStockInSalesReturn(payload);
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
      title={t('page.storage.in.salesReturn.add')}
      onSave={handleSubmit}
    >
      <AForm
        form={form}
        initialValues={{ details: [], inTime: dayjs() }}
        layout="vertical"
      >
        <SalesReturnStockInOperateForm form={form} />
      </AForm>
    </OperatePageLayout>
  );
};

export default SalesReturnStockInCreatePage;
