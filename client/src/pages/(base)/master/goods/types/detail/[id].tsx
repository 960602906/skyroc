import { Suspense, lazy } from 'react';
import { type LoaderFunctionArgs, redirect, useLoaderData, useRevalidator } from 'react-router-dom';

import { DetailPageLayout } from '@/features/crud';
import { fetchGetGoodsTypeDetail, fetchUpdateGoodsType } from '@/service/api';

import GoodsTypeDetailView from './modules/GoodsTypeDetailView';

const GoodsTypeOperateDrawer = lazy(() => import('../modules/GoodsTypeOperateDrawer'));

const LIST_PATH = '/master/goods/types';

export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) {
    return redirect(LIST_PATH);
  }

  try {
    const detail = await fetchGetGoodsTypeDetail(id);
    if (!detail) {
      return redirect(LIST_PATH);
    }
    return detail;
  } catch {
    return redirect(LIST_PATH);
  }
}

const GoodsTypeDetail = () => {
  const { t } = useTranslation();
  const detail = useLoaderData() as Api.GoodsType.Entity;
  const revalidator = useRevalidator();
  const [form] = AForm.useForm<Api.GoodsType.UpdateParams>();
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  function openEdit() {
    form.setFieldsValue({ ...detail });
    setDrawerOpen(true);
  }

  async function handleSubmit() {
    const values = await form.validateFields();
    setSubmitting(true);
    try {
      await fetchUpdateGoodsType({ ...values, id: detail.id });
      window.$message?.success(t('common.modifySuccess'));
      setDrawerOpen(false);
      revalidator.revalidate();
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <>
      <DetailPageLayout
        backLabel={t('page.goods.type.detail.back')}
        listPath={LIST_PATH}
        title={detail.name}
        banner={
          <span>
            {t('page.goods.type.code')}：{detail.code}
          </span>
        }
        extra={
          <AButton
            type="primary"
            onClick={openEdit}
          >
            {t('common.edit')}
          </AButton>
        }
      >
        <GoodsTypeDetailView detail={detail} />
      </DetailPageLayout>

      <Suspense>
        <GoodsTypeOperateDrawer
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

export default GoodsTypeDetail;
