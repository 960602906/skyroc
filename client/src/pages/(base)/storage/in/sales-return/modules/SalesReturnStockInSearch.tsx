import RemoteOptionSelect from '@/components/RemoteOptionSelect';
import { SearchActionsCol, StockDocumentStatusSelect } from '@/features/crud';
import { SELECTION_OPTION_RESOURCES, toOptions, useWareOptions } from '@/service/hooks';

/** 销售退货入库列表搜索栏 */
const SalesReturnStockInSearch: FC<Page.SearchProps> = memo(({ form, reset, search, searchParams }) => {
  const { t } = useTranslation();
  const { data: wares } = useWareOptions();
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
            label={t('page.storage.in.salesReturn.keyword')}
            name="keyword"
          >
            <AInput
              allowClear
              placeholder={t('page.storage.in.salesReturn.form.keyword')}
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
            label={t('page.storage.in.salesReturn.businessStatus')}
            name="businessStatus"
          >
            <StockDocumentStatusSelect placeholder={t('page.storage.in.salesReturn.form.businessStatus')} />
          </AForm.Item>
        </ACol>

        <ACol
          lg={6}
          md={12}
          span={24}
        >
          <AForm.Item
            className="m-0"
            label={t('page.storage.in.salesReturn.ware')}
            name="wareId"
          >
            <ASelect
              allowClear
              options={wareOptions}
              placeholder={t('page.storage.in.salesReturn.form.wareId')}
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
            label={t('page.storage.in.salesReturn.customer')}
            name="customerId"
          >
            <RemoteOptionSelect
              allowClear
              placeholder={t('page.storage.in.salesReturn.form.customerId')}
              resource={SELECTION_OPTION_RESOURCES.CUSTOMER}
            />
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

export default SalesReturnStockInSearch;
