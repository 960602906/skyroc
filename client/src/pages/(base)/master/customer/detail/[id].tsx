import { type LoaderFunctionArgs, redirect, useLoaderData } from 'react-router-dom';

import { DetailPageLayout } from '@/features/crud';
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

  return (
    <DetailPageLayout
      backLabel={t('page.customer.detail.back')}
      listPath={LIST_PATH}
      title={detail.name}
      banner={
        <span>
          {t('page.customer.list.code')}：{detail.code}
        </span>
      }
      extra={
        <AButton
          type="primary"
          onClick={() => nav(`/master/customer/operate/${detail.id}`)}
        >
          {t('common.edit')}
        </AButton>
      }
    >
      <CustomerDetailView detail={detail} />
    </DetailPageLayout>
  );
};

export default CustomerDetail;
