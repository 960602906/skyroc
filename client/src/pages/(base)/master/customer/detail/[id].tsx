import { type LoaderFunctionArgs, redirect, useLoaderData } from 'react-router-dom';

import { useCloseTabAndNavigate } from '@/features/tab';
import { fetchGetCustomerDetail } from '@/service/api';

import CustomerDetailView from './modules/CustomerDetailView';

const LIST_PATH = '/master/customer/list';

export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) {
    return redirect(LIST_PATH);
  }

  try {
    const detail = await fetchGetCustomerDetail(id);
    if (!detail) {
      return redirect(LIST_PATH);
    }
    return detail;
  } catch {
    return redirect(LIST_PATH);
  }
}

const CustomerDetail = () => {
  const { t } = useTranslation();
  const detail = useLoaderData() as Api.Customer.Entity;
  const nav = useNavigate();
  const closeTabAndNavigate = useCloseTabAndNavigate();

  return (
    <div className="h-full min-h-500px flex-col-stretch gap-16px overflow-auto">
      <ACard
        className="card-wrapper"
        title={detail.name}
        variant="borderless"
        extra={
          <ASpace>
            <AButton onClick={() => closeTabAndNavigate(LIST_PATH)}>{t('page.customer.detail.back')}</AButton>
            <AButton
              type="primary"
              onClick={() => nav(`/master/customer/operate/${detail.id}`)}
            >
              {t('common.edit')}
            </AButton>
          </ASpace>
        }
      >
        <span className="opacity-60">
          {t('page.customer.list.code')}：{detail.code}
        </span>
      </ACard>

      <div className="flex-col-stretch gap-16px">
        <CustomerDetailView detail={detail} />
      </div>
    </div>
  );
};

export default CustomerDetail;
