import { enableStatusOptions } from '@/constants/business';
import { useFormRules } from '@/features/form';

interface EnableStatusFormItemProps {
  label: string;
  name?: string;
  required?: boolean;
}

function EnableStatusFormItem({ label, name = 'status', required = true }: EnableStatusFormItemProps) {
  const { t } = useTranslation();
  const { defaultRequiredRule } = useFormRules();

  return (
    <AForm.Item
      label={label}
      name={name}
      rules={required ? [defaultRequiredRule] : undefined}
    >
      <ARadio.Group>
        {enableStatusOptions.map(item => (
          <ARadio
            key={item.value}
            value={item.value}
          >
            {t(item.label)}
          </ARadio>
        ))}
      </ARadio.Group>
    </AForm.Item>
  );
}

export default EnableStatusFormItem;
