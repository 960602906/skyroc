import { afterSaleHandleTypeOptions, afterSaleTypeOptions } from '@/constants/business';
import { AfterSaleStatusSelect, SearchActionsCol } from '@/features/crud';
import { toOptions, useCustomerOptions } from '@/service/hooks';

const AfterSaleSearch: FC<Page.SearchProps> = memo(({ form, reset, search, searchParams }) => {
  const { t } = useTranslation();
  const { data: customers } = useCustomerOptions();

  const typeOptions = afterSaleTypeOptions.map(item => ({
    label: t(item.label as App.I18n.I18nKey),
    value: item.value as Api.AfterSale.AfterSaleType
  }));
  const handleOptions = afterSaleHandleTypeOptions.map(item => ({
    label: t(item.label as App.I18n.I18nKey),
    value: item.value as Api.AfterSale.HandleType
  }));

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
            label={t('page.afterSale.list.keyword')}
            name="keyword"
          >
            <AInput
              allowClear
              placeholder={t('page.afterSale.list.form.keyword')}
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
            label={t('page.afterSale.list.createTime')}
            name="dateRange"
          >
            <ADatePicker.RangePicker
              allowClear
              className="w-full"
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
            label={t('page.afterSale.list.afterStatus')}
            name="afterStatus"
          >
            <AfterSaleStatusSelect placeholder={t('page.afterSale.list.form.afterStatus')} />
          </AForm.Item>
        </ACol>

        <ACol
          lg={6}
          md={12}
          span={24}
        >
          <AForm.Item
            className="m-0"
            label={t('page.afterSale.list.customerId')}
            name="customerId"
          >
            <ASelect
              allowClear
              showSearch
              optionFilterProp="label"
              options={toOptions(customers)}
              placeholder={t('page.afterSale.list.form.customerId')}
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
            label={t('page.afterSale.list.afterSaleType')}
            name="afterSaleType"
          >
            <ASelect
              allowClear
              options={typeOptions}
              placeholder={t('page.afterSale.list.form.afterSaleType')}
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
            label={t('page.afterSale.list.handleType')}
            name="handleType"
          >
            <ASelect
              allowClear
              options={handleOptions}
              placeholder={t('page.afterSale.list.form.handleType')}
            />
          </AForm.Item>
        </ACol>

        <SearchActionsCol
          fieldCount={6}
          onReset={reset}
          onSearch={search}
        />
      </ARow>
    </AForm>
  );
});

export default AfterSaleSearch;
