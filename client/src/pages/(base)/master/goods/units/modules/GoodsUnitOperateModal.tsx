import { EnableStatusFormItem } from '@/features/crud';
import { useFormRules } from '@/features/form';
import { toOptions, useGoodsOptions } from '@/service/hooks/useBaseDataOptions';

type RuleKey = 'conversionRate' | 'goodsId' | 'status';

const GoodsUnitOperateModal: FC<Page.OperateDrawerProps> = memo(
  ({ form, handleSubmit, onClose, open, operateType }) => {
    const { t } = useTranslation();
    const { defaultRequiredRule } = useFormRules();
    const { data: goods } = useGoodsOptions();
    const goodsOptions = toOptions(goods);

    const rules: Record<RuleKey, App.Global.FormRule> = {
      conversionRate: defaultRequiredRule,
      goodsId: defaultRequiredRule,
      status: defaultRequiredRule
    };

    return (
      <AModal
        destroyOnClose
        open={open}
        title={operateType === 'add' ? t('page.goods.unit.add') : t('page.goods.unit.edit')}
        width={640}
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
            label={t('page.goods.unit.goodsId')}
            name="goodsId"
            rules={[rules.goodsId]}
          >
            <ASelect
              options={goodsOptions}
              placeholder={t('page.goods.unit.form.goodsId')}
            />
          </AForm.Item>

          <AForm.Item
            label={t('page.goods.unit.name')}
            name="name"
          >
            <AInput placeholder={t('page.goods.unit.form.name')} />
          </AForm.Item>

          <AForm.Item
            label={t('page.goods.unit.code')}
            name="code"
          >
            <AInput placeholder={t('page.goods.unit.form.code')} />
          </AForm.Item>

          <AForm.Item
            label={t('page.goods.unit.conversionRate')}
            name="conversionRate"
            rules={[rules.conversionRate]}
          >
            <AInputNumber
              className="w-full"
              min={0}
              placeholder={t('page.goods.unit.form.conversionRate')}
            />
          </AForm.Item>

          <AForm.Item
            label={t('page.goods.unit.isBaseUnit')}
            name="isBaseUnit"
            valuePropName="checked"
          >
            <ASwitch />
          </AForm.Item>

          <AForm.Item
            label={t('page.goods.unit.sort')}
            name="sort"
          >
            <AInputNumber
              className="w-full"
              min={0}
              placeholder={t('page.goods.unit.form.sort')}
            />
          </AForm.Item>

          <AForm.Item
            label={t('page.goods.unit.remark')}
            name="remark"
          >
            <AInput.TextArea
              placeholder={t('page.goods.unit.form.remark')}
              rows={2}
            />
          </AForm.Item>

          <EnableStatusFormItem label={t('page.goods.unit.status')} />
        </AForm>
      </AModal>
    );
  }
);

export default GoodsUnitOperateModal;
