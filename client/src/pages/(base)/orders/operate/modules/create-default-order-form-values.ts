import dayjs from 'dayjs';

/** 新增销售订单默认表单值（日期用字符串，与 DatePicker getValueFromEvent 一致） */
export function createDefaultOrderFormValues(): Api.Order.FormValues {
  return {
    contactName: null,
    contactPhone: null,
    customerId: null,
    deliveryAddress: null,
    details: [],
    innerRemark: null,
    orderDate: dayjs().format('YYYY-MM-DD'),
    quotationId: null,
    receiveDate: null,
    remark: null,
    wareId: null
  };
}
