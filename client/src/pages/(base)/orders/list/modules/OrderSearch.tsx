import {
  EnableStatusSelect,
  OrderDateTypeSelect,
  OrderReturnStatusSelect,
  SaleOrderStatusSelect,
  SearchActionsCol
} from '@/features/crud';
import { toOptions, useCustomerOptions, useCustomerTagOptions, useSupplierOptions } from '@/service/hooks';

const OrderSearch: FC<Page.SearchProps> = memo(({ form, reset, search, searchParams }) => {
  const { t } = useTranslation();

  const { data: customers } = useCustomerOptions();
  const { data: customerTags } = useCustomerTagOptions();
  const { data: suppliers } = useSupplierOptions();

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
            label={t('page.order.list.keyword')}
            name="keyword"
          >
            <AInput
              allowClear
              placeholder={t('page.order.list.form.keyword')}
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
            label={t('page.order.list.dateType')}
            name="dateType"
          >
            <OrderDateTypeSelect placeholder={t('page.order.list.form.dateType')} />
          </AForm.Item>
        </ACol>

        <ACol
          lg={6}
          md={12}
          span={24}
        >
          <AForm.Item
            className="m-0"
            label={t('page.order.list.dateRange')}
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
            label={t('page.order.list.orderStatus')}
            name="orderStatus"
          >
            <SaleOrderStatusSelect placeholder={t('page.order.list.form.orderStatus')} />
          </AForm.Item>
        </ACol>

        <ACol
          lg={6}
          md={12}
          span={24}
        >
          <AForm.Item
            className="m-0"
            label={t('page.order.list.customerId')}
            name="customerId"
          >
            <ASelect
              allowClear
              showSearch
              optionFilterProp="label"
              options={toOptions(customers)}
              placeholder={t('page.order.list.form.customerId')}
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
            label={t('page.order.list.returnStatus')}
            name="returnStatus"
          >
            <OrderReturnStatusSelect placeholder={t('page.order.list.form.returnStatus')} />
          </AForm.Item>
        </ACol>

        <ACol
          lg={6}
          md={12}
          span={24}
        >
          <AForm.Item
            className="m-0"
            label={t('page.order.list.hasOutSale')}
            name="hasOutSale"
          >
            <ASelect
              allowClear
              placeholder={t('page.order.list.form.hasOutSale')}
              options={[
                { label: t('common.yesOrNo.yes'), value: true },
                { label: t('common.yesOrNo.no'), value: false }
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
            label={t('page.order.list.hasPurchasePlan')}
            name="hasPurchasePlan"
          >
            <ASelect
              allowClear
              placeholder={t('page.order.list.form.hasPurchasePlan')}
              options={[
                { label: t('common.yesOrNo.yes'), value: true },
                { label: t('common.yesOrNo.no'), value: false }
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
            label={t('page.order.list.updateStatus')}
            name="updateStatus"
          >
            <ASelect
              allowClear
              placeholder={t('page.order.list.form.updateStatus')}
              options={[
                { label: t('common.yesOrNo.yes'), value: true },
                { label: t('common.yesOrNo.no'), value: false }
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
            label={t('page.order.list.goodsKey')}
            name="goodsKey"
          >
            <AInput
              allowClear
              placeholder={t('page.order.list.form.goodsKey')}
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
            label={t('page.order.list.supplierId')}
            name="supplierId"
          >
            <ASelect
              allowClear
              showSearch
              optionFilterProp="label"
              options={toOptions(suppliers)}
              placeholder={t('page.order.list.form.supplierId')}
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
            label={t('page.order.list.customerTagIds')}
            name="customerTagIds"
          >
            <ASelect
              allowClear
              maxTagCount="responsive"
              mode="multiple"
              optionFilterProp="label"
              options={toOptions(customerTags)}
              placeholder={t('page.order.list.form.customerTagIds')}
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
            label={t('page.order.list.status')}
            name="status"
          >
            <EnableStatusSelect placeholder={t('page.order.list.form.status')} />
          </AForm.Item>
        </ACol>

        <SearchActionsCol
          fieldCount={13}
          onReset={reset}
          onSearch={search}
        />
      </ARow>
    </AForm>
  );
});

export default OrderSearch;
