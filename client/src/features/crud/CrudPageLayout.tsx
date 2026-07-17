import type { ReactNode } from 'react';

interface CrudPageLayoutProps {
  extra?: ReactNode;
  search: ReactNode;
  table: ReactNode;
  tableWrapperRef?: React.Ref<HTMLDivElement>;
  title: string;
}

const CrudPageLayout = ({ extra, search, table, tableWrapperRef, title }: CrudPageLayoutProps) => {
  const { t } = useTranslation();
  const isMobile = useMobile();

  return (
    <div className="h-full min-h-500px flex-col-stretch gap-16px overflow-hidden lt-sm:overflow-auto">
      <ACollapse
        bordered={false}
        className="card-wrapper"
        defaultActiveKey={isMobile ? undefined : '1'}
        items={[
          {
            children: search,
            key: '1',
            label: t('common.search')
          }
        ]}
      />

      <ACard
        className="flex-col-stretch sm:flex-1-hidden card-wrapper"
        extra={extra}
        ref={tableWrapperRef}
        title={title}
        variant="borderless"
      >
        {table}
      </ACard>
    </div>
  );
};

export default CrudPageLayout;
