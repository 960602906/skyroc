interface BooleanYesNoBadgeProps {
  /** 否文案，默认 common.yesOrNo.no */
  falseText?: string;
  /** 是文案，默认 common.yesOrNo.yes */
  trueText?: string;
  /** 布尔是否；为空时不渲染 */
  value: boolean | null | undefined;
}

/** 布尔是否徽标（true/false，区别于 Y/N 的 YesOrNoBadge） */
function BooleanYesNoBadge({ falseText, trueText, value }: BooleanYesNoBadgeProps) {
  const { t } = useTranslation();

  if (value === null || value === undefined) {
    return null;
  }

  return (
    <ABadge
      status={value ? 'success' : 'default'}
      text={value ? (trueText ?? t('common.yesOrNo.yes')) : (falseText ?? t('common.yesOrNo.no'))}
    />
  );
}

export default BooleanYesNoBadge;
