import dayjs from 'dayjs';
import { Suspense, lazy } from 'react';
import { type LoaderFunctionArgs, redirect, useLoaderData, useRevalidator } from 'react-router-dom';

import { useCloseTabAndNavigate } from '@/features/tab';
import { fetchGetCustomerProtocolDetail, fetchUpdateCustomerProtocol } from '@/service/api';

import ProtocolDetailView from './modules/ProtocolDetailView';

const ProtocolOperateDrawer = lazy(() => import('../modules/ProtocolOperateDrawer'));

const LIST_PATH = '/master/pricing/protocols';

/** 后端 FixedDateTime 支持 yyyy-MM-dd / yyyy-MM-dd HH:mm:ss；表单统一提交日期部分 */
function formatDateValue(value: unknown) {
  if (!value) return null;
  if (dayjs.isDayjs(value)) {
    return value.format('YYYY-MM-DD');
  }
  const text = String(value).trim();
  if (!text) return null;
  const parsed = dayjs(text);
  return parsed.isValid() ? parsed.format('YYYY-MM-DD') : text;
}

export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) {
    return redirect(LIST_PATH);
  }

  try {
    const detail = await fetchGetCustomerProtocolDetail(id);
    if (!detail) {
      return redirect(LIST_PATH);
    }
    return detail;
  } catch {
    return redirect(LIST_PATH);
  }
}

const ProtocolDetail = () => {
  const { t } = useTranslation();
  const detail = useLoaderData() as Api.CustomerProtocol.Entity;
  const closeTabAndNavigate = useCloseTabAndNavigate();
  const revalidator = useRevalidator();
  const [form] = AForm.useForm<Api.CustomerProtocol.UpdateParams>();
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  function openEdit() {
    form.setFieldsValue({
      ...detail,
      customerIds: detail.customerIds ?? undefined,
      // DatePicker 通过 getValueProps 转 dayjs，表单值保持字符串
      effectiveEnd: detail.effectiveEnd || null,
      effectiveStart: detail.effectiveStart || null
    });
    setDrawerOpen(true);
  }

  async function handleSubmit() {
    const values = await form.validateFields();
    setSubmitting(true);
    try {
      await fetchUpdateCustomerProtocol({
        ...values,
        effectiveEnd: formatDateValue(values.effectiveEnd),
        effectiveStart: formatDateValue(values.effectiveStart) as string,
        id: detail.id
      });
      window.$message?.success(t('common.modifySuccess'));
      setDrawerOpen(false);
      revalidator.revalidate();
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="h-full min-h-500px flex-col-stretch gap-16px overflow-auto">
      <ACard
        className="card-wrapper"
        title={detail.name}
        variant="borderless"
        extra={
          <ASpace>
            <AButton onClick={() => closeTabAndNavigate(LIST_PATH)}>{t('page.customer.protocol.detail.back')}</AButton>
            <AButton
              type="primary"
              onClick={openEdit}
            >
              {t('common.edit')}
            </AButton>
          </ASpace>
        }
      >
        <span className="opacity-60">
          {t('page.customer.protocol.code')}：{detail.code}
        </span>
      </ACard>

      <div className="flex-col-stretch gap-16px">
        <ProtocolDetailView
          detail={detail}
          onGoodsChanged={() => revalidator.revalidate()}
        />
      </div>

      <Suspense>
        <ProtocolOperateDrawer
          form={form}
          handleSubmit={handleSubmit}
          open={drawerOpen}
          operateType="edit"
          onClose={() => !submitting && setDrawerOpen(false)}
        />
      </Suspense>
    </div>
  );
};

export default ProtocolDetail;
