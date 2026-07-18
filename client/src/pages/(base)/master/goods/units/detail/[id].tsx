import { Suspense, lazy } from 'react';
import { type LoaderFunctionArgs, redirect, useLoaderData, useRevalidator } from 'react-router-dom';

import { fetchGetGoodsUnitDetail, fetchUpdateGoodsUnit } from '@/service/api';

import GoodsUnitDetailView from './modules/GoodsUnitDetailView';

const GoodsUnitOperateDrawer = lazy(() => import('../modules/GoodsUnitOperateDrawer'));

export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) {
    return redirect('/master/goods/units');
  }

  try {
    const detail = await fetchGetGoodsUnitDetail(id);
    if (!detail) {
      return redirect('/master/goods/units');
    }
    return detail;
  } catch {
    return redirect('/master/goods/units');
  }
}

const GoodsUnitDetail = () => {
  const { t } = useTranslation();
  const detail = useLoaderData() as Api.GoodsUnit.Entity;
  const nav = useNavigate();
  const revalidator = useRevalidator();
  const [form] = AForm.useForm<Api.GoodsUnit.UpdateParams>();
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  const title = detail.name || detail.goodsName || t('page.goods.unit.detail.title');

  function openEdit() {
    form.setFieldsValue({ ...detail });
    setDrawerOpen(true);
  }

  async function handleSubmit() {
    const values = await form.validateFields();
    setSubmitting(true);
    try {
      await fetchUpdateGoodsUnit({ ...values, id: detail.id });
      window.$message?.success(t('common.modifySuccess'));
      setDrawerOpen(false);
      revalidator.revalidate();
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="h-full min-h-500px flex-col-stretch gap-16px overflow-auto">
      <ACard
        className="card-wrapper"
        title={title}
        variant="borderless"
        extra={
          <ASpace>
            <AButton onClick={() => nav('/master/goods/units')}>{t('page.goods.unit.detail.back')}</AButton>
            <AButton
              type="primary"
              onClick={openEdit}
            >
              {t('common.edit')}
            </AButton>
          </ASpace>
        }
      >
        <span className="opacity-60">
          {t('page.goods.unit.goodsId')}：{detail.goodsName || detail.goodsCode || detail.goodsId}
          {detail.code ? ` · ${t('page.goods.unit.code')}：${detail.code}` : null}
        </span>
      </ACard>

      <div className="flex-col-stretch gap-16px">
        <GoodsUnitDetailView detail={detail} />
      </div>

      <Suspense>
        <GoodsUnitOperateDrawer
          form={form}
          handleSubmit={handleSubmit}
          open={drawerOpen}
          operateType="edit"
          onClose={() => !submitting && setDrawerOpen(false)}
        />
      </Suspense>
    </div>
  );
};

export default GoodsUnitDetail;
