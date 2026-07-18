import { EnableStatusSelect, SearchActionsCol } from '@/features/crud';
import { toOptions, useGoodsOptions } from '@/service/hooks';

const GoodsUnitSearch: FC<Page.SearchProps> = memo(({ form, reset, search, searchParams }) => {
  const { t } = useTranslation();
  const { data: goods } = useGoodsOptions();
  const goodsOptions = toOptions(goods);

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
            label={t('page.goods.unit.goodsId')}
            name="goodsId"
          >
            <ASelect
              allowClear
              options={goodsOptions}
              placeholder={t('page.goods.unit.form.goodsId')}
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
            label={t('page.goods.unit.name')}
            name="name"
          >
            <AInput placeholder={t('page.goods.unit.form.name')} />
          </AForm.Item>
        </ACol>
        <ACol
          lg={6}
          md={12}
          span={24}
        >
          <AForm.Item
            className="m-0"
            label={t('page.goods.unit.status')}
            name="status"
          >
            <EnableStatusSelect placeholder={t('page.goods.unit.form.status')} />
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

export default GoodsUnitSearch;
