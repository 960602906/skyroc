import { Suspense, lazy } from 'react';
import { type LoaderFunctionArgs, redirect, useLoaderData, useRevalidator } from 'react-router-dom';

import { DetailPageLayout } from '@/features/crud';
import { fetchGetGoodsUnitDetail, fetchUpdateGoodsUnit } from '@/service/api';

import GoodsUnitDetailView from './modules/GoodsUnitDetailView';

const GoodsUnitOperateDrawer = lazy(() => import('../modules/GoodsUnitOperateDrawer'));

const LIST_PATH = '/master/goods/units';

export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) {
    return redirect(LIST_PATH);
  }

  try {
    const detail = await fetchGetGoodsUnitDetail(id);
    if (!detail) {
      return redirect(LIST_PATH);
    }
    return detail;
  } catch {
    return redirect(LIST_PATH);
  }
}

const GoodsUnitDetail = () => {
  const { t } = useTranslation();
  const detail = useLoaderData() as Api.GoodsUnit.Entity;
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

  const banner = (
    <>
      {t('page.goods.unit.goodsId')}：{detail.goodsName || detail.goodsCode || detail.goodsId}
      {detail.code ? ` · ${t('page.goods.unit.code')}：${detail.code}` : null}
    </>
  );

  return (
    <>
      <DetailPageLayout
        backLabel={t('page.goods.unit.detail.back')}
        banner={banner}
        listPath={LIST_PATH}
        title={title}
        extra={
          <AButton
            type="primary"
            onClick={openEdit}
          >
            {t('common.edit')}
          </AButton>
        }
      >
        <GoodsUnitDetailView detail={detail} />
      </DetailPageLayout>

      <Suspense>
        <GoodsUnitOperateDrawer
          form={form}
          handleSubmit={handleSubmit}
          open={drawerOpen}
          operateType="edit"
          onClose={() => !submitting && setDrawerOpen(false)}
        />
      </Suspense>
    </>
  );
};

export default GoodsUnitDetail;
