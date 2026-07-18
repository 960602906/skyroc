import { useCloseTabAndNavigate } from '@/features/tab';
import { fetchAddGoods } from '@/service/api';

import GoodsOperateForm from './modules/GoodsOperateForm';
import { createDefaultGoodsFormValues } from './modules/create-default-goods-form-values';

const LIST_PATH = '/master/goods/list';

const GoodsCreate = () => {
  const { t } = useTranslation();
  const closeTabAndNavigate = useCloseTabAndNavigate();
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
      window.$message?.success(t('common.addSuccess'));
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
        title={t('page.goods.operate.addTitle')}
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

export default GoodsCreate;
