import { EnableStatusSelect, PurchasePatternSelect, SearchActionsCol } from '@/features/crud';

const RuleSearch: FC<Page.SearchProps> = memo(({ form, reset, search, searchParams }) => {
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
            label={t('page.purchase.rule.name')}
            name="name"
          >
            <AInput placeholder={t('page.purchase.rule.form.name')} />
          </AForm.Item>
        </ACol>

        <ACol
          lg={6}
          md={12}
          span={24}
        >
          <AForm.Item
            className="m-0"
            label={t('page.purchase.rule.code')}
            name="code"
          >
            <AInput placeholder={t('page.purchase.rule.form.code')} />
          </AForm.Item>
        </ACol>

        <ACol
          lg={6}
          md={12}
          span={24}
        >
          <AForm.Item
            className="m-0"
            label={t('page.purchase.rule.purchasePattern')}
            name="purchasePattern"
          >
            <PurchasePatternSelect placeholder={t('page.purchase.rule.form.purchasePattern')} />
          </AForm.Item>
        </ACol>

        <ACol
          lg={6}
          md={12}
          span={24}
        >
          <AForm.Item
            className="m-0"
            label={t('page.purchase.rule.status')}
            name="status"
          >
            <EnableStatusSelect placeholder={t('page.purchase.rule.form.status')} />
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

export default RuleSearch;
