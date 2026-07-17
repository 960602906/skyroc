import { enableStatusRecord } from '@/constants/business';
import { ATG_MAP } from '@/constants/common';

interface EnableStatusBadgeProps {
  /** 启用状态：1 启用，2 禁用；为空时不渲染 */
  status: Api.Common.EnableStatus | null | undefined;
}

/** 全局启用/禁用状态徽标：原点颜色按状态语义映射（启用 success / 禁用 warning）。 */
function EnableStatusBadge({ status }: EnableStatusBadgeProps) {
  const { t } = useTranslation();

  if (status === null || status === undefined) {
    return null;
  }

  return (
    <ABadge
      status={ATG_MAP[status]}
      text={t(enableStatusRecord[status])}
    />
  );
}

export default EnableStatusBadge;
