import { EnableStatusSelect, SearchActionsCol } from '@/features/crud';

const PurchaserSearch: FC<Page.SearchProps> = memo(({ form, reset, search, searchParams }) => {
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
            label={t('page.purchase.purchaser.name')}
            name="name"
          >
            <AInput placeholder={t('page.purchase.purchaser.form.name')} />
          </AForm.Item>
        </ACol>

        <ACol
          lg={6}
          md={12}
          span={24}
        >
          <AForm.Item
            className="m-0"
            label={t('page.purchase.purchaser.code')}
            name="code"
          >
            <AInput placeholder={t('page.purchase.purchaser.form.code')} />
          </AForm.Item>
        </ACol>

        <ACol
          lg={6}
          md={12}
          span={24}
        >
          <AForm.Item
            className="m-0"
            label={t('page.purchase.purchaser.status')}
            name="status"
          >
            <EnableStatusSelect placeholder={t('page.purchase.purchaser.form.status')} />
          </AForm.Item>
        </ACol>

        <SearchActionsCol
          fieldCount={3}
          onReset={reset}
          onSearch={search}
        />
      </ARow>
    </AForm>
  );
});

export default PurchaserSearch;
