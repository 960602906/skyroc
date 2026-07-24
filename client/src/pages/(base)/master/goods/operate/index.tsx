import { OperatePageLayout } from '@/features/crud';
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
    <OperatePageLayout
      listPath={LIST_PATH}
      loading={submitting}
      title={t('page.goods.operate.addTitle')}
      onSave={handleSubmit}
    >
      <GoodsOperateForm form={form} />
    </OperatePageLayout>
  );
};

export default GoodsCreate;
