import { useQuery } from '@tanstack/react-query';

import { useCloseTabAndNavigate } from '@/features/tab';
import { fetchAddAfterSale, fetchGetOrderDetail, fetchGetOrderList } from '@/service/api';
import { AfterSaleHandleType, AfterSaleReasonType, AfterSaleType } from '@/service/enums';
import { formatField } from '@/utils/common';

import AfterSaleOperateForm, {
  type AfterSaleFormValues,
  type AfterSaleGoodsFormItem
} from './modules/AfterSaleOperateForm';
import { toAfterSaleGoodsPayload } from './modules/after-sale-form-utils';

const LIST_PATH = '/orders/after-sales';

function toGoods(order: Api.Order.Entity): AfterSaleGoodsFormItem[] {
  return (order.details ?? []).map(detail => ({
    actualRefundQuantity: detail.quantity,
    afterSaleType: AfterSaleType.REFUND_ONLY,
    enabled: true,
    goodsCode: detail.goodsCode,
    goodsName: detail.goodsName,
    goodsUnitName: detail.goodsUnitName,
    handleType: AfterSaleHandleType.GOODS_DISCOUNT,
    maxQuantity: detail.quantity,
    reasonType: AfterSaleReasonType.OTHER,
    remark: null,
    saleOrderDetailId: detail.id
  }));
}

const AfterSaleCreatePage = () => {
  const { t } = useTranslation();
  const closeTabAndNavigate = useCloseTabAndNavigate();
  const [form] = AForm.useForm<AfterSaleFormValues>();
  const [submitting, setSubmitting] = useState(false);
  const { data: orderList } = useQuery({
    queryFn: () => fetchGetOrderList({ current: 1, size: 100 }),
    queryKey: ['after-sale-source-orders']
  });

  const orderOptions = useMemo(
    () =>
      (orderList?.records ?? []).map(order => ({
        label: formatField(order, ['orderNo', 'customerName'], ' · '),
        value: order.id
      })),
    [orderList]
  );

  async function handleSaleOrderChange(saleOrderId: string) {
    const order = await fetchGetOrderDetail(saleOrderId);
    if (!order) return;
    form.setFieldsValue({
      contactName: order.contactName,
      contactPhone: order.contactPhone,
      goods: toGoods(order),
      pickupAddress: order.deliveryAddress,
      saleOrderId,
      source: t('page.afterSale.operate.source')
    });
  }

  async function handleSubmit() {
    try {
      const values = await form.validateFields();
      const goods = toAfterSaleGoodsPayload(values.goods);
      if (!goods.length) throw new Error('empty-goods');
      setSubmitting(true);
      try {
        await fetchAddAfterSale({ ...values, goods });
        window.$message?.success(t('common.addSuccess'));
        closeTabAndNavigate(LIST_PATH);
      } finally {
        setSubmitting(false);
      }
    } catch {
      // 表单校验失败或用户未选择售后商品时不提交
    }
  }

  return (
    <div className="h-full min-h-500px flex-col-stretch gap-16px overflow-auto">
      <ACard
        className="card-wrapper"
        styles={{ body: { display: 'none' } }}
        title={t('page.afterSale.operate.addTitle')}
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
      <AfterSaleOperateForm
        sourceEditable
        form={form}
        orderOptions={orderOptions}
        onSaleOrderChange={handleSaleOrderChange}
      />
    </div>
  );
};

export default AfterSaleCreatePage;
