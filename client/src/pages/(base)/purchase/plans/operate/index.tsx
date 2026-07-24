import dayjs from 'dayjs';

import { OperatePageLayout } from '@/features/crud';
import { useCloseTabAndNavigate } from '@/features/tab';
import { fetchAddPurchasePlan } from '@/service/api';

import PurchasePlanOperateForm from './modules/PurchasePlanOperateForm';

const LIST_PATH = '/purchase/plans';

const defaultValues = {
  details: [],
  planDate: undefined,
  purchasePattern: 1,
  purchaserId: undefined,
  remark: undefined,
  supplierId: undefined
};

const PurchasePlanCreatePage = () => {
  const { t } = useTranslation();
  const closeTabAndNavigate = useCloseTabAndNavigate();
  const [form] = AForm.useForm();
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit() {
    try {
      const values = await form.validateFields();
      setSubmitting(true);
      try {
        const payload = {
          ...values,
          planDate: values.planDate ? dayjs(values.planDate).format('YYYY-MM-DD HH:mm:ss') : undefined
        } as Api.PurchasePlan.CreateParams;
        await fetchAddPurchasePlan(payload);
        window.$message?.success(t('common.addSuccess'));
        closeTabAndNavigate(LIST_PATH);
      } finally {
        setSubmitting(false);
      }
    } catch {
      // 表单校验失败时不提交
    }
  }

  return (
    <OperatePageLayout
      listPath={LIST_PATH}
      loading={submitting}
      title={t('page.purchase.plan.add')}
      onSave={handleSubmit}
    >
      <PurchasePlanOperateForm
        form={form}
        initialValues={defaultValues}
      />
    </OperatePageLayout>
  );
};

export default PurchasePlanCreatePage;
