import type { DescriptionsProps } from 'antd';

import { DEFAULT_DETAIL_DESC_PROPS, displayText, renderEnableStatus } from '@/features/crud';

interface PurchaserDetailViewProps {
  detail: Api.Purchaser.Entity;
}

function PurchaserDetailView({ detail }: PurchaserDetailViewProps) {
  const { t } = useTranslation();

  const basicItems: DescriptionsProps['items'] = [
    { children: displayText(detail.name), key: 'name', label: t('page.purchase.purchaser.name') },
    { children: displayText(detail.code), key: 'code', label: t('page.purchase.purchaser.code') },
    {
      children: displayText(detail.phone),
      key: 'phone',
      label: t('page.purchase.purchaser.phone')
    },
    {
      children: displayText(detail.departmentName),
      key: 'departmentName',
      label: t('page.purchase.purchaser.departmentId')
    },
    {
      children: displayText(detail.userName),
      key: 'userName',
      label: t('page.purchase.purchaser.userName')
    }
  ];

  const statusItems: DescriptionsProps['items'] = [
    {
      children: renderEnableStatus(detail.status),
      key: 'status',
      label: t('page.purchase.purchaser.status')
    },
    {
      children: displayText(detail.remark),
      key: 'remark',
      label: t('page.purchase.purchaser.remark'),
      span: 2
    },
    {
      children: displayText(detail.createTime),
      key: 'createTime',
      label: t('page.purchase.purchaser.detail.createTime')
    },
    {
      children: displayText(detail.updateTime),
      key: 'updateTime',
      label: t('page.purchase.purchaser.detail.updateTime')
    }
  ];

  return (
    <>
      <ACard
        className="card-wrapper"
        title={t('page.purchase.purchaser.sectionBasic')}
        variant="borderless"
      >
        <ADescriptions
          {...DEFAULT_DETAIL_DESC_PROPS}
          items={basicItems}
        />
      </ACard>

      <ACard
        className="card-wrapper"
        title={t('page.purchase.purchaser.sectionStatus')}
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

export default PurchaserDetailView;
