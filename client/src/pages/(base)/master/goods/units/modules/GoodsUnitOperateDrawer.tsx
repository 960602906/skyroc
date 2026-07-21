import RemoteOptionSelect from '@/components/RemoteOptionSelect';
import { EnableStatusFormItem } from '@/features/crud';
import { useFormRules } from '@/features/form';
import { SELECTION_OPTION_RESOURCES } from '@/service/hooks';

type RuleKey = 'conversionRate' | 'goodsId' | 'name' | 'status';

const GoodsUnitOperateDrawer: FC<Page.OperateDrawerProps> = memo(
  ({ form, handleSubmit, onClose, open, operateType }) => {
    const { t } = useTranslation();
    const { defaultRequiredRule } = useFormRules();
    const rules: Record<RuleKey, App.Global.FormRule> = {
      conversionRate: defaultRequiredRule,
      goodsId: defaultRequiredRule,
      name: defaultRequiredRule,
      status: defaultRequiredRule
    };

    return (
      <ADrawer
        open={open}
        title={operateType === 'add' ? t('page.goods.unit.add') : t('page.goods.unit.edit')}
        width={520}
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
            label={t('page.goods.unit.goodsId')}
            name="goodsId"
            rules={[rules.goodsId]}
          >
            <RemoteOptionSelect
              placeholder={t('page.goods.unit.form.goodsId')}
              resource={SELECTION_OPTION_RESOURCES.GOODS}
            />
          </AForm.Item>

          <AForm.Item
            label={t('page.goods.unit.name')}
            name="name"
            rules={[rules.name]}
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
      </ADrawer>
    );
  }
);

export default GoodsUnitOperateDrawer;
