import { type LoaderFunctionArgs, redirect, useLoaderData } from 'react-router-dom';

import { renderAfterSaleStatus } from '@/features/crud';
import { useCloseTabAndNavigate } from '@/features/tab';
import { fetchGetAfterSaleDetail } from '@/service/api';

import AfterSaleDetailView from './modules/AfterSaleDetailView';

const LIST_PATH = '/orders/after-sales';

export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) {
    return redirect(LIST_PATH);
  }

  try {
    const detail = await fetchGetAfterSaleDetail(id);
    if (!detail) {
      return redirect(LIST_PATH);
    }
    return detail;
  } catch {
    return redirect(LIST_PATH);
  }
}

const AfterSaleDetailPage = () => {
  const { t } = useTranslation();
  const detail = useLoaderData() as Api.AfterSale.Entity;
  const closeTabAndNavigate = useCloseTabAndNavigate();

  return (
    <div className="h-full min-h-500px flex-col-stretch gap-16px overflow-auto">
      <ACard
        className="card-wrapper"
        extra={<AButton onClick={() => closeTabAndNavigate(LIST_PATH)}>{t('page.afterSale.detail.back')}</AButton>}
        title={detail.afterSaleNo}
        variant="borderless"
      >
        <ASpace size="middle">
          <span className="opacity-60">
            {t('page.afterSale.list.customerName')}：{detail.customerName}
          </span>
          {renderAfterSaleStatus(detail.afterStatus)}
        </ASpace>
      </ACard>

      <div className="flex-col-stretch gap-16px">
        <AfterSaleDetailView detail={detail} />
      </div>
    </div>
  );
};

export default AfterSaleDetailPage;
