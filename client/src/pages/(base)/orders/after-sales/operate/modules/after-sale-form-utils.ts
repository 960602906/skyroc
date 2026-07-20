import type { AfterSaleGoodsFormItem } from './AfterSaleOperateForm';

/** 移除仅供表格展示的字段，生成后端售后接口所需商品行。 */
export function toAfterSaleGoodsPayload(items: AfterSaleGoodsFormItem[]): Api.AfterSale.GoodsPayload[] {
  return items
    .filter(item => item.enabled)
    .map(item => ({
      actualRefundQuantity: item.actualRefundQuantity,
      afterSaleType: item.afterSaleType,
      goodsId: item.goodsId,
      goodsUnitId: item.goodsUnitId,
      handleType: item.handleType,
      reasonType: item.reasonType,
      remark: item.remark,
      saleOrderDetailId: item.saleOrderDetailId,
      unitPrice: item.unitPrice
    }));
}
