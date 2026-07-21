import { Form } from 'antd';

import RemoteOptionSelect from '@/components/RemoteOptionSelect';
import { orderDateTypeOptions, orderDateTypeRecord } from '@/constants/business';
import {
  BooleanYesNoSelect,
  EnableStatusSelect,
  OrderReturnStatusSelect,
  SaleOrderStatusSelect,
  SearchActionsCol
} from '@/features/crud';
import { OrderDateType } from '@/service/enums';
import { SELECTION_OPTION_RESOURCES, toOptions, useCustomerTagOptions } from '@/service/hooks';

const OrderSearch: FC<Page.SearchProps> = memo(({ form, reset, search, searchParams }) => {
  const { t } = useTranslation();

  const { data: customerTags } = useCustomerTagOptions();

  const rawDateType = Form.useWatch('dateType', form);
  const dateType = (
    rawDateType === null || rawDateType === undefined || rawDateType === ''
      ? OrderDateType.ORDER_DATE
      : Number(rawDateType)
  ) as Api.Order.DateType;

  const dateTypeOptions = orderDateTypeOptions.map(item => ({
    label: t(item.label as App.I18n.I18nKey),
    value: item.value as Api.Order.DateType
  }));

  const dateRangeLabel = t(orderDateTypeRecord[dateType] ?? orderDateTypeRecord[OrderDateType.ORDER_DATE]);

  return (
    <AForm
      form={form}
      initialValues={searchParams}
      labelCol={{ md: 7, span: 5 }}
    >
      {/** 日期类型存表单；交互放在 RangePicker 面板内，不占筛选项位 */}
      <AForm.Item
        hidden
        name="dateType"
      >
        <ASelect options={dateTypeOptions} />
      </AForm.Item>

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
            label={dateRangeLabel}
            name="dateRange"
          >
            <ADatePicker.RangePicker
              allowClear
              className="w-full"
              panelRender={panel => (
                <div>
                  <div className="flex items-center gap-8px border-b border-[var(--ant-color-split)] border-solid px-12px py-8px">
                    <span className="shrink-0 text-13px text-[var(--ant-color-text-secondary)]">
                      {t('page.order.list.dateType')}
                    </span>
                    <ARadio.Group
                      buttonStyle="solid"
                      options={dateTypeOptions}
                      optionType="button"
                      size="small"
                      value={dateType}
                      onChange={event => {
                        form.setFieldValue('dateType', event.target.value);
                      }}
                    />
                  </div>
                  {panel}
                </div>
              )}
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
            <RemoteOptionSelect
              allowClear
              placeholder={t('page.order.list.form.customerId')}
              resource={SELECTION_OPTION_RESOURCES.CUSTOMER}
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
            <BooleanYesNoSelect placeholder={t('page.order.list.form.hasOutSale')} />
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
            <BooleanYesNoSelect placeholder={t('page.order.list.form.hasPurchasePlan')} />
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
            <BooleanYesNoSelect placeholder={t('page.order.list.form.updateStatus')} />
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
            <RemoteOptionSelect
              allowClear
              placeholder={t('page.order.list.form.supplierId')}
              resource={SELECTION_OPTION_RESOURCES.SUPPLIER}
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
          fieldCount={12}
          onReset={reset}
          onSearch={search}
        />
      </ARow>
    </AForm>
  );
});

export default OrderSearch;
