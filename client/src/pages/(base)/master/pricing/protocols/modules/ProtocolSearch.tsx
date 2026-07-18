import { EnableStatusSelect, SearchActions } from '@/features/crud';
import { useQuotationOptions } from '@/service/hooks';

const ProtocolSearch: FC<Page.SearchProps> = memo(({ form, reset, search, searchParams }) => {
  const { t } = useTranslation();

  const { data: quotationOptions } = useQuotationOptions();

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
            label={t('page.customer.protocol.name')}
            name="name"
          >
            <AInput placeholder={t('page.customer.protocol.form.name')} />
          </AForm.Item>
        </ACol>

        <ACol
          lg={6}
          md={12}
          span={24}
        >
          <AForm.Item
            className="m-0"
            label={t('page.customer.protocol.code')}
            name="code"
          >
            <AInput placeholder={t('page.customer.protocol.form.code')} />
          </AForm.Item>
        </ACol>

        <ACol
          lg={6}
          md={12}
          span={24}
        >
          <AForm.Item
            className="m-0"
            label={t('page.customer.protocol.quotationId')}
            name="quotationId"
          >
            <ASelect
              allowClear
              options={quotationOptions}
              placeholder={t('page.customer.protocol.form.quotationId')}
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
            label={t('page.customer.protocol.status')}
            name="status"
          >
            <EnableStatusSelect placeholder={t('page.customer.protocol.form.status')} />
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

export default ProtocolSearch;
