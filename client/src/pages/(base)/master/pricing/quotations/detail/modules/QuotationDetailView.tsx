import type { DescriptionsProps } from 'antd';

import {
  DEFAULT_DETAIL_DESC_PROPS,
  DETAIL_EMPTY,
  displayText,
  renderBooleanTag,
  renderEnableStatus
} from '@/features/crud';
import { useCustomerOptions } from '@/service/hooks';

import QuotationGoodsSection from './QuotationGoodsSection';

interface QuotationDetailViewProps {
  detail: Api.Quotation.Entity;
  onGoodsChanged: () => void;
}

function QuotationDetailView({ detail, onGoodsChanged }: QuotationDetailViewProps) {
  const { t } = useTranslation();
  const nav = useNavigate();

  const { data: customers } = useCustomerOptions();

  const customerNameMap = new Map((customers ?? []).map(item => [item.id, item.name]));

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

  const basicItems: DescriptionsProps['items'] = [
    { children: displayText(detail.name), key: 'name', label: t('page.goods.quotation.name') },
    { children: displayText(detail.code), key: 'code', label: t('page.goods.quotation.code') },
    {
      children: displayText(detail.effectiveStart),
      key: 'effectiveStart',
      label: t('page.goods.quotation.effectiveStart')
    },
    {
      children: displayText(detail.effectiveEnd),
      key: 'effectiveEnd',
      label: t('page.goods.quotation.effectiveEnd')
    },
    {
      children: customerNodes,
      key: 'customerIds',
      label: t('page.goods.quotation.customerIds'),
      span: 2
    },
    {
      children: displayText(detail.description),
      key: 'description',
      label: t('page.goods.quotation.description'),
      span: 2
    }
  ];

  const statusItems: DescriptionsProps['items'] = [
    {
      children: renderBooleanTag(
        detail.isAudited,
        t('page.goods.quotation.audited'),
        t('page.goods.quotation.unaudited')
      ),
      key: 'isAudited',
      label: t('page.goods.quotation.isAudited')
    },
    {
      children: renderEnableStatus(detail.status),
      key: 'status',
      label: t('page.goods.quotation.status')
    },
    {
      children: displayText(detail.remark),
      key: 'remark',
      label: t('page.goods.quotation.remark'),
      span: 2
    },
    {
      children: displayText(detail.createTime),
      key: 'createTime',
      label: t('page.goods.quotation.detail.createTime')
    },
    {
      children: displayText(detail.updateTime),
      key: 'updateTime',
      label: t('page.goods.quotation.detail.updateTime')
    }
  ];

  return (
    <>
      <ACard
        className="card-wrapper"
        title={t('page.goods.quotation.sectionBasic')}
        variant="borderless"
      >
        <ADescriptions
          {...DEFAULT_DETAIL_DESC_PROPS}
          items={basicItems}
        />
      </ACard>

      <QuotationGoodsSection
        goods={detail.goods ?? []}
        quotationId={detail.id}
        onChanged={onGoodsChanged}
      />

      <ACard
        className="card-wrapper"
        title={t('page.goods.quotation.sectionStatus')}
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

export default QuotationDetailView;
