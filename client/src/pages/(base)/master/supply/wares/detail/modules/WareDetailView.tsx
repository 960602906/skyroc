import type { DescriptionsProps } from 'antd';

import { displayText, renderEnableStatus } from '@/features/crud';

interface WareDetailViewProps {
  detail: Api.Ware.Entity;
}

function WareDetailView({ detail }: WareDetailViewProps) {
  const { t } = useTranslation();

  const basicItems: DescriptionsProps['items'] = [
    { children: displayText(detail.name), key: 'name', label: t('page.storage.ware.name') },
    { children: displayText(detail.code), key: 'code', label: t('page.storage.ware.code') },
    {
      children: displayText(detail.contactName),
      key: 'contactName',
      label: t('page.storage.ware.contactName')
    },
    {
      children: displayText(detail.contactPhone),
      key: 'contactPhone',
      label: t('page.storage.ware.contactPhone')
    },
    {
      children: displayText(detail.address),
      key: 'address',
      label: t('page.storage.ware.address'),
      span: 2
    },
    {
      children: displayText(detail.sort),
      key: 'sort',
      label: t('page.storage.ware.sort')
    }
  ];

  const statusItems: DescriptionsProps['items'] = [
    {
      children: renderEnableStatus(detail.status),
      key: 'status',
      label: t('page.storage.ware.status')
    },
    {
      children: displayText(detail.remark),
      key: 'remark',
      label: t('page.storage.ware.remark'),
      span: 2
    },
    {
      children: displayText(detail.createTime),
      key: 'createTime',
      label: t('page.storage.ware.detail.createTime')
    },
    {
      children: displayText(detail.updateTime),
      key: 'updateTime',
      label: t('page.storage.ware.detail.updateTime')
    }
  ];

  const descProps: Pick<DescriptionsProps, 'column' | 'size'> = {
    column: { lg: 2, md: 2, sm: 1, xs: 1 },
    size: 'middle'
  };

  return (
    <>
      <ACard
        className="card-wrapper"
        title={t('page.storage.ware.sectionBasic')}
        variant="borderless"
      >
        <ADescriptions
          {...descProps}
          items={basicItems}
        />
      </ACard>

      <ACard
        className="card-wrapper"
        title={t('page.storage.ware.sectionStatus')}
        variant="borderless"
      >
        <ADescriptions
          {...descProps}
          items={statusItems}
        />
      </ACard>
    </>
  );
}

export default WareDetailView;
