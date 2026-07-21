import RemoteOptionSelect from '@/components/RemoteOptionSelect';
import { PurchasePatternSelect, PurchasePlanStatusSelect, SearchActionsCol } from '@/features/crud';
import { SELECTION_OPTION_RESOURCES } from '@/service/hooks';

/** 采购计划列表搜索栏。 */
const PurchasePlanSearch: FC<Page.SearchProps> = memo(({ form, reset, search, searchParams }) => {
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
            label={t('page.purchase.plan.keyword')}
            name="keyword"
          >
            <AInput
              allowClear
              placeholder={t('page.purchase.plan.form.keyword')}
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
            label={t('page.purchase.plan.purchasePattern')}
            name="purchasePattern"
          >
            <PurchasePatternSelect placeholder={t('page.purchase.plan.form.purchasePattern')} />
          </AForm.Item>
        </ACol>

        <ACol
          lg={6}
          md={12}
          span={24}
        >
          <AForm.Item
            className="m-0"
            label={t('page.purchase.plan.purchaseStatus')}
            name="purchaseStatus"
          >
            <PurchasePlanStatusSelect placeholder={t('page.purchase.plan.form.purchaseStatus')} />
          </AForm.Item>
        </ACol>

        <ACol
          lg={6}
          md={12}
          span={24}
        >
          <AForm.Item
            className="m-0"
            label={t('page.purchase.plan.supplier')}
            name="supplierId"
          >
            <RemoteOptionSelect
              allowClear
              placeholder={t('page.purchase.plan.form.supplierId')}
              resource={SELECTION_OPTION_RESOURCES.SUPPLIER}
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

export default PurchasePlanSearch;
