import { useQuery } from '@tanstack/react-query';

import RemoteOptionSelect from '@/components/RemoteOptionSelect';
import { useFormRules } from '@/features/form';
import { fetchGetGoodsDetail, fetchGetGoodsUnitsByGoods } from '@/service/api';
import { SELECTION_OPTION_RESOURCES } from '@/service/hooks';
import { QUERY_KEYS } from '@/service/keys';

type RuleKey = 'fixedGoodsUnitId' | 'fixedPrice' | 'goodsId' | 'goodsUnitId' | 'quantity';

interface OrderDetailLineModalProps {
  form: Page.FormInstance;
  onClose: () => void;
  onSubmit: () => void;
  open: boolean;
  operateType: AntDesign.TableOperateType;
}

/** 销售订单明细行弹窗：商品 / 单位 / 数量 / 单价 */
const OrderDetailLineModal: FC<OrderDetailLineModalProps> = memo(({ form, onClose, onSubmit, open, operateType }) => {
  const { t } = useTranslation();
  const { defaultRequiredRule } = useFormRules();
  const goodsId = AForm.useWatch('goodsId', form) as string | undefined;
  const { data: unitEntities = [] } = useQuery({
    enabled: open && Boolean(goodsId),
    queryFn: () => fetchGetGoodsUnitsByGoods(goodsId!),
    queryKey: QUERY_KEYS.BASE.GOODS_UNITS_BY_GOODS(goodsId ?? ''),
    staleTime: 30_000
  });

  const unitOptions = unitEntities.map(item => ({
    label: item.name ?? item.code ?? item.id,
    value: item.id
  }));

  const unitMap = useMemo(() => new Map(unitEntities.map(item => [item.id, item])), [unitEntities]);
  const prevGoodsIdRef = useRef<string | undefined>(undefined);

  // 仅在用户切换商品时清空单位，避免编辑回填被冲掉
  useEffect(() => {
    if (!open) {
      prevGoodsIdRef.current = undefined;
      return;
    }

    if (prevGoodsIdRef.current !== undefined && prevGoodsIdRef.current !== goodsId) {
      form.setFieldsValue({
        fixedGoodsUnitId: undefined,
        fixedGoodsUnitName: undefined,
        fixedUnitConversion: undefined,
        goodsUnitId: undefined,
        goodsUnitName: undefined,
        unitConversion: undefined
      });
    }

    prevGoodsIdRef.current = goodsId;
  }, [form, goodsId, open]);

  const rules: Record<RuleKey, App.Global.FormRule> = {
    fixedGoodsUnitId: defaultRequiredRule,
    fixedPrice: defaultRequiredRule,
    goodsId: defaultRequiredRule,
    goodsUnitId: defaultRequiredRule,
    quantity: defaultRequiredRule
  };

  async function handleGoodsChange(value: string) {
    const selected = await fetchGetGoodsDetail(value);
    form.setFieldsValue({
      goodsCode: selected.code,
      goodsName: selected.name
    });
  }

  function handleGoodsUnitChange(value: string) {
    const unit = unitMap.get(value);
    const patch: Record<string, unknown> = {
      goodsUnitName: unit?.name,
      unitConversion: unit?.conversionRate
    };

    // 默认单价单位与下单单位一致
    const fixedGoodsUnitId = form.getFieldValue('fixedGoodsUnitId') as string | undefined;
    if (!fixedGoodsUnitId) {
      patch.fixedGoodsUnitId = value;
      patch.fixedGoodsUnitName = unit?.name;
      patch.fixedUnitConversion = unit?.conversionRate;
    }

    form.setFieldsValue(patch);
  }

  function handleFixedGoodsUnitChange(value: string) {
    const unit = unitMap.get(value);
    form.setFieldsValue({
      fixedGoodsUnitName: unit?.name,
      fixedUnitConversion: unit?.conversionRate
    });
  }

  return (
    <AModal
      destroyOnClose
      open={open}
      title={operateType === 'add' ? t('page.order.operate.addDetail') : t('page.order.operate.editDetail')}
      width={640}
      onCancel={onClose}
      onOk={onSubmit}
    >
      <AForm
        form={form}
        layout="vertical"
      >
        <AForm.Item
          hidden
          name="id"
        />
        <AForm.Item
          hidden
          name="goodsName"
        />
        <AForm.Item
          hidden
          name="goodsCode"
        />
        <AForm.Item
          hidden
          name="goodsUnitName"
        />
        <AForm.Item
          hidden
          name="fixedGoodsUnitName"
        />
        <AForm.Item
          hidden
          name="unitConversion"
        />
        <AForm.Item
          hidden
          name="fixedUnitConversion"
        />

        <AForm.Item
          label={t('page.order.detail.goodsName')}
          name="goodsId"
          rules={[rules.goodsId]}
        >
          <RemoteOptionSelect
            placeholder={t('page.order.operate.form.goodsId')}
            resource={SELECTION_OPTION_RESOURCES.GOODS}
            onChange={handleGoodsChange}
          />
        </AForm.Item>

        <ARow gutter={16}>
          <ACol span={12}>
            <AForm.Item
              label={t('page.order.detail.goodsUnitName')}
              name="goodsUnitId"
              rules={[rules.goodsUnitId]}
            >
              <ASelect
                disabled={!goodsId}
                options={unitOptions}
                placeholder={t('page.order.operate.form.goodsUnitId')}
                onChange={handleGoodsUnitChange}
              />
            </AForm.Item>
          </ACol>
          <ACol span={12}>
            <AForm.Item
              label={t('page.order.detail.quantity')}
              name="quantity"
              rules={[rules.quantity]}
            >
              <AInputNumber
                className="w-full"
                min={0.000001}
                placeholder={t('page.order.operate.form.quantity')}
              />
            </AForm.Item>
          </ACol>
          <ACol span={12}>
            <AForm.Item
              label={t('page.order.detail.fixedPrice')}
              name="fixedPrice"
              rules={[rules.fixedPrice]}
            >
              <AInputNumber
                className="w-full"
                min={0}
                placeholder={t('page.order.operate.form.fixedPrice')}
                precision={4}
              />
            </AForm.Item>
          </ACol>
          <ACol span={12}>
            <AForm.Item
              label={t('page.order.detail.fixedGoodsUnitName')}
              name="fixedGoodsUnitId"
              rules={[rules.fixedGoodsUnitId]}
            >
              <ASelect
                disabled={!goodsId}
                options={unitOptions}
                placeholder={t('page.order.operate.form.fixedGoodsUnitId')}
                onChange={handleFixedGoodsUnitChange}
              />
            </AForm.Item>
          </ACol>
        </ARow>

        <AForm.Item
          label={t('page.order.detail.remark')}
          name="remark"
        >
          <AInput.TextArea
            placeholder={t('page.order.operate.form.remark')}
            rows={2}
          />
        </AForm.Item>

        <AForm.Item
          className="mb-0"
          label={t('page.order.detail.innerRemark')}
          name="innerRemark"
        >
          <AInput.TextArea
            placeholder={t('page.order.operate.form.innerRemark')}
            rows={2}
          />
        </AForm.Item>
      </AForm>
    </AModal>
  );
});

export default OrderDetailLineModal;
