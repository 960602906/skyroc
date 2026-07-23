import dayjs from 'dayjs';

/** 明细行表单值 */
export interface PurchaseOrderDetailFormItem {
  goodsCode?: string | null;
  goodsId: string;
  goodsName?: string | null;
  id?: string | null;
  productDate?: string | null;
  purchasePrice: number;
  purchaseQuantity: number;
  purchaseUnitId: string;
  purchaseUnitName?: string | null;
  remark?: string | null;
  requiredQuantity?: number | null;
}

/** 采购单页面表单值 */
export interface PurchaseOrderFormValues {
  details: PurchaseOrderDetailFormItem[];
  id?: string;
  purchasePattern: number;
  purchaserId?: string | null;
  receiveTime?: string | null;
  remark?: string | null;
  supplierContactName?: string | null;
  supplierContactPhone?: string | null;
  supplierId?: string | null;
}

/** 详情实体回填表单 */
export function toPurchaseOrderFormValues(detail: Api.PurchaseOrder.Entity): PurchaseOrderFormValues {
  return {
    details: (detail.details ?? []).map(item => ({
      goodsCode: item.goodsCode,
      goodsId: item.goodsId,
      goodsName: item.goodsName,
      id: item.id,
      productDate: item.productDate ?? null,
      purchasePrice: item.purchasePrice,
      purchaseQuantity: item.purchaseQuantity,
      purchaseUnitId: item.purchaseUnitId,
      purchaseUnitName: item.purchaseUnitName,
      remark: item.remark ?? null,
      requiredQuantity: item.requiredQuantity ?? null
    })),
    id: detail.id,
    purchasePattern: detail.purchasePattern,
    purchaserId: detail.purchaserId ?? null,
    receiveTime: detail.receiveTime ?? null,
    remark: detail.remark ?? null,
    supplierContactName: detail.supplierContactName ?? null,
    supplierContactPhone: detail.supplierContactPhone ?? null,
    supplierId: detail.supplierId ?? null
  };
}

function toCreateDetailPayload(item: PurchaseOrderDetailFormItem): Api.PurchaseOrder.CreateDetailPayload {
  return {
    goodsId: item.goodsId,
    productDate: item.productDate ? dayjs(item.productDate).format('YYYY-MM-DD') : null,
    purchasePrice: Number(item.purchasePrice),
    purchaseQuantity: Number(item.purchaseQuantity),
    purchaseUnitId: item.purchaseUnitId,
    remark: item.remark?.trim() || null,
    requiredQuantity: item.requiredQuantity ?? null
  };
}

function toUpdateDetailPayload(item: PurchaseOrderDetailFormItem): Api.PurchaseOrder.UpdateDetailPayload {
  return {
    goodsId: item.goodsId,
    id: item.id || null,
    planAllocations: [],
    productDate: item.productDate ? dayjs(item.productDate).format('YYYY-MM-DD') : null,
    purchasePrice: Number(item.purchasePrice),
    purchaseQuantity: Number(item.purchaseQuantity),
    purchaseUnitId: item.purchaseUnitId,
    remark: item.remark?.trim() || null,
    requiredQuantity: item.requiredQuantity ?? null
  };
}

/** 表单值转新增请求体 */
export function normalizePurchaseOrderCreatePayload(values: PurchaseOrderFormValues): Api.PurchaseOrder.CreatePayload {
  return {
    details: values.details.map(toCreateDetailPayload),
    purchasePattern: values.purchasePattern,
    purchaserId: values.purchaserId || null,
    receiveTime: values.receiveTime ? dayjs(values.receiveTime).toISOString() : null,
    remark: values.remark?.trim() || null,
    supplierContactName: values.supplierContactName?.trim() || null,
    supplierContactPhone: values.supplierContactPhone?.trim() || null,
    supplierId: values.supplierId || null
  };
}

/** 表单值转编辑请求体 */
export function normalizePurchaseOrderUpdatePayload(values: PurchaseOrderFormValues): Api.PurchaseOrder.UpdatePayload {
  return {
    details: values.details.map(toUpdateDetailPayload),
    id: values.id!,
    purchasePattern: values.purchasePattern,
    purchaserId: values.purchaserId || null,
    receiveTime: values.receiveTime ? dayjs(values.receiveTime).toISOString() : null,
    remark: values.remark?.trim() || null,
    supplierContactName: values.supplierContactName?.trim() || null,
    supplierContactPhone: values.supplierContactPhone?.trim() || null,
    supplierId: values.supplierId || null
  };
}

export function formatMoney(value: number | null | undefined) {
  if (value === null || value === undefined || Number.isNaN(Number(value))) return '-';
  return Number(value).toFixed(4);
}
