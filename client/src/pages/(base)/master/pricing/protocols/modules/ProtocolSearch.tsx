import RemoteOptionSelect from '@/components/RemoteOptionSelect';
import { EnableStatusSelect, SearchActionsCol } from '@/features/crud';
import { SELECTION_OPTION_RESOURCES } from '@/service/hooks';

const ProtocolSearch: FC<Page.SearchProps> = memo(({ form, reset, search, searchParams }) => {
  const { t } = useTranslation();

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
            <RemoteOptionSelect
              allowClear
              placeholder={t('page.customer.protocol.form.quotationId')}
              resource={SELECTION_OPTION_RESOURCES.QUOTATION}
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

        <SearchActionsCol
          fieldCount={4}
          onReset={reset}
          onSearch={search}
        />
      </ARow>
    </AForm>
  );
});

export default ProtocolSearch;
