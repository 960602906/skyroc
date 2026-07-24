import dayjs from 'dayjs';

import { OperatePageLayout } from '@/features/crud';
import { useCloseTabAndNavigate } from '@/features/tab';
import { fetchAddStockInOther } from '@/service/api';

import type { OtherStockInDetailFormValue, OtherStockInFormValue } from './modules/OtherStockInOperateForm';
import OtherStockInOperateForm from './modules/OtherStockInOperateForm';

const LIST_PATH = '/storage/in/other';

/** 把表单明细行转换为接口 payload */
function toDetailPayload(detail: OtherStockInDetailFormValue) {
  return {
    expireDate: detail.expireDate ? dayjs(detail.expireDate).format('YYYY-MM-DD') : null,
    goodsId: detail.goodsId,
    goodsUnitId: detail.goodsUnitId,
    productDate: detail.productDate ? dayjs(detail.productDate).format('YYYY-MM-DD') : null,
    quantity: detail.quantity,
    remark: detail.remark || null,
    unitPrice: detail.unitPrice
  };
}

/** 其他入库新增页 */
const OtherStockInCreatePage = () => {
  const { t } = useTranslation();
  const closeTabAndNavigate = useCloseTabAndNavigate();
  const [form] = AForm.useForm<OtherStockInFormValue>();
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit() {
    try {
      const values = await form.validateFields();
      const payload: Api.StockIn.CreateOtherPayload = {
        departmentId: values.departmentId || null,
        details: values.details.map(toDetailPayload),
        inTime: values.inTime ? dayjs(values.inTime).toISOString() : '',
        remark: values.remark || null,
        wareId: values.wareId
      };
      setSubmitting(true);
      try {
        await fetchAddStockInOther(payload);
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
      title={t('page.storage.in.other.add')}
      onSave={handleSubmit}
    >
      <AForm
        form={form}
        initialValues={{ details: [], inTime: dayjs() }}
        layout="vertical"
      >
        <OtherStockInOperateForm form={form} />
      </AForm>
    </OperatePageLayout>
  );
};

export default OtherStockInCreatePage;
