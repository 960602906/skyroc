import dayjs, { type Dayjs } from 'dayjs';

import { PickupTaskStatusBadge } from '@/features/crud';
import { useDashboardPickupStatuses, useDashboardReconciliation } from '@/service/hooks';

const BACKEND_DATE_TIME_FORMAT = 'YYYY-MM-DD HH:mm:ss';

interface DashboardSearchFormValues {
  /** 用户按本地自然日选择的统计周期。 */
  dateRange: [Dayjs, Dayjs] | null;
}

/** 创建默认的本月统计周期。 */
function createDefaultDateRange(): [Dayjs, Dayjs] {
  const now = dayjs();
  return [now.startOf('month'), now.endOf('day')];
}

/** 将本地自然日范围转换为后端要求的 UTC 包含边界。 */
function createDashboardSearchParams(dateRange: DashboardSearchFormValues['dateRange']): Api.Dashboard.SearchParams {
  if (!dateRange) {
    return {};
  }

  return {
    dateEnd: dateRange[1].endOf('day').utc().format(BACKEND_DATE_TIME_FORMAT),
    dateStart: dateRange[0].startOf('day').utc().format(BACKEND_DATE_TIME_FORMAT)
  };
}

/** 格式化后端已按业务精度返回的金额。 */
function formatMoney(value: number) {
  return new Intl.NumberFormat(undefined, {
    maximumFractionDigits: 2,
    minimumFractionDigits: 2
  }).format(value);
}

interface ReconciliationMetricProps {
  /** 金额指标的语义强调色与背景 token。 */
  accentClassName: string;
  /** 后端已按业务精度返回的金额。 */
  amount: number;
  /** 指标显示名称。 */
  label: string;
  /** 卡片表面的语义背景 token。 */
  surfaceClassName: string;
}

/** 以语义色突出资金流向的对账金额指标。 */
function ReconciliationMetric({ accentClassName, amount, label, surfaceClassName }: ReconciliationMetricProps) {
  return (
    <div
      className={`relative min-h-132px overflow-hidden border border-[var(--ant-color-border-secondary)] rd-12px p-20px ${surfaceClassName}`}
    >
      <span className={`absolute right-0 top-0 h-44px w-44px rd-bl-44px ${accentClassName}`} />
      <div className="relative flex items-center gap-8px text-[var(--ant-color-text-secondary)]">
        <span className={`h-8px w-8px rd-full ${accentClassName}`} />
        <span className="text-13px font-500">{label}</span>
      </div>
      <div className="relative mt-20px text-28px text-[var(--ant-color-text)] font-700 leading-none tabular-nums">
        {formatMoney(amount)}
      </div>
    </div>
  );
}

