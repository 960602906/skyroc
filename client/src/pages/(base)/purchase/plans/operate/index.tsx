import dayjs from 'dayjs';

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
    <div className="h-full min-h-500px flex-col-stretch gap-16px overflow-auto">
      <ACard
        className="card-wrapper"
        styles={{ body: { display: 'none' } }}
        title={t('page.purchase.plan.add')}
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
      <PurchasePlanOperateForm
        form={form}
        initialValues={defaultValues}
      />
    </div>
  );
};

export default PurchasePlanCreatePage;
