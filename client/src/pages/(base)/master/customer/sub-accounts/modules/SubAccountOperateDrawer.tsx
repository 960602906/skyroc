import { EnableStatusFormItem } from '@/features/crud';
import { useFormRules } from '@/features/form';
import { toOptions, useCompanyOptions, useCustomerOptions } from '@/service/hooks';

type RuleKey = 'companyId' | 'username';

const SubAccountOperateDrawer: FC<Page.OperateDrawerProps> = memo(
  ({ form, handleSubmit, onClose, open, operateType }) => {
    const { t } = useTranslation();

    const { defaultRequiredRule } = useFormRules();

    const { data: companies } = useCompanyOptions();
    const { data: customers } = useCustomerOptions();

    const rules: Record<RuleKey, App.Global.FormRule> = {
      companyId: defaultRequiredRule,
      username: defaultRequiredRule
    };

    return (
      <ADrawer
        open={open}
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
        title={
          operateType === 'add'
            ? t('page.customer.subAccount.addSubAccount')
            : t('page.customer.subAccount.editSubAccount')
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
            label={t('page.customer.subAccount.companyId')}
            name="companyId"
            rules={[rules.companyId]}
          >
            <ASelect
              allowClear
              options={toOptions(companies)}
              placeholder={t('page.customer.subAccount.form.companyId')}
            />
          </AForm.Item>

          <AForm.Item
            label={t('page.customer.subAccount.customerId')}
            name="customerId"
          >
            <ASelect
              allowClear
              options={toOptions(customers)}
              placeholder={t('page.customer.subAccount.form.customerId')}
            />
          </AForm.Item>

          <AForm.Item
            label={t('page.customer.subAccount.username')}
            name="username"
            rules={[rules.username]}
          >
            <AInput placeholder={t('page.customer.subAccount.form.username')} />
          </AForm.Item>

          <AForm.Item
            label={t('page.customer.subAccount.nickName')}
            name="nickName"
          >
            <AInput placeholder={t('page.customer.subAccount.form.nickName')} />
          </AForm.Item>

          <AForm.Item
            label={t('page.customer.subAccount.phone')}
            name="phone"
          >
            <AInput placeholder={t('page.customer.subAccount.form.phone')} />
          </AForm.Item>

          <AForm.Item
            label={t('page.customer.subAccount.email')}
            name="email"
          >
            <AInput placeholder={t('page.customer.subAccount.form.email')} />
          </AForm.Item>

          <AForm.Item
            label={t('page.customer.subAccount.remark')}
            name="remark"
          >
            <AInput.TextArea
              placeholder={t('page.customer.subAccount.form.remark')}
              rows={3}
            />
          </AForm.Item>

          <EnableStatusFormItem label={t('page.customer.subAccount.status')} />
        </AForm>
      </ADrawer>
    );
  }
);

export default SubAccountOperateDrawer;
