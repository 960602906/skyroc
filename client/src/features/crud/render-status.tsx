import { enableStatusRecord } from '@/constants/business';
import { ATG_MAP } from '@/constants/common';

export function renderEnableStatus(status: Api.Common.EnableStatus | null, t: App.I18n.$T) {
  if (status === null) {
    return null;
  }

  const label = t(enableStatusRecord[status]);

  return <ATag color={ATG_MAP[status]}>{label}</ATag>;
}

export function renderBooleanTag(value: boolean, trueText: string, falseText: string) {
  return <ATag color={value ? 'success' : 'default'}>{value ? trueText : falseText}</ATag>;
}
