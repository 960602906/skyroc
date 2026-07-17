import { enableStatusOptions } from '@/constants/business';
import { SearchActions } from '@/features/crud';
import { toOptions, useCompanyOptions } from '@/service/hooks/useBaseDataOptions';
import { translateOptions } from '@/utils/common';

const CustomerListSearch: FC<Page.SearchProps> = memo(({ form, reset, search, searchParams }) => {
  const { t } = useTranslation();

  const { data: companies } = useCompanyOptions();

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
            label={t('page.customer.list.name')}
            name="name"
          >
            <AInput placeholder={t('page.customer.list.form.name')} />
          </AForm.Item>
        </ACol>

        <ACol
          lg={6}
          md={12}
          span={24}
        >
          <AForm.Item
            className="m-0"
            label={t('page.customer.list.code')}
            name="code"
          >
            <AInput placeholder={t('page.customer.list.form.code')} />
          </AForm.Item>
        </ACol>

        <ACol
          lg={6}
          md={12}
          span={24}
        >
          <AForm.Item
            className="m-0"
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
          lg={6}
          md={12}
          span={24}
        >
          <AForm.Item
            className="m-0"
            label={t('page.customer.list.status')}
            name="status"
          >
            <ASelect
              allowClear
              options={translateOptions(enableStatusOptions)}
              placeholder={t('page.customer.list.form.status')}
            />
          </AForm.Item>
        </ACol>

        <ACol
          lg={6}
          md={12}
          span={24}
        >
          <AForm.Item className="m-0">
            <SearchActions
              onReset={reset}
              onSearch={search}
            />
          </AForm.Item>
        </ACol>
      </ARow>
    </AForm>
  );
});

export default CustomerListSearch;
