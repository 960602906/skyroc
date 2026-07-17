import { request } from '../request';
import { PRINT_DATA_URLS } from '../urls';

/** 读取指定业务单据的打印数据；预览读取不会修改来源单据状态。 */
export function fetchGetPrintDataDetail(businessType: string) {
  return request<Api.PrintData.Entity>({
    method: 'get',
    url: `${PRINT_DATA_URLS.BASE}/${businessType}`
  });
}

/** 确认订单、入库单或出库单已完成正式打印；不适用于采购单和结算单。 */
export function fetchConfirmPrintData(businessType: string, data?: Api.PrintData.Payload) {
  return request<Api.PrintData.Entity>({
    data,
    method: 'post',
    url: `${PRINT_DATA_URLS.BASE}/${businessType}/confirm`
  });
}
