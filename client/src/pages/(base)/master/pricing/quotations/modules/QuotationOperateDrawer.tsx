import dayjs from 'dayjs';

import { EnableStatusFormItem } from '@/features/crud';
import { useFormRules } from '@/features/form';
import { toOptions, useCustomerOptions } from '@/service/hooks/useBaseDataOptions';

type RuleKey = 'code' | 'name' | 'status';

const QuotationOperateDrawer: FC<Page.OperateDrawerProps> = memo(
  ({ form, handleSubmit, onClose, open, operateType }) => {
    const { t } = useTranslation();
    const { defaultRequiredRule } = useFormRules();
    const { data: customers } = useCustomerOptions();
    const customerOptions = toOptions(customers);

    const rules: Record<RuleKey, App.Global.FormRule> = {
      code: defaultRequiredRule,
      name: defaultRequiredRule,
      status: defaultRequiredRule
    };

    return (
      <ADrawer
        open={open}
        title={operateType === 'add' ? t('page.goods.quotation.add') : t('page.goods.quotation.edit')}
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
            label={t('page.goods.quotation.name')}
            name="name"
            rules={[rules.name]}
          >
            <AInput placeholder={t('page.goods.quotation.form.name')} />
          </AForm.Item>

          <AForm.Item
            label={t('page.goods.quotation.code')}
            name="code"
            rules={[rules.code]}
          >
            <AInput placeholder={t('page.goods.quotation.form.code')} />
          </AForm.Item>

          <AForm.Item
            label={t('page.goods.quotation.description')}
            name="description"
          >
            <AInput.TextArea
              placeholder={t('page.goods.quotation.form.description')}
              rows={2}
            />
          </AForm.Item>

          <ARow gutter={16}>
            <ACol span={12}>
              <AForm.Item
                getValueFromEvent={(_, dateString) => dateString || null}
                label={t('page.goods.quotation.effectiveStart')}
                name="effectiveStart"
                getValueProps={value => ({
                  value: value ? dayjs(value) : undefined
                })}
              >
                <ADatePicker className="w-full" />
              </AForm.Item>
            </ACol>
            <ACol span={12}>
              <AForm.Item
                getValueFromEvent={(_, dateString) => dateString || null}
                label={t('page.goods.quotation.effectiveEnd')}
                name="effectiveEnd"
                getValueProps={value => ({
                  value: value ? dayjs(value) : undefined
                })}
              >
                <ADatePicker className="w-full" />
              </AForm.Item>
            </ACol>
          </ARow>

          <AForm.Item
            label={t('page.goods.quotation.customerIds')}
            name="customerIds"
          >
            <ASelect
              allowClear
              mode="multiple"
              options={customerOptions}
              placeholder={t('page.goods.quotation.form.customerIds')}
            />
          </AForm.Item>

          <AForm.Item
            label={t('page.goods.quotation.isAudited')}
            name="isAudited"
            valuePropName="checked"
          >
            <ASwitch />
          </AForm.Item>

          <AForm.Item
            label={t('page.goods.quotation.remark')}
            name="remark"
          >
            <AInput.TextArea
              placeholder={t('page.goods.quotation.form.remark')}
              rows={2}
            />
          </AForm.Item>

          <EnableStatusFormItem label={t('page.goods.quotation.status')} />
        </AForm>
      </ADrawer>
    );
  }
);

export default QuotationOperateDrawer;
