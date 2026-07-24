import dayjs from 'dayjs';
import { type LoaderFunctionArgs, redirect, useLoaderData } from 'react-router-dom';

import { OperatePageLayout } from '@/features/crud';
import { useCloseTabAndNavigate } from '@/features/tab';
import { fetchGetStockInOtherDetail, fetchUpdateStockInOther } from '@/service/api';
import { StockDocumentStatus } from '@/service/enums';

import type { OtherStockInDetailFormValue, OtherStockInFormValue } from './modules/OtherStockInOperateForm';
import OtherStockInOperateForm from './modules/OtherStockInOperateForm';

const LIST_PATH = '/storage/in/other';

/** 路由切换前加载其他入库数据，非草稿或不存在时重定向列表。 */
export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) return redirect(LIST_PATH);
  try {
    const detail = await fetchGetStockInOtherDetail(id);
    if (!detail || detail.businessStatus !== StockDocumentStatus.DRAFT) return redirect(LIST_PATH);
    return detail;
  } catch {
    return redirect(LIST_PATH);
  }
}

/** 把表单明细行转换为接口 payload（编辑时透传 id） */
function toDetailPayload(detail: OtherStockInDetailFormValue) {
  const base = {
    expireDate: detail.expireDate ? dayjs(detail.expireDate).format('YYYY-MM-DD') : null,
    goodsId: detail.goodsId,
    goodsUnitId: detail.goodsUnitId,
    productDate: detail.productDate ? dayjs(detail.productDate).format('YYYY-MM-DD') : null,
    quantity: detail.quantity,
    remark: detail.remark || null,
    unitPrice: detail.unitPrice
  };
  return detail.id ? { id: detail.id, ...base } : base;
}

/** 把 API 实体转换为表单回显值（日期字段转 Dayjs） */
function toFormValue(detail: Api.StockIn.Entity): OtherStockInFormValue {
  return {
    departmentId: detail.departmentId,
    details: detail.details.map(item => ({
      expireDate: item.expireDate ? dayjs(item.expireDate) : undefined,
      goodsCode: item.goodsCode,
      goodsId: item.goodsId,
      goodsName: item.goodsName,
      goodsUnitId: item.goodsUnitId,
      goodsUnitName: item.goodsUnitName,
      id: item.id,
      productDate: item.productDate ? dayjs(item.productDate) : undefined,
      quantity: item.quantity,
      remark: item.remark,
      unitPrice: item.unitPrice
    })),
    id: detail.id,
    inTime: detail.inTime ? dayjs(detail.inTime) : '',
    remark: detail.remark,
    wareId: detail.wareId
  };
}

/** 其他入库编辑页 */
const OtherStockInEditPage = () => {
  const { t } = useTranslation();
  const detail = useLoaderData() as Api.StockIn.Entity;
  const closeTabAndNavigate = useCloseTabAndNavigate();
  const [form] = AForm.useForm<OtherStockInFormValue>();
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    form.setFieldsValue(toFormValue(detail) as unknown as OtherStockInFormValue);
  }, [detail, form]);

  async function handleSubmit() {
    try {
      const values = await form.validateFields();
      const payload: Api.StockIn.UpdateOtherPayload = {
        departmentId: values.departmentId || null,
        details: values.details.map(toDetailPayload),
        id: detail.id,
        inTime: values.inTime ? dayjs(values.inTime).toISOString() : '',
        remark: values.remark || null,
        wareId: values.wareId
      };
      setSubmitting(true);
      try {
        await fetchUpdateStockInOther(payload);
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
      title={t('page.storage.in.other.edit')}
      onSave={handleSubmit}
    >
      <AForm
        form={form}
        layout="vertical"
      >
        <OtherStockInOperateForm form={form} />
      </AForm>
    </OperatePageLayout>
  );
};

export default OtherStockInEditPage;
