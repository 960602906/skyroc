import dayjs, { type Dayjs } from 'dayjs';

import { EnableStatus } from '@/service/enums';

type PickupTaskAssignFormValues = {
  driverId: string;
  plannedPickupTime: Dayjs | null;
  remark?: string | null;
};

interface PickupTaskAssignModalProps {
  drivers: Api.Driver.AllEntity[];
  loading: boolean;
  onCancel: () => void;
  onSubmit: (values: Api.AfterSale.AssignPickupTaskPayload) => Promise<void>;
  open: boolean;
  task: Api.AfterSale.PickupTask | null;
}

const backendDateTimeFormat = 'YYYY-MM-DD HH:mm:ss';

/** 将接口返回的 UTC 墙钟时间转换为本地 DatePicker 值。 */
function toLocalDateTime(value: string | null) {
  return value ? dayjs.utc(value, backendDateTimeFormat).local() : null;
}

/** 取货任务的司机分配与预约时间弹窗。 */
const PickupTaskAssignModal: FC<PickupTaskAssignModalProps> = memo(
  ({ drivers, loading, onCancel, onSubmit, open, task }) => {
    const { t } = useTranslation();
    const [form] = AForm.useForm<PickupTaskAssignFormValues>();

    useEffect(() => {
      if (!open || !task) {
        return;
      }

      form.setFieldsValue({
        driverId: task.driverId ?? undefined,
        plannedPickupTime: toLocalDateTime(task.plannedPickupTime),
        remark: task.remark
      });
    }, [form, open, task]);

    const driverOptions = drivers
      .filter(driver => driver.status === EnableStatus.ENABLED)
      .map(driver => ({
        label: [driver.name, driver.phone].filter(Boolean).join(' · ') || driver.id,
        value: driver.id
      }));

    async function handleSubmit() {
      const values = await form.validateFields();
      await onSubmit({
        driverId: values.driverId,
        plannedPickupTime: values.plannedPickupTime
          ? values.plannedPickupTime.utc().format(backendDateTimeFormat)
          : null,
        remark: values.remark?.trim() || null
      });
    }

    return (
      <AModal
        destroyOnClose
        confirmLoading={loading}
        open={open}
        title={t('page.pickupTask.assignTitle', { taskNo: task?.taskNo ?? '' })}
        onCancel={onCancel}
        onOk={handleSubmit}
      >
        <AForm
          form={form}
          layout="vertical"
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

          <AForm.Item
            label={t('page.pickupTask.plannedPickupTime')}
            name="plannedPickupTime"
          >
            <ADatePicker
              allowClear
              className="w-full"
              format={backendDateTimeFormat}
              showTime={{ format: 'HH:mm' }}
            />
          </AForm.Item>

          <AForm.Item
            label={t('page.pickupTask.remark')}
            name="remark"
          >
            <AInput.TextArea
              allowClear
              placeholder={t('page.pickupTask.form.remark')}
              rows={3}
            />
          </AForm.Item>
        </AForm>
      </AModal>
    );
  }
);

export default PickupTaskAssignModal;
