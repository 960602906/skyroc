import { type LoaderFunctionArgs, redirect, useLoaderData } from 'react-router-dom';

import { renderSaleOrderStatus } from '@/features/crud';
import { useCloseTabAndNavigate } from '@/features/tab';
import { fetchGetOrderDetail } from '@/service/api';
import { SaleOrderStatus } from '@/service/enums';

import OrderDetailView from './modules/OrderDetailView';

const LIST_PATH = '/orders/list';

export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) {
    return redirect(LIST_PATH);
  }

  try {
    const detail = await fetchGetOrderDetail(id);
    if (!detail) {
      return redirect(LIST_PATH);
    }
    return detail;
  } catch {
    return redirect(LIST_PATH);
  }
}

const OrderDetailPage = () => {
  const { t } = useTranslation();
  const detail = useLoaderData() as Api.Order.Entity;
  const nav = useNavigate();
  const closeTabAndNavigate = useCloseTabAndNavigate();

  const canEdit =
    detail.orderStatus === SaleOrderStatus.PENDING_AUDIT || detail.orderStatus === SaleOrderStatus.REJECTED;

  return (
    <div className="h-full min-h-500px flex-col-stretch gap-16px overflow-auto">
      <ACard
        className="card-wrapper"
        title={detail.orderNo}
        variant="borderless"
        extra={
          <ASpace>
            <AButton onClick={() => closeTabAndNavigate(LIST_PATH)}>{t('page.order.detail.back')}</AButton>
            {canEdit && (
              <AButton
                type="primary"
                onClick={() => nav(`/orders/edit/${detail.id}`)}
              >
                {t('common.edit')}
              </AButton>
            )}
          </ASpace>
        }
      >
        <ASpace size="middle">
          <span className="opacity-60">
            {t('page.order.list.customerName')}：{detail.customerName}
          </span>
          {renderSaleOrderStatus(detail.orderStatus)}
        </ASpace>
      </ACard>

      <div className="flex-col-stretch gap-16px">
        <OrderDetailView detail={detail} />
      </div>
    </div>
  );
};

export default OrderDetailPage;
