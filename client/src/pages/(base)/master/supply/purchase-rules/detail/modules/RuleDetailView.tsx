import type { DescriptionsProps } from 'antd';

import { displayText, renderEnableStatus, renderPurchasePattern } from '@/features/crud';
import { useGoodsTypeOptions, usePurchaserOptions, useSupplierOptions, useWareOptions } from '@/service/hooks';

interface RuleDetailViewProps {
  detail: Api.PurchaseRule.Entity;
}

function resolveOptionLabel(id: string | null, options?: { id: string; name: string }[]) {
  if (!id) {
    return null;
  }

  return options?.find(item => item.id === id)?.name ?? id;
}

function RuleDetailView({ detail }: RuleDetailViewProps) {
  const { t } = useTranslation();

  const { data: goodsTypes } = useGoodsTypeOptions();
  const { data: suppliers } = useSupplierOptions();
  const { data: purchasers } = usePurchaserOptions();
  const { data: wares } = useWareOptions();

  const basicItems: DescriptionsProps['items'] = [
    { children: displayText(detail.name), key: 'name', label: t('page.purchase.rule.name') },
    { children: displayText(detail.code), key: 'code', label: t('page.purchase.rule.code') },
    {
      children: renderPurchasePattern(detail.purchasePattern),
      key: 'purchasePattern',
      label: t('page.purchase.rule.purchasePattern')
    }
  ];

  const relationItems: DescriptionsProps['items'] = [
    {
      children: displayText(resolveOptionLabel(detail.goodsTypeId, goodsTypes)),
      key: 'goodsTypeId',
      label: t('page.purchase.rule.goodsTypeId')
    },
    {
      children: displayText(resolveOptionLabel(detail.supplierId, suppliers)),
      key: 'supplierId',
      label: t('page.purchase.rule.supplierId')
    },
    {
      children: displayText(resolveOptionLabel(detail.purchaserId, purchasers)),
      key: 'purchaserId',
      label: t('page.purchase.rule.purchaserId')
    },
    {
      children: displayText(resolveOptionLabel(detail.wareId, wares)),
      key: 'wareId',
      label: t('page.purchase.rule.wareId')
    }
  ];

  const statusItems: DescriptionsProps['items'] = [
    {
      children: renderEnableStatus(detail.status),
      key: 'status',
      label: t('page.purchase.rule.status')
    },
    {
      children: displayText(detail.remark),
      key: 'remark',
      label: t('page.purchase.rule.remark'),
      span: 2
    },
    {
      children: displayText(detail.createTime),
      key: 'createTime',
      label: t('page.purchase.rule.detail.createTime')
    },
    {
      children: displayText(detail.updateTime),
      key: 'updateTime',
      label: t('page.purchase.rule.detail.updateTime')
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
        title={t('page.purchase.rule.sectionBasic')}
        variant="borderless"
      >
        <ADescriptions
          {...descProps}
          items={basicItems}
        />
      </ACard>

      <ACard
        className="card-wrapper"
        title={t('page.purchase.rule.sectionRelation')}
        variant="borderless"
      >
        <ADescriptions
          {...descProps}
          items={relationItems}
        />
      </ACard>

      <ACard
        className="card-wrapper"
        title={t('page.purchase.rule.sectionStatus')}
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

export default RuleDetailView;
