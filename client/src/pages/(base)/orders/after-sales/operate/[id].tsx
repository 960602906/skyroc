import { type LoaderFunctionArgs, redirect, useLoaderData } from 'react-router-dom';

import { OperatePageLayout } from '@/features/crud';
import { useCloseTabAndNavigate } from '@/features/tab';
import { fetchGetAfterSaleDetail, fetchGetOrderDetail, fetchUpdateAfterSale } from '@/service/api';
import { AfterSaleStatus } from '@/service/enums';

import AfterSaleOperateForm, {
  type AfterSaleFormValues,
  type AfterSaleGoodsFormItem
} from './modules/AfterSaleOperateForm';
import { toAfterSaleGoodsPayload } from './modules/after-sale-form-utils';

const LIST_PATH = '/orders/after-sales';

export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) return redirect(LIST_PATH);
  try {
    const afterSale = await fetchGetAfterSaleDetail(id);
    if (!afterSale || afterSale.afterStatus !== AfterSaleStatus.DRAFT || !afterSale.saleOrderId)
      return redirect(LIST_PATH);
    const order = await fetchGetOrderDetail(afterSale.saleOrderId);
    if (!order) return redirect(LIST_PATH);
    return { afterSale, order };
  } catch {
    return redirect(LIST_PATH);
  }
}

function toGoods(goods: Api.AfterSale.Goods[]): AfterSaleGoodsFormItem[] {
  return goods.map(item => ({
    ...item,
    enabled: true,
    maxQuantity: item.actualRefundQuantity,
    reasonType: item.reasonType
  }));
}

const AfterSaleEditPage = () => {
  const { t } = useTranslation();
  const { afterSale, order } = useLoaderData() as { afterSale: Api.AfterSale.Entity; order: Api.Order.Entity };
  const closeTabAndNavigate = useCloseTabAndNavigate();
  const [form] = AForm.useForm<AfterSaleFormValues>();
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    form.setFieldsValue({
      contactName: afterSale.contactName,
      contactPhone: afterSale.contactPhone,
      goods: toGoods(afterSale.goods),
      pickupAddress: afterSale.pickupAddress,
      remark: afterSale.remark,
      saleOrderId: afterSale.saleOrderId ?? undefined,
      source: afterSale.source
    });
  }, [afterSale, form]);

  async function handleSubmit() {
    try {
      const values = await form.validateFields();
      const goods = toAfterSaleGoodsPayload(values.goods);
      if (!goods.length) throw new Error('empty-goods');
      setSubmitting(true);
      try {
        await fetchUpdateAfterSale({
          contactName: values.contactName,
          contactPhone: values.contactPhone,
          goods,
          id: afterSale.id,
          pickupAddress: values.pickupAddress,
          remark: values.remark
        });
        window.$message?.success(t('common.updateSuccess'));
        closeTabAndNavigate(LIST_PATH);
      } finally {
        setSubmitting(false);
      }
    } catch {
      // 表单校验失败或用户未选择售后商品时不提交
    }
  }

  return (
    <OperatePageLayout
      listPath={LIST_PATH}
      loading={submitting}
      title={t('page.afterSale.operate.editTitle')}
      onSave={handleSubmit}
    >
      <AfterSaleOperateForm
        form={form}
        orderNo={order.orderNo}
        sourceEditable={false}
      />
    </OperatePageLayout>
  );
};

export default AfterSaleEditPage;
