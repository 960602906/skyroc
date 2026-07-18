import { useQuery } from '@tanstack/react-query';
import type { DescriptionsProps } from 'antd';

import { displayText, renderEnableStatus } from '@/features/crud';
import { fetchGetCustomerTagTree } from '@/service/api';
import { QUERY_KEYS } from '@/service/keys';

type TagTreeNode = Api.CustomerTag.Entity & { children?: TagTreeNode[] };

interface CustomerTagDetailViewProps {
  detail: Api.CustomerTag.Entity;
}

function findTagName(nodes: TagTreeNode[] | undefined, id: string | null | undefined): string | null {
  if (!nodes?.length || !id) {
    return null;
  }

  for (const node of nodes) {
    if (node.id === id) {
      return node.name;
    }
    const childName = findTagName(node.children, id);
    if (childName) {
      return childName;
    }
  }

  return null;
}

function CustomerTagDetailView({ detail }: CustomerTagDetailViewProps) {
  const { t } = useTranslation();

  const { data: tagTree } = useQuery({
    queryFn: () => fetchGetCustomerTagTree(),
    queryKey: QUERY_KEYS.BASE.CUSTOMER_TAGS
  });

  const parentName = findTagName(tagTree as TagTreeNode[] | undefined, detail.parentId);

  const basicItems: DescriptionsProps['items'] = [
    { children: displayText(detail.name), key: 'name', label: t('page.customer.tag.name') },
    { children: displayText(detail.code), key: 'code', label: t('page.customer.tag.code') },
    {
      children: displayText(parentName),
      key: 'parentId',
      label: t('page.customer.tag.parentId')
    },
    { children: displayText(detail.sort), key: 'sort', label: t('page.customer.tag.sort') }
  ];

  const statusItems: DescriptionsProps['items'] = [
    {
      children: renderEnableStatus(detail.status),
      key: 'status',
      label: t('page.customer.tag.status')
    },
    {
      children: displayText(detail.remark),
      key: 'remark',
      label: t('page.customer.tag.remark'),
      span: 2
    },
    {
      children: displayText(detail.createTime),
      key: 'createTime',
      label: t('page.customer.tag.detail.createTime')
    },
    {
      children: displayText(detail.updateTime),
      key: 'updateTime',
      label: t('page.customer.tag.detail.updateTime')
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
        title={t('page.customer.tag.sectionBasic')}
        variant="borderless"
      >
        <ADescriptions
          {...descProps}
          items={basicItems}
        />
      </ACard>

      <ACard
        className="card-wrapper"
        title={t('page.customer.tag.sectionStatus')}
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

export default CustomerTagDetailView;
