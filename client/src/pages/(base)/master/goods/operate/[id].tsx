import { type LoaderFunctionArgs, redirect, useLoaderData } from 'react-router-dom';

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
    <div className="h-full min-h-500px flex-col-stretch gap-16px overflow-auto">
      <ACard
        className="card-wrapper"
        styles={{ body: { display: 'none' } }}
        title={t('page.goods.operate.editTitle')}
        variant="borderless"
        extra={
          <ASpace>
            <AButton onClick={() => closeTabAndNavigate(LIST_PATH)}>{t('common.cancel')}</AButton>
            <AButton
              loading={submitting}
              type="primary"
              onClick={handleSubmit}
            >
              {t('common.confirm')}
            </AButton>
          </ASpace>
        }
      />
      <GoodsOperateForm form={form} />
    </div>
  );
};

export default GoodsEdit;
