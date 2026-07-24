import dayjs from 'dayjs';
import { type LoaderFunctionArgs, redirect, useLoaderData } from 'react-router-dom';

import { OperatePageLayout } from '@/features/crud';
import { useCloseTabAndNavigate } from '@/features/tab';
import { fetchGetStockInPurchaseDetail, fetchUpdateStockInPurchase } from '@/service/api';
import { StockDocumentStatus } from '@/service/enums';

import type { PurchaseStockInDetailFormValue, PurchaseStockInFormValue } from './modules/PurchaseStockInOperateForm';
import PurchaseStockInOperateForm from './modules/PurchaseStockInOperateForm';

const LIST_PATH = '/storage/in/purchase';

/** 路由元信息：编辑采购入库页不显示在菜单中 */
export const handle = {
  hideInMenu: true,
  i18nKey: 'route.(base)_storage_in_purchase_operate_[id]',
  keepAlive: false
};

/** 路由切换前加载采购入库数据，非草稿或不存在时重定向列表。 */
export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) return redirect(LIST_PATH);
  try {
    const detail = await fetchGetStockInPurchaseDetail(id);
    if (!detail || detail.businessStatus !== StockDocumentStatus.DRAFT) return redirect(LIST_PATH);
    return detail;
  } catch {
    return redirect(LIST_PATH);
  }
}

/** 把表单明细行转换为接口 payload（编辑时透传 id） */
function toDetailPayload(detail: PurchaseStockInDetailFormValue) {
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
  return detail.id ? { id: detail.id, ...base } : base;
}

/** 把 API 实体转换为表单回显值（日期字段转 Dayjs） */
function toFormValue(detail: Api.StockIn.Entity): PurchaseStockInFormValue {
  return {
    departmentId: detail.departmentId,
    details: detail.details.map(item => ({
      ...item,
      expireDate: item.expireDate ? dayjs(item.expireDate) : undefined,
      id: item.id,
      productDate: item.productDate ? dayjs(item.productDate) : undefined
    })),
    expectedArrivalTime: detail.expectedArrivalTime ? dayjs(detail.expectedArrivalTime) : undefined,
    id: detail.id,
    inTime: detail.inTime ? dayjs(detail.inTime) : '',
    purchaseOrderId: detail.purchaseOrderId,
    purchasePattern: detail.purchasePattern as Api.StockIn.PurchasePattern,
    purchaserId: detail.purchaserId,
    remark: detail.remark,
    supplierId: detail.supplierId,
    wareId: detail.wareId
  };
}

/** 采购入库编辑页 */
const PurchaseStockInEditPage = () => {
  const { t } = useTranslation();
  const detail = useLoaderData() as Api.StockIn.Entity;
  const closeTabAndNavigate = useCloseTabAndNavigate();
  const [form] = AForm.useForm<PurchaseStockInFormValue>();
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    form.setFieldsValue(toFormValue(detail) as unknown as PurchaseStockInFormValue);
  }, [detail, form]);

  async function handleSubmit() {
    try {
      const values = await form.validateFields();
      const payload = {
        departmentId: values.departmentId || null,
        details: values.details.map(toDetailPayload),
        expectedArrivalTime: values.expectedArrivalTime ? dayjs(values.expectedArrivalTime).toISOString() : null,
        id: detail.id,
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
        await fetchUpdateStockInPurchase(payload);
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
      title={t('page.storage.in.edit')}
      onSave={handleSubmit}
    >
      <AForm
        form={form}
        layout="vertical"
      >
        <PurchaseStockInOperateForm form={form} />
      </AForm>
    </OperatePageLayout>
  );
};

export default PurchaseStockInEditPage;
