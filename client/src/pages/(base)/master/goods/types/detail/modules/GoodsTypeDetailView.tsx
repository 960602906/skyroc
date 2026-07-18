import type { DescriptionsProps } from 'antd';

import { DETAIL_EMPTY, displayText, renderBooleanTag, renderEnableStatus } from '@/features/crud';
import { useGoodsTypeTreeOptions } from '@/service/hooks';

import { findGoodsTypeName } from '../../../utils/tree';

interface GoodsTypeDetailViewProps {
  detail: Api.GoodsType.Entity;
}

function GoodsTypeDetailView({ detail }: GoodsTypeDetailViewProps) {
  const { t } = useTranslation();
  const { data: goodsTypeTree } = useGoodsTypeTreeOptions();

  const parentName = findGoodsTypeName(goodsTypeTree, detail.parentId);

  const basicItems: DescriptionsProps['items'] = [
    { children: displayText(detail.name), key: 'name', label: t('page.goods.type.name') },
    { children: displayText(detail.code), key: 'code', label: t('page.goods.type.code') },
    {
      children: displayText(parentName),
      key: 'parentId',
      label: t('page.goods.type.parentId')
    },
    { children: displayText(detail.sort), key: 'sort', label: t('page.goods.type.sort') }
  ];

  const taxItems: DescriptionsProps['items'] = [
    {
      children:
        detail.defaultTaxRate === null || detail.defaultTaxRate === undefined
          ? DETAIL_EMPTY
          : `${detail.defaultTaxRate}%`,
      key: 'defaultTaxRate',
      label: t('page.goods.type.defaultTaxRate')
    },
    {
      children: renderBooleanTag(detail.isTaxExempt, t('common.yesOrNo.yes'), t('common.yesOrNo.no')),
      key: 'isTaxExempt',
      label: t('page.goods.type.isTaxExempt')
    },
    {
      children: displayText(detail.taxCategoryCode),
      key: 'taxCategoryCode',
      label: t('page.goods.type.taxCategoryCode')
    },
    {
      children: displayText(detail.taxCategoryName),
      key: 'taxCategoryName',
      label: t('page.goods.type.taxCategoryName')
    },
    {
      children: displayText(detail.invoiceGoodsShortName),
      key: 'invoiceGoodsShortName',
      label: t('page.goods.type.invoiceGoodsShortName')
    },
    {
      children: displayText(detail.taxPolicyBasis),
      key: 'taxPolicyBasis',
      label: t('page.goods.type.taxPolicyBasis'),
      span: 2
    }
  ];

  const statusItems: DescriptionsProps['items'] = [
    {
      children: renderEnableStatus(detail.status),
      key: 'status',
      label: t('page.goods.type.status')
    },
    {
      children: displayText(detail.remark),
      key: 'remark',
      label: t('page.goods.type.remark'),
      span: 2
    },
    {
      children: displayText(detail.createTime),
      key: 'createTime',
      label: t('page.goods.type.detail.createTime')
    },
    {
      children: displayText(detail.updateTime),
      key: 'updateTime',
      label: t('page.goods.type.detail.updateTime')
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
        title={t('page.goods.type.sectionBasic')}
        variant="borderless"
      >
        <ADescriptions
          {...descProps}
          items={basicItems}
        />
      </ACard>

      <ACard
        className="card-wrapper"
        title={t('page.goods.type.sectionTax')}
        variant="borderless"
      >
        <ADescriptions
          {...descProps}
          items={taxItems}
        />
      </ACard>

      <ACard
        className="card-wrapper"
        title={t('page.goods.type.sectionStatus')}
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

export default GoodsTypeDetailView;
