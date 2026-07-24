import type { Dayjs } from 'dayjs';
import { Suspense, lazy } from 'react';
import { type LoaderFunctionArgs, redirect, useLoaderData, useRevalidator } from 'react-router-dom';

import { DetailPageLayout } from '@/features/crud';
import { fetchGetCustomerProtocolDetail, fetchUpdateCustomerProtocol } from '@/service/api';
import { toBackendDate } from '@/utils/datetime';

import ProtocolDetailView from './modules/ProtocolDetailView';

const ProtocolOperateDrawer = lazy(() => import('../modules/ProtocolOperateDrawer'));

const LIST_PATH = '/master/pricing/protocols';

/** 业务日期提交：统一 YYYY-MM-DD */
function formatDateValue(value: unknown) {
  return toBackendDate(value as Dayjs | string | number | Date | null | undefined);
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
    <>
      <DetailPageLayout
        backLabel={t('page.customer.protocol.detail.back')}
        listPath={LIST_PATH}
        title={detail.name}
        banner={
          <span>
            {t('page.customer.protocol.code')}：{detail.code}
          </span>
        }
        extra={
          <AButton
            type="primary"
            onClick={openEdit}
          >
            {t('common.edit')}
          </AButton>
        }
      >
        <ProtocolDetailView
          detail={detail}
          onGoodsChanged={() => revalidator.revalidate()}
        />
      </DetailPageLayout>

      <Suspense>
        <ProtocolOperateDrawer
          form={form}
          handleSubmit={handleSubmit}
          open={drawerOpen}
          operateType="edit"
          onClose={() => !submitting && setDrawerOpen(false)}
        />
      </Suspense>
    </>
  );
};

export default ProtocolDetail;
