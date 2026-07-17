import { EnableStatusFormItem } from '@/features/crud';
import { useFormRules } from '@/features/form';

type RuleKey = 'code' | 'name';

const WareOperateDrawer: FC<Page.OperateDrawerProps> = memo(({ form, handleSubmit, onClose, open, operateType }) => {
  const { t } = useTranslation();

  const { defaultRequiredRule } = useFormRules();

  const rules: Record<RuleKey, App.Global.FormRule> = {
    code: defaultRequiredRule,
    name: defaultRequiredRule
  };

  return (
    <ADrawer
      open={open}
      title={operateType === 'add' ? t('page.storage.ware.addWare') : t('page.storage.ware.editWare')}
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
          label={t('page.storage.ware.name')}
          name="name"
          rules={[rules.name]}
        >
          <AInput placeholder={t('page.storage.ware.form.name')} />
        </AForm.Item>

        <AForm.Item
          label={t('page.storage.ware.code')}
          name="code"
          rules={[rules.code]}
        >
          <AInput placeholder={t('page.storage.ware.form.code')} />
        </AForm.Item>

        <AForm.Item
          label={t('page.storage.ware.contactName')}
          name="contactName"
        >
          <AInput placeholder={t('page.storage.ware.form.contactName')} />
        </AForm.Item>

        <AForm.Item
          label={t('page.storage.ware.contactPhone')}
          name="contactPhone"
        >
          <AInput placeholder={t('page.storage.ware.form.contactPhone')} />
        </AForm.Item>

        <AForm.Item
          label={t('page.storage.ware.address')}
          name="address"
        >
          <AInput placeholder={t('page.storage.ware.form.address')} />
        </AForm.Item>

        <AForm.Item
          label={t('page.storage.ware.sort')}
          name="sort"
        >
          <AInputNumber
            className="w-full"
            min={0}
            placeholder={t('page.storage.ware.form.sort')}
          />
        </AForm.Item>

        <AForm.Item
          label={t('page.storage.ware.remark')}
          name="remark"
        >
          <AInput.TextArea
            placeholder={t('page.storage.ware.form.remark')}
            rows={3}
          />
        </AForm.Item>

        <EnableStatusFormItem label={t('page.storage.ware.status')} />
      </AForm>
    </ADrawer>
  );
});

export default WareOperateDrawer;
