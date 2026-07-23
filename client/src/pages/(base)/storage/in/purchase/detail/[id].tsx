import { type LoaderFunctionArgs, redirect, useLoaderData } from 'react-router-dom';

import { DetailPageLayout } from '@/features/crud';
import { renderStockDocumentStatus } from '@/features/crud/render-status';
import { fetchGetStockInPurchaseDetail } from '@/service/api';

import PurchaseStockInDetailView from './modules/PurchaseStockInDetailView';

const LIST_PATH = '/storage/in/purchase';

/** 路由元信息：采购入库详情页不显示在菜单中 */
export const handle = {
  hideInMenu: true,
  i18nKey: 'route.(base)_storage_in_purchase',
  keepAlive: false
};

/** 路由切换前加载采购入库详情，入库单不存在或加载失败时返回列表。 */
export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) return redirect(LIST_PATH);

  try {
    const detail = await fetchGetStockInPurchaseDetail(id);
    return detail ?? redirect(LIST_PATH);
  } catch {
    return redirect(LIST_PATH);
  }
}

/** 采购入库基础信息、商品明细和审核轨迹详情页。 */
const PurchaseStockInDetail = () => {
  const { t } = useTranslation();
  const detail = useLoaderData() as Api.StockIn.Entity;

  if (!detail) return null;

  return (
    <DetailPageLayout
      backLabel={t('page.storage.in.purchase.back')}
      banner={renderStockDocumentStatus(detail.businessStatus)}
      listPath={LIST_PATH}
      title={detail.inNo}
    >
      <PurchaseStockInDetailView detail={detail} />
    </DetailPageLayout>
  );
};

export default PurchaseStockInDetail;
