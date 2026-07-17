import { Card, Result } from 'antd';
import { useTranslation } from 'react-i18next';

/** Temporary stub for menus that are not implemented yet */
const PagePlaceholder = () => {
  const { t } = useTranslation();

  return (
    <Card className="h-full card-wrapper">
      <Result
        status="info"
        title={t('common.lookForward')}
      />
    </Card>
  );
};

export default PagePlaceholder;
