import { EnableStatusSelect, SearchActionsCol } from '@/features/crud';
import { toOptions, useCompanyOptions, useCustomerOptions } from '@/service/hooks';

const SubAccountSearch: FC<Page.SearchProps> = memo(({ form, reset, search, searchParams }) => {
  const { t } = useTranslation();

  const { data: companies } = useCompanyOptions();
  const { data: customers } = useCustomerOptions();

  return (
    <AForm
      form={form}
      initialValues={searchParams}
      labelCol={{
        md: 7,
        span: 5
      }}
    >
      <ARow
        wrap
        gutter={[16, 16]}
      >
        <ACol
          lg={6}
          md={12}
          span={24}
        >
          <AForm.Item
            className="m-0"
            label={t('page.customer.subAccount.companyId')}
            name="companyId"
          >
            <ASelect
              allowClear
              options={toOptions(companies)}
              placeholder={t('page.customer.subAccount.form.companyId')}
            />
          </AForm.Item>
        </ACol>

        <ACol
          lg={6}
          md={12}
          span={24}
        >
          <AForm.Item
            className="m-0"
            label={t('page.customer.subAccount.customerId')}
            name="customerId"
          >
            <ASelect
              allowClear
              options={toOptions(customers)}
              placeholder={t('page.customer.subAccount.form.customerId')}
            />
          </AForm.Item>
        </ACol>

        <ACol
          lg={6}
          md={12}
          span={24}
        >
          <AForm.Item
            className="m-0"
            label={t('page.customer.subAccount.username')}
            name="username"
          >
            <AInput placeholder={t('page.customer.subAccount.form.username')} />
          </AForm.Item>
        </ACol>

        <ACol
          lg={6}
          md={12}
          span={24}
        >
          <AForm.Item
            className="m-0"
            label={t('page.customer.subAccount.status')}
            name="status"
          >
            <EnableStatusSelect placeholder={t('page.customer.subAccount.form.status')} />
          </AForm.Item>
        </ACol>

        <SearchActionsCol
          fieldCount={4}
          onReset={reset}
          onSearch={search}
        />
      </ARow>
    </AForm>
  );
});

export default SubAccountSearch;
