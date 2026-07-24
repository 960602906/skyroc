import type { ReactNode } from 'react';

import { useCloseTabAndNavigate } from '@/features/tab';

interface OperatePageLayoutProps {
  /** 表单分区内容 */
  children: ReactNode;
  /** 列表页路径，取消时关签并跳转 */
  listPath: string;
  /** 确认按钮 loading */
  loading?: boolean;
  /** 确认回调 */
  onSave: () => void;
  /** 顶栏标题 */
  title: ReactNode;
}

/** 全页新增/编辑统一布局：顶栏取消/确认 + 分区表单容器。 */
const OperatePageLayout = ({ children, listPath, loading, onSave, title }: OperatePageLayoutProps) => {
  const { t } = useTranslation();
  const closeTabAndNavigate = useCloseTabAndNavigate();

  return (
    <div className="h-full min-h-500px flex-col-stretch gap-16px overflow-auto">
      <ACard
        className="card-wrapper"
        styles={{ body: { display: 'none' } }}
        title={title}
        variant="borderless"
        extra={
          <ASpace>
            <AButton onClick={() => closeTabAndNavigate(listPath)}>{t('common.cancel')}</AButton>
            <AButton
              loading={loading}
              type="primary"
              onClick={onSave}
            >
              {t('common.confirm')}
            </AButton>
          </ASpace>
        }
      />
      {children}
    </div>
  );
};

export default OperatePageLayout;
