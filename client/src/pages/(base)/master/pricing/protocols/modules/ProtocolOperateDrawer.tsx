import dayjs from 'dayjs';

import { EnableStatusFormItem } from '@/features/crud';
import { useFormRules } from '@/features/form';
import { toOptions, useCustomerOptions, useQuotationOptions } from '@/service/hooks';

type RuleKey = 'code' | 'effectiveStart' | 'name';

const ProtocolOperateDrawer: FC<Page.OperateDrawerProps> = memo(
  ({ form, handleSubmit, onClose, open, operateType }) => {
    const { t } = useTranslation();

    const { defaultRequiredRule } = useFormRules();

    const { data: quotationOptions } = useQuotationOptions();
    const { data: customers } = useCustomerOptions();

    const rules: Record<RuleKey, App.Global.FormRule> = {
      code: defaultRequiredRule,
      effectiveStart: defaultRequiredRule,
      name: defaultRequiredRule
    };

    return (
      <ADrawer
        open={open}
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
        title={
          operateType === 'add' ? t('page.customer.protocol.addProtocol') : t('page.customer.protocol.editProtocol')
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
            label={t('page.customer.protocol.name')}
            name="name"
            rules={[rules.name]}
          >
            <AInput placeholder={t('page.customer.protocol.form.name')} />
          </AForm.Item>

          <AForm.Item
            label={t('page.customer.protocol.code')}
            name="code"
            rules={[rules.code]}
          >
            <AInput placeholder={t('page.customer.protocol.form.code')} />
          </AForm.Item>

          <AForm.Item
            label={t('page.customer.protocol.quotationId')}
            name="quotationId"
          >
            <ASelect
              allowClear
              options={quotationOptions}
              placeholder={t('page.customer.protocol.form.quotationId')}
            />
          </AForm.Item>

          <ARow gutter={16}>
            <ACol span={12}>
              <AForm.Item
                getValueFromEvent={(_, dateString) => dateString || null}
                label={t('page.customer.protocol.effectiveStart')}
                name="effectiveStart"
                rules={[rules.effectiveStart]}
                getValueProps={value => ({
                  value: value ? dayjs(value) : undefined
                })}
              >
                <ADatePicker
                  className="w-full"
                  format="YYYY-MM-DD"
                />
              </AForm.Item>
            </ACol>
            <ACol span={12}>
              <AForm.Item
                getValueFromEvent={(_, dateString) => dateString || null}
                label={t('page.customer.protocol.effectiveEnd')}
                name="effectiveEnd"
                getValueProps={value => ({
                  value: value ? dayjs(value) : undefined
                })}
              >
                <ADatePicker
                  className="w-full"
                  format="YYYY-MM-DD"
                />
              </AForm.Item>
            </ACol>
          </ARow>

          <AForm.Item
            label={t('page.customer.protocol.customerIds')}
            name="customerIds"
          >
            <ASelect
              allowClear
              mode="multiple"
              options={toOptions(customers)}
              placeholder={t('page.customer.protocol.form.customerIds')}
            />
          </AForm.Item>

          <AForm.Item
            label={t('page.customer.protocol.remark')}
            name="remark"
          >
            <AInput.TextArea
              placeholder={t('page.customer.protocol.form.remark')}
              rows={3}
            />
          </AForm.Item>

          <EnableStatusFormItem label={t('page.customer.protocol.status')} />
        </AForm>
      </ADrawer>
    );
  }
);

export default ProtocolOperateDrawer;
