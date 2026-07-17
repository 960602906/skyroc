import { EnableStatusSelect, SearchActions } from '@/features/crud';

const GoodsTypeSearch: FC<Page.SearchProps> = memo(({ form, reset, search, searchParams }) => {
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
            label={t('page.goods.type.name')}
            name="name"
          >
            <AInput placeholder={t('page.goods.type.form.name')} />
          </AForm.Item>
        </ACol>
        <ACol
          lg={6}
          md={12}
          span={24}
        >
          <AForm.Item
            className="m-0"
            label={t('page.goods.type.code')}
            name="code"
          >
            <AInput placeholder={t('page.goods.type.form.code')} />
          </AForm.Item>
        </ACol>
        <ACol
          lg={6}
          md={12}
          span={24}
        >
          <AForm.Item
            className="m-0"
            label={t('page.goods.type.status')}
            name="status"
          >
            <EnableStatusSelect placeholder={t('page.goods.type.form.status')} />
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

export default GoodsTypeSearch;
