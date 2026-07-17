import { enableStatusOptions } from '@/constants/business';

import EnableStatusBadge from './EnableStatusBadge';

type EnableStatusSelectProps = Omit<React.ComponentProps<typeof ASelect<Api.Common.EnableStatus>>, 'options'>;

/** 全局启用/禁用状态下拉：选项与选中值均使用 EnableStatusBadge 展示。 */
function EnableStatusSelect({ allowClear = true, ...props }: EnableStatusSelectProps) {
  const options = enableStatusOptions.map(item => ({
    label: <EnableStatusBadge status={item.value as Api.Common.EnableStatus} />,
    value: item.value as Api.Common.EnableStatus
  }));

  return (
    <ASelect
      allowClear={allowClear}
      options={options}
      {...props}
    />
  );
}

export default EnableStatusSelect;
