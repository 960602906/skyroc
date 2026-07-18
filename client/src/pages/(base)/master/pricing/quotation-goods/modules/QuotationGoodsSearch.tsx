import { SearchActions } from '@/features/crud';
import { toOptions, useGoodsOptions, useQuotationOptions } from '@/service/hooks';

const QuotationGoodsSearch: FC<Page.SearchProps> = memo(({ form, reset, search, searchParams }) => {
  const { t } = useTranslation();
  const { data: quotationOptions = [] } = useQuotationOptions();
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
            label={t('page.goods.quotationGoods.quotationId')}
            name="quotationId"
          >
            <ASelect
              allowClear
              options={quotationOptions}
              placeholder={t('page.goods.quotationGoods.form.quotationId')}
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
            label={t('page.goods.quotationGoods.goodsId')}
            name="goodsId"
          >
            <ASelect
              allowClear
              options={goodsOptions}
              placeholder={t('page.goods.quotationGoods.form.goodsId')}
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
            label={t('page.goods.quotationGoods.isOnSale')}
            name="isOnSale"
          >
            <ASelect
              allowClear
              placeholder={t('page.goods.quotationGoods.form.isOnSale')}
              options={[
                { label: t('page.goods.list.onSale'), value: true },
                { label: t('page.goods.list.offSale'), value: false }
              ]}
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

export default QuotationGoodsSearch;