/** 对账汇总与取货履约状态看板。 */
const DashboardOps = () => {
  const { t } = useTranslation();
  const [form] = AForm.useForm<DashboardSearchFormValues>();
  const defaultDateRange = useMemo(createDefaultDateRange, []);
  const [searchParams, setSearchParams] = useState<Api.Dashboard.SearchParams>(() =>
    createDashboardSearchParams(defaultDateRange)
  );
  const {
    data: reconciliation,
    isFetching: isReconciliationFetching,
    refetch: refetchReconciliation
  } = useDashboardReconciliation(searchParams);
  const {
    data: pickupStatuses,
    isFetching: isPickupStatusesFetching,
    refetch: refetchPickupStatuses
  } = useDashboardPickupStatuses(searchParams);
  const pickupTaskTotal = pickupStatuses?.reduce((total, item) => total + item.taskCount, 0) ?? 0;

  function handleSearch(values: DashboardSearchFormValues) {
    setSearchParams(createDashboardSearchParams(values.dateRange));
  }

  function handleReset() {
    const dateRange = createDefaultDateRange();
    form.setFieldValue('dateRange', dateRange);
    setSearchParams(createDashboardSearchParams(dateRange));
  }

  async function handleRefresh() {
    await Promise.allSettled([refetchReconciliation(), refetchPickupStatuses()]);
  }

  return (
    <ASpace
      className="w-full"
      direction="vertical"
      size={16}
    >
      <ACard
        className="overflow-hidden border border-t-3px border-[var(--ant-color-border-secondary)] border-t-[var(--ant-color-primary)] card-wrapper"
        size="small"
        variant="borderless"
      >
        <AForm
          className="w-full"
          form={form}
          initialValues={{ dateRange: defaultDateRange }}
          onFinish={handleSearch}
        >
          <div className="flex flex-col gap-16px lg:flex-row lg:items-center">
            <div className="flex shrink-0 items-center gap-10px lg:pr-8px">
              <div className="h-44px w-44px flex flex-center rd-12px bg-[var(--ant-color-primary-bg)]">
                <SvgIcon
                  className="text-22px text-[var(--ant-color-primary)]"
                  icon="mdi:calendar-range-outline"
                />
              </div>
              <div className="flex flex-col gap-2px">
                <span className="text-15px font-700 leading-none">{t('page.dashboard.period')}</span>
                <span className="text-12px text-[var(--ant-color-text-secondary)] leading-none">
                  {t('page.dashboard.periodStart')} / {t('page.dashboard.periodEnd')}
                </span>
              </div>
            </div>

            <div className="min-w-0 flex flex-col flex-1 gap-10px xl:flex-row xl:items-center">
              <AForm.Item
                className="m-0 min-w-0 flex-1 rounded-10px bg-[var(--ant-color-fill-quaternary)] p-4px"
                name="dateRange"
              >
                <ADatePicker.RangePicker
                  allowClear
                  aria-label={t('page.dashboard.period')}
                  className="w-full bg-transparent"
                  placeholder={[t('page.dashboard.periodStart'), t('page.dashboard.periodEnd')]}
                />
              </AForm.Item>

              <div className="flex shrink-0 flex-wrap gap-6px rounded-10px bg-[var(--ant-color-fill-quaternary)] p-4px">
                <AButton
                  htmlType="submit"
                  icon={<IconIcRoundSearch className="text-icon" />}
                  type="primary"
                >
                  {t('common.search')}
                </AButton>
                <AButton onClick={handleReset}>{t('common.reset')}</AButton>
                <AButton
                  icon={<IconIcRoundRefresh className="text-icon" />}
                  loading={isReconciliationFetching || isPickupStatusesFetching}
                  onClick={handleRefresh}
                >
                  {t('common.refresh')}
                </AButton>
              </div>
            </div>
          </div>
        </AForm>
      </ACard>

      <ACard
        className="overflow-hidden border border-[var(--ant-color-border-secondary)] card-wrapper"
        loading={isReconciliationFetching}
        size="small"
        variant="borderless"
      >
        {reconciliation ? (
          <>
            <div className="mb-20px flex flex-col gap-12px border-b border-[var(--ant-color-border-secondary)] pb-16px sm:flex-row sm:items-end sm:justify-between">
              <div className="flex items-center gap-10px">
                <span className="h-10px w-10px rd-full bg-[var(--ant-color-primary)] shadow-[0_0_0_5px_var(--ant-color-primary-bg)]" />
                <h2 className="m-0 text-18px font-700 leading-none">{t('page.dashboard.reconciliationSummary')}</h2>
              </div>
              <div className="flex items-baseline self-start gap-8px rounded-full bg-[var(--ant-color-fill-quaternary)] px-12px py-6px sm:self-auto">
                <span className="text-12px text-[var(--ant-color-text-secondary)]">
                  {t('page.dashboard.billCount')}
                </span>
                <strong className="text-18px font-700 leading-none tabular-nums">{reconciliation.billCount}</strong>
              </div>
            </div>

            <ARow gutter={[16, 16]}>
              <ACol
                lg={8}
                md={12}
                span={24}
              >
                <ReconciliationMetric
                  accentClassName="bg-[var(--ant-color-primary)]"
                  amount={reconciliation.receivableAmount}
                  label={t('page.dashboard.receivableAmount')}
                  surfaceClassName="bg-[var(--ant-color-primary-bg)]"
                />
              </ACol>
              <ACol
                lg={8}
                md={12}
                span={24}
              >
                <ReconciliationMetric
                  accentClassName="bg-[var(--ant-color-success)]"
                  amount={reconciliation.settledAmount}
                  label={t('page.dashboard.settledAmount')}
                  surfaceClassName="bg-[var(--ant-color-success-bg)]"
                />
              </ACol>
              <ACol
                lg={8}
                md={24}
                span={24}
              >
                <ReconciliationMetric
                  accentClassName="bg-[var(--ant-color-warning)]"
                  amount={reconciliation.pendingAmount}
                  label={t('page.dashboard.pendingAmount')}
                  surfaceClassName="bg-[var(--ant-color-warning-bg)]"
                />
              </ACol>
            </ARow>
          </>
        ) : (
          <div className="py-24px">
            <AEmpty description={t('common.noData')} />
          </div>
        )}
      </ACard>

      <ACard
        className="overflow-hidden border border-[var(--ant-color-border-secondary)] card-wrapper"
        loading={isPickupStatusesFetching}
        size="small"
        variant="borderless"
      >
        {pickupStatuses && pickupStatuses.length > 0 ? (
          <>
            <div className="mb-20px flex flex-col gap-12px border-b border-[var(--ant-color-border-secondary)] pb-16px sm:flex-row sm:items-end sm:justify-between">
              <div className="flex items-center gap-10px">
                <span className="h-10px w-10px rd-full bg-[var(--ant-color-primary)] shadow-[0_0_0_5px_var(--ant-color-primary-bg)]" />
                <h2 className="m-0 text-18px font-700 leading-none">{t('page.dashboard.pickupStatusSummary')}</h2>
              </div>
              <strong className="self-start text-24px font-700 leading-none tabular-nums sm:self-auto">
                {pickupTaskTotal}
              </strong>
            </div>

            <ARow gutter={[16, 16]}>
              {pickupStatuses.map(item => {
                const share = pickupTaskTotal > 0 ? (item.taskCount / pickupTaskTotal) * 100 : 0;

                return (
                  <ACol
                    key={item.pickupStatus}
                    lg={8}
                    md={12}
                    span={24}
                  >
                    <div className="min-h-132px flex flex-col justify-between border border-[var(--ant-color-border-secondary)] rd-12px bg-[var(--ant-color-fill-quaternary)] p-16px transition-shadow duration-200 hover:shadow-md">
                      <div className="flex items-center justify-between gap-12px">
                        <PickupTaskStatusBadge pickupStatus={item.pickupStatus} />
                        <span className="text-26px font-700 leading-none tabular-nums">{item.taskCount}</span>
                      </div>
                      <div className="mt-20px h-5px overflow-hidden rd-full bg-[var(--ant-color-fill-secondary)]">
                        <div
                          className="h-full rd-full bg-[var(--ant-color-primary)] transition-[width] duration-300"
                          style={{ width: `${share}%` }}
                        />
                      </div>
                    </div>
                  </ACol>
                );
              })}
            </ARow>
          </>
        ) : (
          <div className="py-24px">
            <AEmpty description={t('common.noData')} />
          </div>
        )}
      </ACard>
    </ASpace>
  );
};

export default DashboardOps;
