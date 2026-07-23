import { type LoaderFunctionArgs, redirect, useLoaderData } from 'react-router-dom';

import { DetailPageLayout } from '@/features/crud';
import { fetchGetGoodsDetail } from '@/service/api';

import GoodsDetailView from './modules/GoodsDetailView';

const LIST_PATH = '/master/goods/list';

export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) {
    return redirect(LIST_PATH);
  }

  try {
    const detail = await fetchGetGoodsDetail(id);
    if (!detail) {
      return redirect(LIST_PATH);
    }
    return detail;
  } catch {
    return redirect(LIST_PATH);
  }
}

const GoodsDetail = () => {
  const { t } = useTranslation();
  const detail = useLoaderData() as Api.Goods.Entity;
  const nav = useNavigate();

  return (
    <DetailPageLayout
      backLabel={t('page.goods.detail.back')}
      listPath={LIST_PATH}
      title={detail.name}
      banner={
        <span>
          {t('page.goods.operate.code')}：{detail.code}
        </span>
      }
      extra={
        <AButton
          type="primary"
          onClick={() => nav(`/master/goods/operate/${detail.id}`)}
        >
          {t('common.edit')}
        </AButton>
      }
    >
      <GoodsDetailView detail={detail} />
    </DetailPageLayout>
  );
};

export default GoodsDetail;
