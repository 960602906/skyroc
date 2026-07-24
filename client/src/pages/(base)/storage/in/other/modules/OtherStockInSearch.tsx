import { SearchActionsCol, StockDocumentStatusSelect } from '@/features/crud';
import { toOptions, useWareOptions } from '@/service/hooks';

/** 其他入库列表搜索栏 */
const OtherStockInSearch: FC<Page.SearchProps> = memo(({ form, reset, search, searchParams }) => {
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
            label={t('page.storage.in.other.keyword')}
            name="keyword"
          >
            <AInput
              allowClear
              placeholder={t('page.storage.in.other.form.keyword')}
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
            label={t('page.storage.in.other.businessStatus')}
            name="businessStatus"
          >
            <StockDocumentStatusSelect placeholder={t('page.storage.in.other.form.businessStatus')} />
          </AForm.Item>
        </ACol>

        <ACol
          lg={6}
          md={12}
          span={24}
        >
          <AForm.Item
            className="m-0"
            label={t('page.storage.in.other.ware')}
            name="wareId"
          >
            <ASelect
              allowClear
              options={wareOptions}
              placeholder={t('page.storage.in.other.form.wareId')}
            />
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

export default OtherStockInSearch;
