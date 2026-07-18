import BooleanYesNoBadge from './BooleanYesNoBadge';
import { type BooleanSelectValue, booleanToSelectValue, selectValueToBoolean } from './boolean-utils';

type BooleanYesNoSelectProps = Omit<
  React.ComponentProps<typeof ASelect<BooleanSelectValue>>,
  'onChange' | 'options' | 'value'
> & {
  /** 否选项文案，默认「否」 */
  falseText?: string;
  onChange?: (value: boolean | null) => void;
  /** 是选项文案，默认「是」 */
  trueText?: string;
  /** 外部始终使用 boolean */
  value?: boolean | null;
};

/** 布尔是否下拉（对外 value 为 true/false）。 内部用 1/0 适配 antd Select；Y/N 字符串场景请用 YesOrNoSelect。 */
function BooleanYesNoSelect({
  allowClear = true,
  falseText,
  onChange,
  trueText,
  value,
  ...props
}: BooleanYesNoSelectProps) {
  const { t } = useTranslation();

  const yesText = trueText ?? t('common.yesOrNo.yes');
  const noText = falseText ?? t('common.yesOrNo.no');

  const options = [
    {
      label: (
        <BooleanYesNoBadge
          value
          trueText={yesText}
        />
      ),
      value: 1 as BooleanSelectValue
    },
    {
      label: (
        <BooleanYesNoBadge
          falseText={noText}
          value={false}
        />
      ),
      value: 0 as BooleanSelectValue
    }
  ];

  return (
    <ASelect
      allowClear={allowClear}
      options={options}
      value={booleanToSelectValue(value)}
      onChange={next => {
        onChange?.(selectValueToBoolean(next) ?? null);
      }}
      {...props}
    />
  );
}

export default BooleanYesNoSelect;
