import { PickupTaskStatusSelect, SearchActionsCol } from '@/features/crud';
import { formatField } from '@/utils/common';

interface PickupTaskSearchProps extends Page.SearchProps {
  drivers?: { code?: null | string; id: string; name: string }[];
}

/** 取货任务分页筛选表单。 */
const PickupTaskSearch: FC<PickupTaskSearchProps> = memo(({ drivers = [], form, reset, search, searchParams }) => {
  const { t } = useTranslation();

  const driverOptions = drivers.map(driver => ({
    label: formatField(driver, ['name', 'code'], ' · ') || driver.id,
    value: driver.id
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
            label={t('page.pickupTask.keyword')}
            name="keyword"
          >
            <AInput
              allowClear
              placeholder={t('page.pickupTask.form.keyword')}
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
            label={t('page.pickupTask.plannedPickupTime')}
            name="plannedRange"
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
            label={t('page.pickupTask.pickupStatus')}
            name="pickupStatus"
          >
            <PickupTaskStatusSelect placeholder={t('page.pickupTask.form.pickupStatus')} />
          </AForm.Item>
        </ACol>

        <ACol
          lg={6}
          md={12}
          span={24}
        >
          <AForm.Item
            className="m-0"
            label={t('page.pickupTask.driver')}
            name="driverId"
          >
            <ASelect
              allowClear
              showSearch
              optionFilterProp="label"
              options={driverOptions}
              placeholder={t('page.pickupTask.form.driver')}
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

export default PickupTaskSearch;
