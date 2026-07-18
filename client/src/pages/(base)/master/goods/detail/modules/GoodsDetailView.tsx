import type { DescriptionsProps } from 'antd';

import { renderBooleanTag, renderEnableStatus } from '@/features/crud';
import {
  toOptions,
  useGoodsTypeTreeOptions,
  useGoodsUnitOptions,
  useSupplierOptions,
  useWareOptions
} from '@/service/hooks';

import { findGoodsTypeName } from '../../utils/tree';

interface GoodsDetailViewProps {
  detail: Api.Goods.Entity;
}

const EMPTY = '-';

function displayText(value: string | number | null | undefined) {
  if (value === null || value === undefined || value === '') {
    return EMPTY;
  }
  return String(value);
}

function GoodsDetailView({ detail }: GoodsDetailViewProps) {
  const { t } = useTranslation();
  const { data: goodsTypeTree } = useGoodsTypeTreeOptions();
  const { data: unitOptions = [] } = useGoodsUnitOptions();
  const { data: suppliers } = useSupplierOptions();
  const { data: wares } = useWareOptions();

  const supplierOptions = toOptions(suppliers);
  const wareOptions = toOptions(wares);

  const goodsTypeName = findGoodsTypeName(goodsTypeTree, detail.goodsTypeId);
  const baseUnitName = unitOptions.find(item => item.value === detail.baseUnitId)?.label;
  const defaultWareName = wareOptions.find(item => item.value === detail.defaultWareId)?.label;
  const defaultSupplierName = supplierOptions.find(item => item.value === detail.defaultSupplierId)?.label;
  const supplierNames =
    detail.supplierIds
      ?.map(id => supplierOptions.find(item => item.value === id)?.label)
      .filter(Boolean)
      .join('、') || EMPTY;

  const basicItems: DescriptionsProps['items'] = [
    { children: displayText(detail.name), key: 'name', label: t('page.goods.operate.name') },
    { children: displayText(detail.code), key: 'code', label: t('page.goods.operate.code') },
    {
      children: displayText(goodsTypeName),
      key: 'goodsTypeId',
      label: t('page.goods.operate.goodsTypeId')
    },
    { children: displayText(detail.spec), key: 'spec', label: t('page.goods.operate.spec') },
    { children: displayText(detail.brand), key: 'brand', label: t('page.goods.operate.brand') },
    { children: displayText(detail.origin), key: 'origin', label: t('page.goods.operate.origin') },
    {
      children: displayText(detail.description),
      key: 'description',
      label: t('page.goods.operate.description'),
      span: 2
    }
  ];

  const supplyItems: DescriptionsProps['items'] = [
    {
      children: displayText(baseUnitName),
      key: 'baseUnitId',
      label: t('page.goods.operate.baseUnitId')
    },
    {
      children: displayText(defaultWareName),
      key: 'defaultWareId',
      label: t('page.goods.operate.defaultWareId')
    },
    {
      children: displayText(defaultSupplierName),
      key: 'defaultSupplierId',
      label: t('page.goods.operate.defaultSupplierId')
    },
    {
      children: supplierNames,
      key: 'supplierIds',
      label: t('page.goods.operate.supplierIds')
    },
    {
      children: detail.taxRate === null || detail.taxRate === undefined ? EMPTY : `${detail.taxRate}%`,
      key: 'taxRate',
      label: t('page.goods.operate.taxRate')
    }
  ];

  const saleItems: DescriptionsProps['items'] = [
    {
      children: renderBooleanTag(detail.isOnSale, t('page.goods.list.onSale'), t('page.goods.list.offSale')),
      key: 'isOnSale',
      label: t('page.goods.operate.isOnSale')
    },
    {
      children: renderEnableStatus(detail.status),
      key: 'status',
      label: t('page.goods.operate.status')
    },
    {
      children: displayText(detail.remark),
      key: 'remark',
      label: t('page.goods.operate.remark'),
      span: 2
    },
    {
      children: displayText(detail.createTime),
      key: 'createTime',
      label: t('page.goods.list.createTime')
    },
    {
      children: displayText(detail.updateTime),
      key: 'updateTime',
      label: t('page.goods.detail.updateTime')
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
        title={t('page.goods.operate.sectionBasic')}
        variant="borderless"
      >
        <ADescriptions
          {...descProps}
          items={basicItems}
        />
      </ACard>

      <ACard
        className="card-wrapper"
        title={t('page.goods.operate.sectionSupply')}
        variant="borderless"
      >
        <ADescriptions
          {...descProps}
          items={supplyItems}
        />
      </ACard>

      <ACard
        className="card-wrapper"
        title={t('page.goods.operate.sectionSale')}
        variant="borderless"
      >
        <ADescriptions
          {...descProps}
          items={saleItems}
        />
      </ACard>
    </>
  );
}

export default GoodsDetailView;
