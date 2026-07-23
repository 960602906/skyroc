import type { DescriptionsProps } from 'antd';

import { DEFAULT_DETAIL_DESC_PROPS, displayText, renderEnableStatus } from '@/features/crud';

interface CompanyDetailViewProps {
  detail: Api.Company.Entity;
}

function CompanyDetailView({ detail }: CompanyDetailViewProps) {
  const { t } = useTranslation();

  const basicItems: DescriptionsProps['items'] = [
    { children: displayText(detail.name), key: 'name', label: t('page.customer.company.name') },
    { children: displayText(detail.code), key: 'code', label: t('page.customer.company.code') },
    {
      children: displayText(detail.contactName),
      key: 'contactName',
      label: t('page.customer.company.contactName')
    },
    {
      children: displayText(detail.contactPhone),
      key: 'contactPhone',
      label: t('page.customer.company.contactPhone')
    },
    {
      children: displayText(detail.address),
      key: 'address',
      label: t('page.customer.company.address'),
      span: 2
    }
  ];

  const statusItems: DescriptionsProps['items'] = [
    {
      children: renderEnableStatus(detail.status),
      key: 'status',
      label: t('page.customer.company.status')
    },
    {
      children: displayText(detail.remark),
      key: 'remark',
      label: t('page.customer.company.remark'),
      span: 2
    },
    {
      children: displayText(detail.createTime),
      key: 'createTime',
      label: t('page.customer.company.detail.createTime')
    },
    {
      children: displayText(detail.updateTime),
      key: 'updateTime',
      label: t('page.customer.company.detail.updateTime')
    }
  ];

  return (
    <>
      <ACard
        className="card-wrapper"
        title={t('page.customer.company.sectionBasic')}
        variant="borderless"
      >
        <ADescriptions
          {...DEFAULT_DETAIL_DESC_PROPS}
          items={basicItems}
        />
      </ACard>

      <ACard
        className="card-wrapper"
        title={t('page.customer.company.sectionStatus')}
        variant="borderless"
      >
        <ADescriptions
          {...DEFAULT_DETAIL_DESC_PROPS}
          items={statusItems}
        />
      </ACard>
    </>
  );
}

export default CompanyDetailView;
