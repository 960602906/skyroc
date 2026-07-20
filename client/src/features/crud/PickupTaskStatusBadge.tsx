import { pickupTaskStatusRecord } from '@/constants/business';
import { PICKUP_TASK_STATUS_MAP } from '@/constants/common';

interface PickupTaskStatusBadgeProps {
  /** 取货任务履约状态；为空时不渲染。 */
  pickupStatus: Api.AfterSale.PickupStatus | null | undefined;
}

/** 售后取货任务履约状态徽标。 */
function PickupTaskStatusBadge({ pickupStatus }: PickupTaskStatusBadgeProps) {
  const { t } = useTranslation();

  if (pickupStatus === null || pickupStatus === undefined) {
    return null;
  }

  return (
    <ABadge
      status={PICKUP_TASK_STATUS_MAP[pickupStatus]}
      text={t(pickupTaskStatusRecord[pickupStatus])}
    />
  );
}

export default PickupTaskStatusBadge;
