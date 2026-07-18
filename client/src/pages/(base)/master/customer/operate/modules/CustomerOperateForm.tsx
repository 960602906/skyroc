import { useQuery } from '@tanstack/react-query';

import { EnableStatusFormItem } from '@/features/crud';
import { useFormRules } from '@/features/form';
import { fetchGetAllCustomerTags } from '@/service/api';
import { toOptions, useCompanyOptions, useQuotationOptions, useWareOptions } from '@/service/hooks';
import { QUERY_KEYS } from '@/service/keys';

type RuleKey = 'code' | 'name';

interface CustomerOperateFormProps {
  form: Page.FormInstance;
}

function CustomerOperateForm({ form }: CustomerOperateFormProps) {
  const { t } = useTranslation();

  const { defaultRequiredRule } = useFormRules();

  const { data: companies } = useCompanyOptions();
  const { data: wares } = useWareOptions();
  const { data: quotationOptions } = useQuotationOptions();

  const { data: tags } = useQuery({
    queryFn: () => fetchGetAllCustomerTags(),
    queryKey: QUERY_KEYS.BASE.CUSTOMER_TAGS,
    staleTime: 60_000
  });

  const rules: Record<RuleKey, App.Global.FormRule> = {
    code: defaultRequiredRule,
    name: defaultRequiredRule
  };

  return (
    <AForm
      className="flex-col-stretch gap-16px"
      form={form}
      layout="vertical"
    >
      <AForm.Item
        hidden
        name="id"
      />

      <ACard
        className="card-wrapper"
        title={t('page.customer.operate.sectionBasic')}
        variant="borderless"
      >
        <ARow gutter={16}>
          <ACol
            lg={12}
            span={24}
          >
            <AForm.Item
              label={t('page.customer.list.name')}
              name="name"
              rules={[rules.name]}
            >
              <AInput placeholder={t('page.customer.list.form.name')} />
            </AForm.Item>
          </ACol>
          <ACol
            lg={12}
            span={24}
          >
            <AForm.Item
              label={t('page.customer.list.code')}
              name="code"
              rules={[rules.code]}
            >
              <AInput placeholder={t('page.customer.list.form.code')} />
            </AForm.Item>
          </ACol>
          <ACol
            lg={12}
            span={24}
          >
            <AForm.Item
              label={t('page.customer.list.companyId')}
              name="companyId"
            >
              <ASelect
                allowClear
                options={toOptions(companies)}
                placeholder={t('page.customer.list.form.companyId')}
              />
            </AForm.Item>
          </ACol>
          <ACol
            lg={12}
            span={24}
          >
            <AForm.Item
              label={t('page.customer.operate.defaultWareId')}
              name="defaultWareId"
            >
              <ASelect
                allowClear
                options={toOptions(wares)}
                placeholder={t('page.customer.operate.form.defaultWareId')}
              />
            </AForm.Item>
          </ACol>
          <ACol
            lg={12}
            span={24}
          >
            <AForm.Item
              label={t('page.customer.list.contactName')}
              name="contactName"
            >
              <AInput placeholder={t('page.customer.list.form.contactName')} />
            </AForm.Item>
          </ACol>
          <ACol
            lg={12}
            span={24}
          >
            <AForm.Item
              label={t('page.customer.list.contactPhone')}
              name="contactPhone"
            >
              <AInput placeholder={t('page.customer.list.form.contactPhone')} />
            </AForm.Item>
          </ACol>
          <ACol span={24}>
            <AForm.Item
              label={t('page.customer.list.address')}
              name="address"
            >
              <AInput placeholder={t('page.customer.list.form.address')} />
            </AForm.Item>
          </ACol>
          <ACol
            lg={12}
            span={24}
          >
            <AForm.Item
              label={t('page.customer.operate.quotationId')}
              name="quotationId"
            >
              <ASelect
                allowClear
                options={quotationOptions}
                placeholder={t('page.customer.operate.form.quotationId')}
              />
            </AForm.Item>
          </ACol>
          <ACol
            lg={12}
            span={24}
          >
            <AForm.Item
              label={t('page.customer.operate.tagIds')}
              name="tagIds"
            >
              <ASelect
                allowClear
                mode="multiple"
                options={toOptions(tags)}
                placeholder={t('page.customer.operate.form.tagIds')}
              />
            </AForm.Item>
          </ACol>
          <ACol
            lg={12}
            span={24}
          >
            <EnableStatusFormItem label={t('page.customer.list.status')} />
          </ACol>
        </ARow>
      </ACard>

      <ACard
        className="card-wrapper"
        title={t('page.customer.operate.sectionInvoice')}
        variant="borderless"
      >
        <ARow gutter={16}>
          <ACol
            lg={12}
            span={24}
          >
            <AForm.Item
              label={t('page.customer.operate.invoiceTitle')}
              name="invoiceTitle"
            >
              <AInput placeholder={t('page.customer.operate.form.invoiceTitle')} />
            </AForm.Item>
          </ACol>
          <ACol
            lg={12}
            span={24}
          >
            <AForm.Item
              label={t('page.customer.operate.taxpayerIdentificationNumber')}
              name="taxpayerIdentificationNumber"
            >
              <AInput placeholder={t('page.customer.operate.form.taxpayerIdentificationNumber')} />
            </AForm.Item>
          </ACol>
          <ACol
            lg={12}
            span={24}
          >
            <AForm.Item
              label={t('page.customer.operate.invoicePhone')}
              name="invoicePhone"
            >
              <AInput placeholder={t('page.customer.operate.form.invoicePhone')} />
            </AForm.Item>
          </ACol>
          <ACol
            lg={12}
            span={24}
          >
            <AForm.Item
              label={t('page.customer.operate.invoiceEmail')}
              name="invoiceEmail"
            >
              <AInput placeholder={t('page.customer.operate.form.invoiceEmail')} />
            </AForm.Item>
          </ACol>
          <ACol span={24}>
            <AForm.Item
              label={t('page.customer.operate.invoiceAddress')}
              name="invoiceAddress"
            >
              <AInput placeholder={t('page.customer.operate.form.invoiceAddress')} />
            </AForm.Item>
          </ACol>
          <ACol
            lg={12}
            span={24}
          >
            <AForm.Item
              label={t('page.customer.operate.invoiceReceiverName')}
              name="invoiceReceiverName"
            >
              <AInput placeholder={t('page.customer.operate.form.invoiceReceiverName')} />
            </AForm.Item>
          </ACol>
          <ACol
            lg={12}
            span={24}
          >
            <AForm.Item
              label={t('page.customer.operate.invoiceReceiverPhone')}
              name="invoiceReceiverPhone"
            >
              <AInput placeholder={t('page.customer.operate.form.invoiceReceiverPhone')} />
            </AForm.Item>
          </ACol>
          <ACol span={24}>
            <AForm.Item
              label={t('page.customer.operate.invoiceReceiverAddress')}
              name="invoiceReceiverAddress"
            >
              <AInput placeholder={t('page.customer.operate.form.invoiceReceiverAddress')} />
            </AForm.Item>
          </ACol>
          <ACol
            lg={12}
            span={24}
          >
            <AForm.Item
              label={t('page.customer.operate.bankName')}
              name="bankName"
            >
              <AInput placeholder={t('page.customer.operate.form.bankName')} />
            </AForm.Item>
          </ACol>
          <ACol
            lg={12}
            span={24}
          >
            <AForm.Item
              className="mb-0"
              label={t('page.customer.operate.bankAccount')}
              name="bankAccount"
            >
              <AInput placeholder={t('page.customer.operate.form.bankAccount')} />
            </AForm.Item>
          </ACol>
        </ARow>
      </ACard>

      <ACard
        className="card-wrapper"
        title={t('page.customer.operate.sectionBusiness')}
        variant="borderless"
      >
        <ARow gutter={16}>
          <ACol
            lg={12}
            span={24}
          >
            <AForm.Item
              label={t('page.customer.operate.legalRepresentative')}
              name="legalRepresentative"
            >
              <AInput placeholder={t('page.customer.operate.form.legalRepresentative')} />
            </AForm.Item>
          </ACol>
          <ACol
            lg={12}
            span={24}
          >
            <AForm.Item
              label={t('page.customer.operate.unifiedSocialCreditCode')}
              name="unifiedSocialCreditCode"
            >
              <AInput placeholder={t('page.customer.operate.form.unifiedSocialCreditCode')} />
            </AForm.Item>
          </ACol>
          <ACol span={24}>
            <AForm.Item
              label={t('page.customer.operate.registeredAddress')}
              name="registeredAddress"
            >
              <AInput placeholder={t('page.customer.operate.form.registeredAddress')} />
            </AForm.Item>
          </ACol>
          <ACol
            lg={12}
            span={24}
          >
            <AForm.Item
              label={t('page.customer.operate.registeredCapital')}
              name="registeredCapital"
            >
              <AInput placeholder={t('page.customer.operate.form.registeredCapital')} />
            </AForm.Item>
          </ACol>
          <ACol
            lg={12}
            span={24}
          >
            <AForm.Item
              label={t('page.customer.operate.establishDate')}
              name="establishDate"
            >
              <ADatePicker className="w-full" />
            </AForm.Item>
          </ACol>
          <ACol
            lg={12}
            span={24}
          >
            <AForm.Item
              label={t('page.customer.operate.businessTerm')}
              name="businessTerm"
            >
              <AInput placeholder={t('page.customer.operate.form.businessTerm')} />
            </AForm.Item>
          </ACol>
          <ACol
            lg={12}
            span={24}
          >
            <AForm.Item
              label={t('page.customer.operate.registrationAuthority')}
              name="registrationAuthority"
            >
              <AInput placeholder={t('page.customer.operate.form.registrationAuthority')} />
            </AForm.Item>
          </ACol>
          <ACol
            lg={12}
            span={24}
          >
            <AForm.Item
              label={t('page.customer.operate.registrationStatus')}
              name="registrationStatus"
            >
              <AInput placeholder={t('page.customer.operate.form.registrationStatus')} />
            </AForm.Item>
          </ACol>
          <ACol span={24}>
            <AForm.Item
              label={t('page.customer.operate.businessScope')}
              name="businessScope"
            >
              <AInput.TextArea
                placeholder={t('page.customer.operate.form.businessScope')}
                rows={4}
              />
            </AForm.Item>
          </ACol>
          <ACol span={24}>
            <AForm.Item
              className="mb-0"
              label={t('page.customer.operate.remark')}
              name="remark"
            >
              <AInput.TextArea
                placeholder={t('page.customer.operate.form.remark')}
                rows={3}
              />
            </AForm.Item>
          </ACol>
        </ARow>
      </ACard>
    </AForm>
  );
}

export default CustomerOperateForm;
