import dayjs from 'dayjs';
import { type LoaderFunctionArgs, redirect, useLoaderData } from 'react-router-dom';

import { OperatePageLayout } from '@/features/crud';
import { useCloseTabAndNavigate } from '@/features/tab';
import { fetchGetStockInSalesReturnDetail, fetchUpdateStockInSalesReturn } from '@/service/api';
import { StockDocumentStatus } from '@/service/enums';

import type {
  SalesReturnStockInDetailFormValue,
  SalesReturnStockInFormValue
} from './modules/SalesReturnStockInOperateForm';
import SalesReturnStockInOperateForm from './modules/SalesReturnStockInOperateForm';

const LIST_PATH = '/storage/in/sales-return';

/** 路由切换前加载销售退货入库数据，非草稿或不存在时重定向列表。 */
export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) return redirect(LIST_PATH);
  try {
    const detail = await fetchGetStockInSalesReturnDetail(id);
    if (!detail || detail.businessStatus !== StockDocumentStatus.DRAFT) return redirect(LIST_PATH);
    return detail;
  } catch {
    return redirect(LIST_PATH);
  }
}

/** 把表单明细行转换为接口 payload（编辑时透传 id） */
function toDetailPayload(detail: SalesReturnStockInDetailFormValue) {
  const base = {
    expireDate: detail.expireDate ? dayjs(detail.expireDate).format('YYYY-MM-DD') : null,
    goodsId: detail.goodsId,
    goodsUnitId: detail.goodsUnitId,
    pickupTaskId: detail.pickupTaskId || null,
    productDate: detail.productDate ? dayjs(detail.productDate).format('YYYY-MM-DD') : null,
    quantity: detail.quantity,
    remark: detail.remark || null,
    unitPrice: detail.unitPrice
  };
  return detail.id ? { id: detail.id, ...base } : base;
}

/** 把 API 实体转换为表单回显值（任务号直接取接口字段） */
function toFormValue(detail: Api.StockIn.Entity): SalesReturnStockInFormValue {
  return {
    afterSaleId: detail.afterSaleId,
    customerId: detail.customerId || '',
    customerName: detail.customerName || undefined,
    departmentId: detail.departmentId,
    details: detail.details.map(item => ({
      expireDate: item.expireDate ? dayjs(item.expireDate) : undefined,
      goodsCode: item.goodsCode,
      goodsId: item.goodsId,
      goodsName: item.goodsName,
      goodsUnitId: item.goodsUnitId,
      goodsUnitName: item.goodsUnitName,
      id: item.id,
      pickupTaskId: item.pickupTaskId,
      productDate: item.productDate ? dayjs(item.productDate) : undefined,
      quantity: item.quantity,
      remark: item.remark,
      taskNo: item.pickupTaskNo || undefined,
      unitPrice: item.unitPrice
    })),
    id: detail.id,
    inTime: detail.inTime ? dayjs(detail.inTime) : '',
    remark: detail.remark,
    wareId: detail.wareId
  };
}

/** 销售退货入库编辑页 */
const SalesReturnStockInEditPage = () => {
  const { t } = useTranslation();
  const detail = useLoaderData() as Api.StockIn.Entity;
  const closeTabAndNavigate = useCloseTabAndNavigate();
  const [form] = AForm.useForm<SalesReturnStockInFormValue>();
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    form.setFieldsValue(toFormValue(detail) as unknown as SalesReturnStockInFormValue);
  }, [detail, form]);

  async function handleSubmit() {
    try {
      const values = await form.validateFields();
      const payload: Api.StockIn.UpdateSalesReturnPayload = {
        afterSaleId: values.afterSaleId || null,
        customerId: values.customerId,
        departmentId: values.departmentId || null,
        details: values.details.map(toDetailPayload),
        id: detail.id,
        inTime: values.inTime ? dayjs(values.inTime).toISOString() : '',
        remark: values.remark || null,
        wareId: values.wareId
      };
      setSubmitting(true);
      try {
        await fetchUpdateStockInSalesReturn(payload);
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
    <OperatePageLayout
      listPath={LIST_PATH}
      loading={submitting}
      title={t('page.storage.in.salesReturn.edit')}
      onSave={handleSubmit}
    >
      <AForm
        form={form}
        layout="vertical"
      >
        <SalesReturnStockInOperateForm
          editMode
          form={form}
        />
      </AForm>
    </OperatePageLayout>
  );
};

export default SalesReturnStockInEditPage;
