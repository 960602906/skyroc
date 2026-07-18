import { SearchActionsCol } from '@/features/crud';
import { toOptions, useGoodsOptions, useProtocolOptions } from '@/service/hooks';

const ProtocolGoodsSearch: FC<Page.SearchProps> = memo(({ form, reset, search, searchParams }) => {
  const { t } = useTranslation();

  const { data: protocolOptions } = useProtocolOptions();
  const { data: goods } = useGoodsOptions();

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
            label={t('page.customer.protocolGoods.customerProtocolId')}
            name="customerProtocolId"
          >
            <ASelect
              allowClear
              options={protocolOptions}
              placeholder={t('page.customer.protocolGoods.form.customerProtocolId')}
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
            label={t('page.customer.protocolGoods.goodsId')}
            name="goodsId"
          >
            <ASelect
              allowClear
              options={toOptions(goods)}
              placeholder={t('page.customer.protocolGoods.form.goodsId')}
            />
          </AForm.Item>
        </ACol>

        <SearchActionsCol
          fieldCount={2}
          onReset={reset}
          onSearch={search}
        />
      </ARow>
    </AForm>
  );
});

export default ProtocolGoodsSearch;
