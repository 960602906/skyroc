import { useFormRules } from '@/features/form';
import { toOptions, useGoodsOptions, useProtocolOptions } from '@/service/hooks/useBaseDataOptions';

type RuleKey = 'customerProtocolId' | 'goodsId' | 'goodsUnitId' | 'protocolPrice';

const ProtocolGoodsOperateModal: FC<Page.OperateDrawerProps> = memo(
  ({ form, handleSubmit, onClose, open, operateType }) => {
    const { t } = useTranslation();

    const { defaultRequiredRule } = useFormRules();

    const { data: protocolOptions } = useProtocolOptions();
    const { data: goods } = useGoodsOptions();

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

          <AForm.Item
            label={t('page.customer.protocolGoods.goodsId')}
            name="goodsId"
            rules={[rules.goodsId]}
          >
            <ASelect
              allowClear
              options={toOptions(goods)}
              placeholder={t('page.customer.protocolGoods.form.goodsId')}
            />
          </AForm.Item>

          <AForm.Item
            label={t('page.customer.protocolGoods.goodsUnitId')}
            name="goodsUnitId"
            rules={[rules.goodsUnitId]}
          >
            <AInput placeholder={t('page.customer.protocolGoods.form.goodsUnitId')} />
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
