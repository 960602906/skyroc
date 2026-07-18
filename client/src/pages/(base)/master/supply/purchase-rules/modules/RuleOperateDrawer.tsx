import { EnableStatusFormItem } from '@/features/crud';
import { useFormRules } from '@/features/form';
import {
  toOptions,
  useGoodsTypeOptions,
  usePurchaserOptions,
  useSupplierOptions,
  useWareOptions
} from '@/service/hooks';

type RuleKey = 'code' | 'name' | 'purchasePattern';

const purchasePatternOptions = [
  { label: 'page.purchase.rule.purchasePatternDirect', value: 1 },
  { label: 'page.purchase.rule.purchasePatternMarket', value: 2 }
];

const RuleOperateDrawer: FC<Page.OperateDrawerProps> = memo(({ form, handleSubmit, onClose, open, operateType }) => {
  const { t } = useTranslation();

  const { defaultRequiredRule } = useFormRules();

  const { data: goodsTypes } = useGoodsTypeOptions();
  const { data: suppliers } = useSupplierOptions();
  const { data: purchasers } = usePurchaserOptions();
  const { data: wares } = useWareOptions();

  const rules: Record<RuleKey, App.Global.FormRule> = {
    code: defaultRequiredRule,
    name: defaultRequiredRule,
    purchasePattern: defaultRequiredRule
  };

  return (
    <ADrawer
      open={open}
      title={operateType === 'add' ? t('page.purchase.rule.addRule') : t('page.purchase.rule.editRule')}
      width={640}
      footer={
        <AFlex justify="space-between">
          <AButton onClick={onClose}>{t('common.cancel')}</AButton>
          <AButton
            type="primary"
            onClick={handleSubmit}
          >
            {t('common.confirm')}
          </AButton>
        </AFlex>
      }
      onClose={onClose}
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
          label={t('page.purchase.rule.name')}
          name="name"
          rules={[rules.name]}
        >
          <AInput placeholder={t('page.purchase.rule.form.name')} />
        </AForm.Item>

        <AForm.Item
          label={t('page.purchase.rule.code')}
          name="code"
          rules={[rules.code]}
        >
          <AInput placeholder={t('page.purchase.rule.form.code')} />
        </AForm.Item>

        <AForm.Item
          label={t('page.purchase.rule.purchasePattern')}
          name="purchasePattern"
          rules={[rules.purchasePattern]}
        >
          <ASelect
            placeholder={t('page.purchase.rule.form.purchasePattern')}
            options={purchasePatternOptions.map(item => ({
              label: t(item.label),
              value: item.value
            }))}
          />
        </AForm.Item>

        <AForm.Item
          label={t('page.purchase.rule.goodsTypeId')}
          name="goodsTypeId"
        >
          <ASelect
            allowClear
            showSearch
            optionFilterProp="label"
            options={toOptions(goodsTypes)}
            placeholder={t('page.purchase.rule.form.goodsTypeId')}
          />
        </AForm.Item>

        <AForm.Item
          label={t('page.purchase.rule.supplierId')}
          name="supplierId"
        >
          <ASelect
            allowClear
            showSearch
            optionFilterProp="label"
            options={toOptions(suppliers)}
            placeholder={t('page.purchase.rule.form.supplierId')}
          />
        </AForm.Item>

        <AForm.Item
          label={t('page.purchase.rule.purchaserId')}
          name="purchaserId"
        >
          <ASelect
            allowClear
            showSearch
            optionFilterProp="label"
            options={toOptions(purchasers)}
            placeholder={t('page.purchase.rule.form.purchaserId')}
          />
        </AForm.Item>

        <AForm.Item
          label={t('page.purchase.rule.wareId')}
          name="wareId"
        >
          <ASelect
            allowClear
            showSearch
            optionFilterProp="label"
            options={toOptions(wares)}
            placeholder={t('page.purchase.rule.form.wareId')}
          />
        </AForm.Item>

        <AForm.Item
          label={t('page.purchase.rule.remark')}
          name="remark"
        >
          <AInput.TextArea
            placeholder={t('page.purchase.rule.form.remark')}
            rows={3}
          />
        </AForm.Item>

        <EnableStatusFormItem label={t('page.purchase.rule.status')} />
      </AForm>
    </ADrawer>
  );
});

export default RuleOperateDrawer;
