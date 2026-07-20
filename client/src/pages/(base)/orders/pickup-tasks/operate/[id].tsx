import dayjs, { type Dayjs } from 'dayjs';
import { type LoaderFunctionArgs, redirect, useLoaderData } from 'react-router-dom';

import { useCloseTabAndNavigate } from '@/features/tab';
import {
  fetchGetAfterSalePickupTaskDetail,
  fetchGetAllDrivers,
  fetchUpdateAfterSalePickupTasksAssign
} from '@/service/api';
import { EnableStatus, PickupTaskStatus } from '@/service/enums';
import { formatField } from '@/utils/common';

const LIST_PATH = '/orders/pickup-tasks';
const BACKEND_DATE_TIME_FORMAT = 'YYYY-MM-DD HH:mm:ss';

type PickupTaskScheduleFormValues = {
  driverId: string;
  plannedPickupTime: Dayjs | null;
  remark?: string | null;
};

/** 将后端 UTC 时间转换为本地时间选择器使用的值。 */
function toLocalDateTime(value: string | null) {
  return value ? dayjs.utc(value, BACKEND_DATE_TIME_FORMAT).local() : null;
}

export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) {
    return redirect(LIST_PATH);
  }

  try {
    const [task, drivers] = await Promise.all([fetchGetAfterSalePickupTaskDetail(id), fetchGetAllDrivers()]);
    const canSchedule =
      task.pickupStatus === PickupTaskStatus.PENDING_ASSIGN || task.pickupStatus === PickupTaskStatus.PENDING_PICKUP;
    if (!canSchedule) {
      return redirect(`/orders/pickup-tasks/detail/${id}`);
    }
    return { drivers, task };
  } catch {
    return redirect(LIST_PATH);
  }
}

/** 编辑未开始取货任务的司机、预约时间和调度备注。 */
const PickupTaskOperatePage = () => {
  const { t } = useTranslation();
  const { drivers, task } = useLoaderData() as { drivers: Api.Driver.AllEntity[]; task: Api.AfterSale.PickupTask };
  const closeTabAndNavigate = useCloseTabAndNavigate();
  const [form] = AForm.useForm<PickupTaskScheduleFormValues>();
  const [submitting, setSubmitting] = useState(false);
  const driverOptions = drivers
    .filter(driver => driver.status === EnableStatus.ENABLED)
    .map(driver => ({
      label: formatField(driver, ['name', 'phone'], ' · ') || driver.id,
      value: driver.id
    }));

  async function handleSubmit() {
    const values = await form.validateFields();
    setSubmitting(true);
    try {
      await fetchUpdateAfterSalePickupTasksAssign(task.id, {
        driverId: values.driverId,
        plannedPickupTime: values.plannedPickupTime
          ? values.plannedPickupTime.utc().format(BACKEND_DATE_TIME_FORMAT)
          : null,
        remark: values.remark?.trim() || null
      });
      window.$message?.success(t('common.updateSuccess'));
      closeTabAndNavigate(LIST_PATH);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="h-full min-h-500px flex-col-stretch gap-16px overflow-auto">
      <ACard
        className="card-wrapper"
        styles={{ body: { display: 'none' } }}
        title={t('page.pickupTask.operate.title', { taskNo: task.taskNo })}
        variant="borderless"
        extra={
          <ASpace>
            <AButton onClick={() => closeTabAndNavigate(LIST_PATH)}>{t('common.cancel')}</AButton>
            <AButton
              loading={submitting}
              type="primary"
              onClick={handleSubmit}
            >
              {t('common.confirm')}
            </AButton>
          </ASpace>
        }
      />

      <AForm
        form={form}
        layout="vertical"
        initialValues={{
          driverId: task.driverId ?? undefined,
          plannedPickupTime: toLocalDateTime(task.plannedPickupTime),
          remark: task.remark
        }}
      >
        <ACard
          className="card-wrapper"
          title={t('page.pickupTask.operate.scheduleInfo')}
          variant="borderless"
        >
          <ARow gutter={16}>
            <ACol
              lg={12}
              xs={24}
            >
              <AForm.Item
                label={t('page.pickupTask.driver')}
                name="driverId"
                rules={[{ message: t('page.pickupTask.form.driver'), required: true }]}
              >
                <ASelect
                  showSearch
                  optionFilterProp="label"
                  options={driverOptions}
                  placeholder={t('page.pickupTask.form.driver')}
                />
              </AForm.Item>
            </ACol>
            <ACol
              lg={12}
              xs={24}
            >
              <AForm.Item
                label={t('page.pickupTask.plannedPickupTime')}
                name="plannedPickupTime"
              >
                <ADatePicker
                  allowClear
                  className="w-full"
                  format={BACKEND_DATE_TIME_FORMAT}
                  showTime={{ format: 'HH:mm' }}
                />
              </AForm.Item>
            </ACol>
            <ACol span={24}>
              <AForm.Item
                className="mb-0"
                label={t('page.pickupTask.remark')}
                name="remark"
              >
                <AInput.TextArea
                  allowClear
                  placeholder={t('page.pickupTask.form.remark')}
                  rows={4}
                />
              </AForm.Item>
            </ACol>
          </ARow>
        </ACard>
      </AForm>
    </div>
  );
};

export default PickupTaskOperatePage;
