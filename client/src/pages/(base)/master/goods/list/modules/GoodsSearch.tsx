import { EnableStatusSelect, SearchActions } from '@/features/crud';
import { toOptions, useGoodsTypeOptions, useSupplierOptions, useWareOptions } from '@/service/hooks';

const GoodsSearch: FC<Page.SearchProps> = memo(({ form, reset, search, searchParams }) => {
  const { t } = useTranslation();
  const { data: goodsTypes } = useGoodsTypeOptions();
  const { data: suppliers } = useSupplierOptions();
  const { data: wares } = useWareOptions();

  const goodsTypeOptions = toOptions(goodsTypes);
  const supplierOptions = toOptions(suppliers);
  const wareOptions = toOptions(wares);

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
            label={t('page.goods.list.name')}
            name="name"
          >
            <AInput placeholder={t('page.goods.list.form.name')} />
          </AForm.Item>
        </ACol>
        <ACol
          lg={6}
          md={12}
          span={24}
        >
          <AForm.Item
            className="m-0"
            label={t('page.goods.list.code')}
            name="code"
          >
            <AInput placeholder={t('page.goods.list.form.code')} />
          </AForm.Item>
        </ACol>
        <ACol
          lg={6}
          md={12}
          span={24}
        >
          <AForm.Item
            className="m-0"
            label={t('page.goods.list.goodsTypeId')}
            name="goodsTypeId"
          >
            <ASelect
              allowClear
              options={goodsTypeOptions}
              placeholder={t('page.goods.list.form.goodsTypeId')}
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
            label={t('page.goods.list.defaultSupplierId')}
            name="defaultSupplierId"
          >
            <ASelect
              allowClear
              options={supplierOptions}
              placeholder={t('page.goods.list.form.defaultSupplierId')}
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
            label={t('page.goods.list.defaultWareId')}
            name="defaultWareId"
          >
            <ASelect
              allowClear
              options={wareOptions}
              placeholder={t('page.goods.list.form.defaultWareId')}
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
            label={t('page.goods.list.isOnSale')}
            name="isOnSale"
          >
            <ASelect
              allowClear
              placeholder={t('page.goods.list.form.isOnSale')}
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
          <AForm.Item
            className="m-0"
            label={t('page.goods.list.status')}
            name="status"
          >
            <EnableStatusSelect placeholder={t('page.goods.list.form.status')} />
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

export default GoodsSearch;
