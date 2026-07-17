import { EnableStatusFormItem } from '@/features/crud';
import { useFormRules } from '@/features/form';

type RuleKey = 'code' | 'name';

const CompanyOperateDrawer: FC<Page.OperateDrawerProps> = memo(({ form, handleSubmit, onClose, open, operateType }) => {
  const { t } = useTranslation();

  const { defaultRequiredRule } = useFormRules();

  const rules: Record<RuleKey, App.Global.FormRule> = {
    code: defaultRequiredRule,
    name: defaultRequiredRule
  };

  return (
    <ADrawer
      open={open}
      title={operateType === 'add' ? t('page.customer.company.addCompany') : t('page.customer.company.editCompany')}
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
          label={t('page.customer.company.name')}
          name="name"
          rules={[rules.name]}
        >
          <AInput placeholder={t('page.customer.company.form.name')} />
        </AForm.Item>

        <AForm.Item
          label={t('page.customer.company.code')}
          name="code"
          rules={[rules.code]}
        >
          <AInput placeholder={t('page.customer.company.form.code')} />
        </AForm.Item>

        <AForm.Item
          label={t('page.customer.company.contactName')}
          name="contactName"
        >
          <AInput placeholder={t('page.customer.company.form.contactName')} />
        </AForm.Item>

        <AForm.Item
          label={t('page.customer.company.contactPhone')}
          name="contactPhone"
        >
          <AInput placeholder={t('page.customer.company.form.contactPhone')} />
        </AForm.Item>

        <AForm.Item
          label={t('page.customer.company.address')}
          name="address"
        >
          <AInput placeholder={t('page.customer.company.form.address')} />
        </AForm.Item>

        <AForm.Item
          label={t('page.customer.company.remark')}
          name="remark"
        >
          <AInput.TextArea
            placeholder={t('page.customer.company.form.remark')}
            rows={3}
          />
        </AForm.Item>

        <EnableStatusFormItem label={t('page.customer.company.status')} />
      </AForm>
    </ADrawer>
  );
});

export default CompanyOperateDrawer;
