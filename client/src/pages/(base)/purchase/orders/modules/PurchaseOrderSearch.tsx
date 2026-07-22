import RemoteOptionSelect from '@/components/RemoteOptionSelect';
import { PurchaseOrderStatusSelect, PurchasePatternSelect, SearchActionsCol } from '@/features/crud';
import { SELECTION_OPTION_RESOURCES, toOptions, usePurchaserOptions } from '@/service/hooks';

/** 采购单列表搜索栏。 */
const PurchaseOrderSearch: FC<Page.SearchProps> = memo(({ form, reset, search, searchParams }) => {
  const { t } = useTranslation();
  const { data: purchasers } = usePurchaserOptions();
  const purchaserOptions = toOptions(purchasers);

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
            label={t('page.purchase.order.keyword')}
            name="keyword"
          >
            <AInput
              allowClear
              placeholder={t('page.purchase.order.form.keyword')}
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
            label={t('page.purchase.order.purchasePattern')}
            name="purchasePattern"
          >
            <PurchasePatternSelect placeholder={t('page.purchase.order.form.purchasePattern')} />
          </AForm.Item>
        </ACol>

        <ACol
          lg={6}
          md={12}
          span={24}
        >
          <AForm.Item
            className="m-0"
            label={t('page.purchase.order.businessStatus')}
            name="businessStatus"
          >
            <PurchaseOrderStatusSelect placeholder={t('page.purchase.order.form.businessStatus')} />
          </AForm.Item>
        </ACol>

        <ACol
          lg={6}
          md={12}
          span={24}
        >
          <AForm.Item
            className="m-0"
            label={t('page.purchase.order.supplier')}
            name="supplierId"
          >
            <RemoteOptionSelect
              allowClear
              placeholder={t('page.purchase.order.form.supplierId')}
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
            label={t('page.purchase.order.purchaser')}
            name="purchaserId"
          >
            <ASelect
              allowClear
              options={purchaserOptions}
              placeholder={t('page.purchase.order.form.purchaserId')}
            />
          </AForm.Item>
        </ACol>

        <SearchActionsCol
          fieldCount={5}
          onReset={reset}
          onSearch={search}
        />
      </ARow>
    </AForm>
  );
});

export default PurchaseOrderSearch;
