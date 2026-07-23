import type { ReactNode } from 'react';

import { useCloseTabAndNavigate } from '@/features/tab';

interface DetailPageLayoutProps {
  /** 返回按钮文案 */
  backLabel: string;
  /** 顶部卡片标题下的描述节点（如编号、状态 Badge 组合） */
  banner?: ReactNode;
  /** 各分区内容（ACard 组合） */
  children: ReactNode;
  /** 右上角操作区（编辑/审核/删除等），返回按钮由组件自带 */
  extra?: ReactNode;
  /** 列表页路径，用于「返回」按钮关闭当前页签并跳转 */
  listPath: string;
  /** 实体主标题（如订单号、客户名） */
  title: ReactNode;
}

/** 详情页统一布局：顶部带返回/操作的 banner 卡片 + 分区内容容器。 */
const DetailPageLayout = ({ backLabel, banner, children, extra, listPath, title }: DetailPageLayoutProps) => {
  const closeTabAndNavigate = useCloseTabAndNavigate();

  return (
    <div className="h-full min-h-500px flex-col-stretch gap-16px overflow-auto">
      <ACard
        className="card-wrapper"
        title={title}
        variant="borderless"
        extra={
          <ASpace>
            <AButton onClick={() => closeTabAndNavigate(listPath)}>{backLabel}</AButton>
            {extra}
          </ASpace>
        }
      >
        {banner !== undefined && <div className="opacity-60">{banner}</div>}
      </ACard>

      <div className="flex-col-stretch gap-16px">{children}</div>
    </div>
  );
};

export default DetailPageLayout;
