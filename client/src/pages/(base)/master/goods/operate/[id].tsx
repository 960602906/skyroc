import { fetchGetGoodsDetail, fetchUpdateGoods } from '@/service/api';

import GoodsOperateForm from './modules/GoodsOperateForm';

const GoodsEdit = () => {
  const { t } = useTranslation();
  const { id } = useParams<{ id: string }>();
  const nav = useNavigate();
  const [form] = AForm.useForm<Api.Goods.UpdateParams>();
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (!id) {
      return;
    }

    setLoading(true);
    fetchGetGoodsDetail(id)
      .then(detail => {
        if (detail) {
          form.setFieldsValue({
            ...detail,
            supplierIds: detail.supplierIds ?? undefined
          });
        }
      })
      .finally(() => setLoading(false));
  }, [form, id]);

  async function handleSubmit() {
    const values = await form.validateFields();
    setSubmitting(true);
    try {
      await fetchUpdateGoods({ ...values, id: id! });
      window.$message?.success(t('common.updateSuccess'));
      nav('/master/goods/list');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <ACard
      className="card-wrapper"
      title={t('page.goods.operate.editTitle')}
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
    >
      <ASpin spinning={loading}>
        <GoodsOperateForm form={form} />
      </ASpin>
    </ACard>
  );
};

export default GoodsEdit;
