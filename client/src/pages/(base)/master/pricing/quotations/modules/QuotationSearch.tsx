import { EnableStatusSelect, SearchActionsCol } from '@/features/crud';

const QuotationSearch: FC<Page.SearchProps> = memo(({ form, reset, search, searchParams }) => {
  const { t } = useTranslation();

  return (
    <AForm
      form={form}
      initialValues={searchParams}
      labelCol={{ md: 7, span: 5 }}
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
            label={t('page.goods.quotation.name')}
            name="name"
          >
            <AInput placeholder={t('page.goods.quotation.form.name')} />
          </AForm.Item>
        </ACol>
        <ACol
          lg={6}
          md={12}
          span={24}
        >
          <AForm.Item
            className="m-0"
            label={t('page.goods.quotation.code')}
            name="code"
          >
            <AInput placeholder={t('page.goods.quotation.form.code')} />
          </AForm.Item>
        </ACol>
        <ACol
          lg={6}
          md={12}
          span={24}
        >
          <AForm.Item
            className="m-0"
            label={t('page.goods.quotation.isAudited')}
            name="isAudited"
          >
            <ASelect
              allowClear
              placeholder={t('page.goods.quotation.form.isAudited')}
              options={[
                {
                  label: (
                    <ABadge
                      status="success"
                      text={t('page.goods.quotation.audited')}
                    />
                  ),
                  value: true
                },
                {
                  label: (
                    <ABadge
                      status="default"
                      text={t('page.goods.quotation.unaudited')}
                    />
                  ),
                  value: false
                }
              ]}
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
            label={t('page.goods.quotation.status')}
            name="status"
          >
            <EnableStatusSelect placeholder={t('page.goods.quotation.form.status')} />
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

export default QuotationSearch;
