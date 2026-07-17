import { fetchAddGoods } from '@/service/api';

import GoodsOperateForm, { createDefaultGoodsFormValues } from './modules/GoodsOperateForm';

const GoodsCreate = () => {
  const { t } = useTranslation();
  const nav = useNavigate();
  const [form] = AForm.useForm<Api.Goods.CreateParams>();
  const [submitting, setSubmitting] = useState(false);

  useMount(() => {
    form.setFieldsValue(createDefaultGoodsFormValues());
  });

  async function handleSubmit() {
    const values = await form.validateFields();
    setSubmitting(true);
    try {
      await fetchAddGoods(values);
      window.$message?.success(t('common.updateSuccess'));
      nav('/master/goods/list');
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <ACard
      className="card-wrapper"
      title={t('page.goods.operate.addTitle')}
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
      <GoodsOperateForm form={form} />
    </ACard>
  );
};

export default GoodsCreate;
