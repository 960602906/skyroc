import { enableStatusOptions } from '@/constants/business';
import { useFormRules } from '@/features/form';

type RuleKey = 'code' | 'name' | 'status';

const SupplierOperateDrawer: FC<Page.OperateDrawerProps> = memo(
  ({ form, handleSubmit, onClose, open, operateType }) => {
    const { t } = useTranslation();

    const { defaultRequiredRule } = useFormRules();

    const rules: Record<RuleKey, App.Global.FormRule> = {
      code: defaultRequiredRule,
      name: defaultRequiredRule,
      status: defaultRequiredRule
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
          operateType === 'add' ? t('page.purchase.supplier.addSupplier') : t('page.purchase.supplier.editSupplier')
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
            label={t('page.purchase.supplier.name')}
            name="name"
            rules={[rules.name]}
          >
            <AInput placeholder={t('page.purchase.supplier.form.name')} />
          </AForm.Item>

          <AForm.Item
            label={t('page.purchase.supplier.code')}
            name="code"
            rules={[rules.code]}
          >
            <AInput placeholder={t('page.purchase.supplier.form.code')} />
          </AForm.Item>

          <AForm.Item
            label={t('page.purchase.supplier.contactName')}
            name="contactName"
          >
            <AInput placeholder={t('page.purchase.supplier.form.contactName')} />
          </AForm.Item>

          <AForm.Item
            label={t('page.purchase.supplier.contactPhone')}
            name="contactPhone"
          >
            <AInput placeholder={t('page.purchase.supplier.form.contactPhone')} />
          </AForm.Item>

          <AForm.Item
            label={t('page.purchase.supplier.address')}
            name="address"
          >
            <AInput placeholder={t('page.purchase.supplier.form.address')} />
          </AForm.Item>

          <AForm.Item
            label={t('page.purchase.supplier.taxNo')}
            name="taxNo"
          >
            <AInput placeholder={t('page.purchase.supplier.form.taxNo')} />
          </AForm.Item>

          <AForm.Item
            label={t('page.purchase.supplier.bankName')}
            name="bankName"
          >
            <AInput placeholder={t('page.purchase.supplier.form.bankName')} />
          </AForm.Item>

          <AForm.Item
            label={t('page.purchase.supplier.bankAccount')}
            name="bankAccount"
          >
            <AInput placeholder={t('page.purchase.supplier.form.bankAccount')} />
          </AForm.Item>

          <AForm.Item
            label={t('page.purchase.supplier.remark')}
            name="remark"
          >
            <AInput.TextArea
              placeholder={t('page.purchase.supplier.form.remark')}
              rows={3}
            />
          </AForm.Item>

          <AForm.Item
            label={t('page.purchase.supplier.status')}
            name="status"
            rules={[rules.status]}
          >
            <ARadio.Group>
              {enableStatusOptions.map(item => (
                <ARadio
                  key={item.value}
                  value={item.value}
                >
                  {t(item.label)}
                </ARadio>
              ))}
            </ARadio.Group>
          </AForm.Item>
        </AForm>
      </ADrawer>
    );
  }
);

export default SupplierOperateDrawer;
