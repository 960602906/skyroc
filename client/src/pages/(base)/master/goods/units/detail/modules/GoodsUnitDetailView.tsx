import type { DescriptionsProps } from 'antd';

import { renderBooleanTag, renderEnableStatus } from '@/features/crud';

interface GoodsUnitDetailViewProps {
  detail: Api.GoodsUnit.Entity;
}

const EMPTY = '-';

function displayText(value: string | number | boolean | null | undefined) {
  if (value === null || value === undefined || value === '') {
    return EMPTY;
  }
  return String(value);
}

function GoodsUnitDetailView({ detail }: GoodsUnitDetailViewProps) {
  const { t } = useTranslation();
  const nav = useNavigate();

  const goodsLabel = detail.goodsName || detail.goodsCode || detail.goodsId;

  const basicItems: DescriptionsProps['items'] = [
    {
      children: detail.goodsId ? (
        <AButton
          className="h-auto p-0 leading-normal"
          size="small"
          type="link"
          onClick={() => nav(`/master/goods/detail/${detail.goodsId}`)}
        >
          {goodsLabel}
        </AButton>
      ) : (
        EMPTY
      ),
      key: 'goodsName',
      label: t('page.goods.unit.goodsId')
    },
    {
      children: displayText(detail.goodsCode),
      key: 'goodsCode',
      label: t('page.goods.unit.goodsCode')
    },
    { children: displayText(detail.name), key: 'name', label: t('page.goods.unit.name') },
    { children: displayText(detail.code), key: 'code', label: t('page.goods.unit.code') },
    {
      children: displayText(detail.conversionRate),
      key: 'conversionRate',
      label: t('page.goods.unit.conversionRate')
    },
    {
      children: renderBooleanTag(detail.isBaseUnit, t('common.yesOrNo.yes'), t('common.yesOrNo.no')),
      key: 'isBaseUnit',
      label: t('page.goods.unit.isBaseUnit')
    },
    { children: displayText(detail.sort), key: 'sort', label: t('page.goods.unit.sort') }
  ];

  const statusItems: DescriptionsProps['items'] = [
    {
      children: renderEnableStatus(detail.status),
      key: 'status',
      label: t('page.goods.unit.status')
    },
    {
      children: displayText(detail.remark),
      key: 'remark',
      label: t('page.goods.unit.remark'),
      span: 2
    },
    {
      children: displayText(detail.createTime),
      key: 'createTime',
      label: t('page.goods.unit.detail.createTime')
    },
    {
      children: displayText(detail.updateTime),
      key: 'updateTime',
      label: t('page.goods.unit.detail.updateTime')
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
        title={t('page.goods.unit.sectionBasic')}
        variant="borderless"
      >
        <ADescriptions
          {...descProps}
          items={basicItems}
        />
      </ACard>

      <ACard
        className="card-wrapper"
        title={t('page.goods.unit.sectionStatus')}
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

export default GoodsUnitDetailView;
