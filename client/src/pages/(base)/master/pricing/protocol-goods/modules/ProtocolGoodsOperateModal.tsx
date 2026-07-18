import { useFormRules } from '@/features/form';
import { toOptions, useGoodsOptions, useGoodsUnitsByGoodsOptions, useProtocolOptions } from '@/service/hooks';

type RuleKey = 'customerProtocolId' | 'goodsId' | 'goodsUnitId' | 'protocolPrice';

type ProtocolGoodsOperateModalProps = Page.OperateDrawerProps & {
  /** 固定客户协议时隐藏协议选择，由表单 customerProtocolId 字段承载 */
  lockCustomerProtocolId?: boolean;
};

const ProtocolGoodsOperateModal: FC<ProtocolGoodsOperateModalProps> = memo(
  ({ form, handleSubmit, lockCustomerProtocolId = false, onClose, open, operateType }) => {
    const { t } = useTranslation();

    const { defaultRequiredRule } = useFormRules();

    const { data: protocolOptions } = useProtocolOptions();
    const { data: goods } = useGoodsOptions();
    const goodsOptions = toOptions(goods);

    const goodsId = AForm.useWatch('goodsId', form);
    const { data: goodsUnitOptions = [] } = useGoodsUnitsByGoodsOptions(goodsId);
    const prevGoodsIdRef = useRef<string | undefined>(undefined);

    // 仅在用户切换商品时清空单位，避免编辑回填被冲掉
    useEffect(() => {
      if (!open) {
        prevGoodsIdRef.current = undefined;
        return;
      }

      if (prevGoodsIdRef.current !== undefined && prevGoodsIdRef.current !== goodsId) {
        form.setFieldValue('goodsUnitId', undefined);
      }

      prevGoodsIdRef.current = goodsId;
    }, [form, goodsId, open]);

    const rules: Record<RuleKey, App.Global.FormRule> = {
      customerProtocolId: defaultRequiredRule,
      goodsId: defaultRequiredRule,
      goodsUnitId: defaultRequiredRule,
      protocolPrice: defaultRequiredRule
    };

    return (
      <AModal
        destroyOnClose
        open={open}
        width={520}
        title={
          operateType === 'add'
            ? t('page.customer.protocolGoods.addProtocolGoods')
            : t('page.customer.protocolGoods.editProtocolGoods')
        }
        onCancel={onClose}
        onOk={handleSubmit}
      >
        <AForm
          form={form}
          layout="vertical"
        >
          <AForm.Item
            hidden
            name="id"
          />

          {lockCustomerProtocolId ? (
            <AForm.Item
              hidden
              name="customerProtocolId"
              rules={[rules.customerProtocolId]}
            />
          ) : (
            <AForm.Item
              label={t('page.customer.protocolGoods.customerProtocolId')}
              name="customerProtocolId"
              rules={[rules.customerProtocolId]}
            >
              <ASelect
                allowClear
                options={protocolOptions}
                placeholder={t('page.customer.protocolGoods.form.customerProtocolId')}
              />
            </AForm.Item>
          )}

          <AForm.Item
            label={t('page.customer.protocolGoods.goodsId')}
            name="goodsId"
            rules={[rules.goodsId]}
          >
            <ASelect
              allowClear
              options={goodsOptions}
              placeholder={t('page.customer.protocolGoods.form.goodsId')}
            />
          </AForm.Item>

          <AForm.Item
            label={t('page.customer.protocolGoods.goodsUnitId')}
            name="goodsUnitId"
            rules={[rules.goodsUnitId]}
          >
            <ASelect
              allowClear
              disabled={!goodsId}
              options={goodsUnitOptions}
              placeholder={t('page.customer.protocolGoods.form.goodsUnitId')}
            />
          </AForm.Item>

          <AForm.Item
            label={t('page.customer.protocolGoods.protocolPrice')}
            name="protocolPrice"
            rules={[rules.protocolPrice]}
          >
            <AInputNumber
              className="w-full"
              min={0}
              placeholder={t('page.customer.protocolGoods.form.protocolPrice')}
            />
          </AForm.Item>

          <AForm.Item
            label={t('page.customer.protocolGoods.minOrderQuantity')}
            name="minOrderQuantity"
          >
            <AInputNumber
              className="w-full"
              min={0}
              placeholder={t('page.customer.protocolGoods.form.minOrderQuantity')}
            />
          </AForm.Item>

          <AForm.Item
            label={t('page.customer.protocolGoods.remark')}
            name="remark"
          >
            <AInput.TextArea
              placeholder={t('page.customer.protocolGoods.form.remark')}
              rows={3}
            />
          </AForm.Item>
        </AForm>
      </AModal>
    );
  }
);

export default ProtocolGoodsOperateModal;
