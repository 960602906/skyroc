import { type LoaderFunctionArgs, redirect, useLoaderData } from 'react-router-dom';

import { OperatePageLayout } from '@/features/crud';
import { useCloseTabAndNavigate } from '@/features/tab';
import { fetchGetGoodsDetail, fetchUpdateGoods } from '@/service/api';

import GoodsOperateForm from './modules/GoodsOperateForm';

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

const GoodsEdit = () => {
  const { t } = useTranslation();
  const detail = useLoaderData() as Api.Goods.Entity;
  const closeTabAndNavigate = useCloseTabAndNavigate();
  const [form] = AForm.useForm<Api.Goods.UpdateParams>();
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    form.setFieldsValue({
      ...detail,
      supplierIds: detail.supplierIds ?? undefined
    });
  }, [detail, form]);

  async function handleSubmit() {
    const values = await form.validateFields();
    setSubmitting(true);
    try {
      await fetchUpdateGoods({ ...values, id: detail.id });
      window.$message?.success(t('common.updateSuccess'));
      closeTabAndNavigate(LIST_PATH);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <OperatePageLayout
      listPath={LIST_PATH}
      loading={submitting}
      title={t('page.goods.operate.editTitle')}
      onSave={handleSubmit}
    >
      <GoodsOperateForm form={form} />
    </OperatePageLayout>
  );
};

export default GoodsEdit;
