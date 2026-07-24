import dayjs from 'dayjs';

/** 后端 FixedDateTime 支持 yyyy-MM-dd */
export function formatDateValue(value: unknown) {
  if (!value) return null;
  if (dayjs.isDayjs(value)) {
    return value.format('YYYY-MM-DD');
  }
  const text = String(value).trim();
  if (!text) return null;
  const parsed = dayjs(text);
  return parsed.isValid() ? parsed.format('YYYY-MM-DD') : text;
}

/** 明细行金额预估：quantity * goodsUnitRate / fixedUnitRate * fixedPrice */
export function estimateDetailTotal(detail: Api.Order.DetailFormItem) {
  const quantity = Number(detail.quantity);
  const fixedPrice = Number(detail.fixedPrice);
  if (!Number.isFinite(quantity) || !Number.isFinite(fixedPrice)) {
    return null;
  }

  const unitConversion = Number(detail.unitConversion);
  const fixedUnitConversion = Number(detail.fixedUnitConversion);

  if (
    Number.isFinite(unitConversion) &&
    unitConversion > 0 &&
    Number.isFinite(fixedUnitConversion) &&
    fixedUnitConversion > 0
  ) {
    return (quantity * unitConversion * fixedPrice) / fixedUnitConversion;
  }

  // 单位换算未知时，同单位按 quantity * price 估算
  if (detail.goodsUnitId && detail.fixedGoodsUnitId && detail.goodsUnitId === detail.fixedGoodsUnitId) {
    return quantity * fixedPrice;
  }

  return null;
}

export function formatMoney(value: number | null | undefined) {
  if (value === null || value === undefined || Number.isNaN(Number(value))) {
    return '-';
  }
  return Number(value).toFixed(2);
}

/** 详情实体回填表单 */
export function toOrderFormValues(detail: Api.Order.Entity): Api.Order.FormValues {
  return {
    contactName: detail.contactName,
    contactPhone: detail.contactPhone,
    customerId: detail.customerId,
    deliveryAddress: detail.deliveryAddress,
    details: (detail.details ?? []).map(item => {
      const fixedGoodsUnitId = item.fixedGoodsUnitId || item.goodsUnitId;
      // 详情只回传下单单位换算率；单价单位相同时可复用做金额预估
      const sameUnit = fixedGoodsUnitId === item.goodsUnitId;
      return {
        fixedGoodsUnitId,
        fixedGoodsUnitName: item.fixedGoodsUnitName,
        fixedPrice: item.fixedPrice,
        fixedUnitConversion: sameUnit ? item.unitConversion : null,
        goodsCode: item.goodsCode,
        goodsId: item.goodsId,
        goodsName: item.goodsName,
        goodsUnitId: item.goodsUnitId,
        goodsUnitName: item.goodsUnitName,
        id: item.id,
        innerRemark: item.innerRemark,
        quantity: item.quantity,
        remark: item.remark,
        unitConversion: item.unitConversion
      };
    }),
    id: detail.id,
    innerRemark: detail.innerRemark,
    // 与表单 DatePicker 的字符串值保持一致，避免 dayjs / string 混用
    orderDate: formatDateValue(detail.orderDate),
    quotationId: detail.quotationId,
    receiveDate: formatDateValue(detail.receiveDate),
    remark: detail.remark,
    wareId: detail.wareId
  };
}

function normalizeDetail(item: Api.Order.DetailFormItem): Api.Order.DetailUpdateParams {
  return {
    fixedGoodsUnitId: item.fixedGoodsUnitId,
    fixedPrice: Number(item.fixedPrice),
    goodsId: item.goodsId,
    goodsUnitId: item.goodsUnitId,
    id: item.id || null,
    innerRemark: item.innerRemark?.trim() || null,
    quantity: Number(item.quantity),
    remark: item.remark?.trim() || null
  };
}

/** 表单值转创建/更新请求体 */
export function normalizeOrderPayload(
  values: Api.Order.FormValues,
  options?: { id?: string }
): Api.Order.CreateParams | Api.Order.UpdateParams {
  const orderDate = formatDateValue(values.orderDate);
  if (!orderDate) {
    throw new Error('orderDate is required');
  }

  const details = (values.details ?? []).map(normalizeDetail);
  const base: Api.Order.CreateParams = {
    contactName: values.contactName?.trim() || null,
    contactPhone: values.contactPhone?.trim() || null,
    customerId: values.customerId!,
    deliveryAddress: values.deliveryAddress?.trim() || null,
    details,
    innerRemark: values.innerRemark?.trim() || null,
    orderDate,
    quotationId: values.quotationId || null,
    receiveDate: formatDateValue(values.receiveDate),
    remark: values.remark?.trim() || null,
    wareId: values.wareId || null
  };

  if (options?.id) {
    return {
      ...base,
      id: options.id
    };
  }

  return base;
}
