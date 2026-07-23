import type { DescriptionsProps } from 'antd';

import { DEFAULT_DETAIL_DESC_PROPS, DETAIL_EMPTY, displayText, renderEnableStatus } from '@/features/crud';
import { useCustomerOptions, useQuotationOptions } from '@/service/hooks';

import ProtocolGoodsSection from './ProtocolGoodsSection';

interface ProtocolDetailViewProps {
  detail: Api.CustomerProtocol.Entity;
  onGoodsChanged: () => void;
}

function ProtocolDetailView({ detail, onGoodsChanged }: ProtocolDetailViewProps) {
  const { t } = useTranslation();
  const nav = useNavigate();

  const { data: customers } = useCustomerOptions();
  const { data: quotationOptions } = useQuotationOptions();

  const customerNameMap = new Map((customers ?? []).map(item => [item.id, item.name]));
  const quotationNameMap = new Map((quotationOptions ?? []).map(item => [item.value, item.label]));

  const customerNodes = detail.customerIds?.length ? (
    <ASpace
      wrap
      size={[4, 4]}
    >
      {detail.customerIds.map(id => {
        const name = customerNameMap.get(id) || id;
        return (
          <AButton
            className="h-auto p-0 leading-normal"
            key={id}
            size="small"
            type="link"
            onClick={() => nav(`/master/customer/detail/${id}`)}
          >
            {name}
          </AButton>
        );
      })}
    </ASpace>
  ) : (
    DETAIL_EMPTY
  );

  const quotationNode = detail.quotationId ? (
    <AButton
      className="h-auto p-0 leading-normal"
      size="small"
      type="link"
      onClick={() => nav(`/master/pricing/quotations/detail/${detail.quotationId}`)}
    >
      {displayText(detail.quotationName || quotationNameMap.get(detail.quotationId) || detail.quotationId)}
    </AButton>
  ) : (
    DETAIL_EMPTY
  );

  const basicItems: DescriptionsProps['items'] = [
    { children: displayText(detail.name), key: 'name', label: t('page.customer.protocol.name') },
    { children: displayText(detail.code), key: 'code', label: t('page.customer.protocol.code') },
    {
      children: displayText(detail.effectiveStart),
      key: 'effectiveStart',
      label: t('page.customer.protocol.effectiveStart')
    },
    {
      children: displayText(detail.effectiveEnd),
      key: 'effectiveEnd',
      label: t('page.customer.protocol.effectiveEnd')
    },
    {
      children: quotationNode,
      key: 'quotationId',
      label: t('page.customer.protocol.quotationId')
    },
    {
      children: customerNodes,
      key: 'customerIds',
      label: t('page.customer.protocol.customerIds'),
      span: 2
    }
  ];

  const statusItems: DescriptionsProps['items'] = [
    {
      children: renderEnableStatus(detail.status),
      key: 'status',
      label: t('page.customer.protocol.status')
    },
    {
      children: displayText(detail.remark),
      key: 'remark',
      label: t('page.customer.protocol.remark'),
      span: 2
    },
    {
      children: displayText(detail.createTime),
      key: 'createTime',
      label: t('page.customer.protocol.detail.createTime')
    },
    {
      children: displayText(detail.updateTime),
      key: 'updateTime',
      label: t('page.customer.protocol.detail.updateTime')
    }
  ];

  return (
    <>
      <ACard
        className="card-wrapper"
        title={t('page.customer.protocol.sectionBasic')}
        variant="borderless"
      >
        <ADescriptions
          {...DEFAULT_DETAIL_DESC_PROPS}
          items={basicItems}
        />
      </ACard>

      <ProtocolGoodsSection
        customerProtocolId={detail.id}
        goods={detail.goods ?? []}
        onChanged={onGoodsChanged}
      />

      <ACard
        className="card-wrapper"
        title={t('page.customer.protocol.sectionStatus')}
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

export default ProtocolDetailView;
