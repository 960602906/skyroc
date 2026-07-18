import { type LoaderFunctionArgs, redirect, useLoaderData } from 'react-router-dom';

import { fetchGetGoodsDetail } from '@/service/api';

import GoodsDetailView from './modules/GoodsDetailView';

export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) {
    return redirect('/master/goods/list');
  }

  try {
    const detail = await fetchGetGoodsDetail(id);
    if (!detail) {
      return redirect('/master/goods/list');
    }
    return detail;
  } catch {
    return redirect('/master/goods/list');
  }
}

const GoodsDetail = () => {
  const { t } = useTranslation();
  const detail = useLoaderData() as Api.Goods.Entity;
  const nav = useNavigate();

  return (
    <div className="h-full min-h-500px flex-col-stretch gap-16px overflow-auto">
      <ACard
        className="card-wrapper"
        title={detail.name}
        variant="borderless"
        extra={
          <ASpace>
            <AButton onClick={() => nav('/master/goods/list')}>{t('page.goods.detail.back')}</AButton>
            <AButton
              type="primary"
              onClick={() => nav(`/master/goods/operate/${detail.id}`)}
            >
              {t('common.edit')}
            </AButton>
          </ASpace>
        }
      >
        <span className="opacity-60">
          {t('page.goods.operate.code')}：{detail.code}
        </span>
      </ACard>

      <div className="flex-col-stretch gap-16px">
        <GoodsDetailView detail={detail} />
      </div>
    </div>
  );
};

export default GoodsDetail;
