import type { DescriptionsProps } from 'antd';

import { displayText, renderEnableStatus } from '@/features/crud';

interface SupplierDetailViewProps {
  detail: Api.Supplier.Entity;
}

function SupplierDetailView({ detail }: SupplierDetailViewProps) {
  const { t } = useTranslation();

  const basicItems: DescriptionsProps['items'] = [
    { children: displayText(detail.name), key: 'name', label: t('page.purchase.supplier.name') },
    { children: displayText(detail.code), key: 'code', label: t('page.purchase.supplier.code') },
    {
      children: displayText(detail.contactName),
      key: 'contactName',
      label: t('page.purchase.supplier.contactName')
    },
    {
      children: displayText(detail.contactPhone),
      key: 'contactPhone',
      label: t('page.purchase.supplier.contactPhone')
    },
    {
      children: displayText(detail.address),
      key: 'address',
      label: t('page.purchase.supplier.address'),
      span: 2
    }
  ];

  const financeItems: DescriptionsProps['items'] = [
    {
      children: displayText(detail.taxNo),
      key: 'taxNo',
      label: t('page.purchase.supplier.taxNo')
    },
    {
      children: displayText(detail.bankName),
      key: 'bankName',
      label: t('page.purchase.supplier.bankName')
    },
    {
      children: displayText(detail.bankAccount),
      key: 'bankAccount',
      label: t('page.purchase.supplier.bankAccount'),
      span: 2
    }
  ];

  const statusItems: DescriptionsProps['items'] = [
    {
      children: renderEnableStatus(detail.status),
      key: 'status',
      label: t('page.purchase.supplier.status')
    },
    {
      children: displayText(detail.remark),
      key: 'remark',
      label: t('page.purchase.supplier.remark'),
      span: 2
    },
    {
      children: displayText(detail.createTime),
      key: 'createTime',
      label: t('page.purchase.supplier.detail.createTime')
    },
    {
      children: displayText(detail.updateTime),
      key: 'updateTime',
      label: t('page.purchase.supplier.detail.updateTime')
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
        title={t('page.purchase.supplier.sectionBasic')}
        variant="borderless"
      >
        <ADescriptions
          {...descProps}
          items={basicItems}
        />
      </ACard>

      <ACard
        className="card-wrapper"
        title={t('page.purchase.supplier.sectionFinance')}
        variant="borderless"
      >
        <ADescriptions
          {...descProps}
          items={financeItems}
        />
      </ACard>

      <ACard
        className="card-wrapper"
        title={t('page.purchase.supplier.sectionStatus')}
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

export default SupplierDetailView;
