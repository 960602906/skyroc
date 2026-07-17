import { yesOrNoOptions } from '@/constants/common';

import YesOrNoBadge from './YesOrNoBadge';

type YesOrNoSelectProps = Omit<React.ComponentProps<typeof ASelect<CommonType.YesOrNo>>, 'options'>;

/** 全局是否下拉：选项与选中值均使用 YesOrNoBadge */
function YesOrNoSelect({ allowClear = true, ...props }: YesOrNoSelectProps) {
  const options = yesOrNoOptions.map(item => ({
    label: <YesOrNoBadge value={item.value as CommonType.YesOrNo} />,
    value: item.value as CommonType.YesOrNo
  }));

  return (
    <ASelect
      allowClear={allowClear}
      options={options}
      {...props}
    />
  );
}

export default YesOrNoSelect;
