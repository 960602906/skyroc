import type { DescriptionsProps } from 'antd';

import { DETAIL_EMPTY, displayText, renderEnableStatus } from '@/features/crud';
import { useCompanyOptions, useCustomerOptions } from '@/service/hooks';

interface SubAccountDetailViewProps {
  detail: Api.CustomerSubAccount.Entity;
}

function findName(items: Array<{ id: string; name: string }> | undefined, id: string | null | undefined) {
  if (!items?.length || !id) {
    return null;
  }
  return items.find(item => item.id === id)?.name ?? null;
}

function SubAccountDetailView({ detail }: SubAccountDetailViewProps) {
  const { t } = useTranslation();
  const nav = useNavigate();

  const { data: companies } = useCompanyOptions();
  const { data: customers } = useCustomerOptions();

  const companyName = findName(companies, detail.companyId);
  const customerName = findName(customers, detail.customerId);

  const basicItems: DescriptionsProps['items'] = [
    {
      children: detail.companyId ? (
        <AButton
          className="h-auto p-0 leading-normal"
          size="small"
          type="link"
          onClick={() => nav(`/master/customer/companies/detail/${detail.companyId}`)}
        >
          {companyName || detail.companyId}
        </AButton>
      ) : (
        DETAIL_EMPTY
      ),
      key: 'companyId',
      label: t('page.customer.subAccount.companyId')
    },
    {
      children: detail.customerId ? (
        <AButton
          className="h-auto p-0 leading-normal"
          size="small"
          type="link"
          onClick={() => nav(`/master/customer/detail/${detail.customerId}`)}
        >
          {customerName || detail.customerId}
        </AButton>
      ) : (
        DETAIL_EMPTY
      ),
      key: 'customerId',
      label: t('page.customer.subAccount.customerId')
    },
    {
      children: displayText(detail.username),
      key: 'username',
      label: t('page.customer.subAccount.username')
    },
    {
      children: displayText(detail.nickName),
      key: 'nickName',
      label: t('page.customer.subAccount.nickName')
    },
    {
      children: displayText(detail.phone),
      key: 'phone',
      label: t('page.customer.subAccount.phone')
    },
    {
      children: displayText(detail.email),
      key: 'email',
      label: t('page.customer.subAccount.email')
    }
  ];

  const statusItems: DescriptionsProps['items'] = [
    {
      children: renderEnableStatus(detail.status),
      key: 'status',
      label: t('page.customer.subAccount.status')
    },
    {
      children: displayText(detail.remark),
      key: 'remark',
      label: t('page.customer.subAccount.remark'),
      span: 2
    },
    {
      children: displayText(detail.createTime),
      key: 'createTime',
      label: t('page.customer.subAccount.detail.createTime')
    },
    {
      children: displayText(detail.updateTime),
      key: 'updateTime',
      label: t('page.customer.subAccount.detail.updateTime')
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
        title={t('page.customer.subAccount.sectionBasic')}
        variant="borderless"
      >
        <ADescriptions
          {...descProps}
          items={basicItems}
        />
      </ACard>

      <ACard
        className="card-wrapper"
        title={t('page.customer.subAccount.sectionStatus')}
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

export default SubAccountDetailView;
