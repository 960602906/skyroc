import { type LoaderFunctionArgs, redirect, useLoaderData } from 'react-router-dom';

import { fetchGetGoodsDetail, fetchUpdateGoods } from '@/service/api';

import GoodsOperateForm from './modules/GoodsOperateForm';

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

const GoodsEdit = () => {
  const { t } = useTranslation();
  const detail = useLoaderData() as Api.Goods.Entity;
  const nav = useNavigate();
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
      nav('/master/goods/list');
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
            <AButton onClick={() => nav('/master/goods/list')}>{t('common.cancel')}</AButton>
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
