import dayjs from 'dayjs';

import { OperatePageLayout } from '@/features/crud';
import { useCloseTabAndNavigate } from '@/features/tab';
import { fetchAddStockInSalesReturn } from '@/service/api';

import type {
  SalesReturnStockInDetailFormValue,
  SalesReturnStockInFormValue
} from './modules/SalesReturnStockInOperateForm';
import SalesReturnStockInOperateForm from './modules/SalesReturnStockInOperateForm';

const LIST_PATH = '/storage/in/sales-return';

/** 把表单明细行转换为接口 payload */
function toDetailPayload(detail: SalesReturnStockInDetailFormValue) {
  return {
    expireDate: detail.expireDate ? dayjs(detail.expireDate).format('YYYY-MM-DD') : null,
    goodsId: detail.goodsId,
    goodsUnitId: detail.goodsUnitId,
    pickupTaskId: detail.pickupTaskId || null,
    productDate: detail.productDate ? dayjs(detail.productDate).format('YYYY-MM-DD') : null,
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
        inTime: values.inTime ? dayjs(values.inTime).toISOString() : '',
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
