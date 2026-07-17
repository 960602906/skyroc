import { YES_OR_NO_MAP, yesOrNoRecord } from '@/constants/common';

interface YesOrNoBadgeProps {
  /** 是否：Y 是 / N 否；为空时不渲染 */
  value: CommonType.YesOrNo | null | undefined;
}

/** 全局是否徽标 */
function YesOrNoBadge({ value }: YesOrNoBadgeProps) {
  const { t } = useTranslation();

  if (value === null || value === undefined) {
    return null;
  }

  return (
    <ABadge
      status={YES_OR_NO_MAP[value]}
      text={t(yesOrNoRecord[value])}
    />
  );
}

export default YesOrNoBadge;
