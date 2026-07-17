import { request } from '../request';
import { PRINT_TEMPLATE_URLS } from '../urls';

/** 分页查询打印模板。 */
export function fetchGetPrintTemplateList(params?: Api.PrintTemplate.SearchParams) {
  return request<Api.PrintTemplate.List>({
    method: 'get',
    params,
    url: PRINT_TEMPLATE_URLS.BASE
  });
}

/** 新增打印模板和完整字段定义。 */
export function fetchAddPrintTemplate(data?: Api.PrintTemplate.Payload) {
  return request<Api.PrintTemplate.Entity>({
    data,
    method: 'post',
    url: PRINT_TEMPLATE_URLS.BASE
  });
}

/** 完整更新打印模板，传入字段集合将替换原字段定义。 */
export function fetchUpdatePrintTemplate(data?: Api.PrintTemplate.Payload) {
  return request<Api.PrintTemplate.Entity>({
    data,
    method: 'put',
    url: PRINT_TEMPLATE_URLS.BASE
  });
}

/** 按稳定模板编码查询模板和字段定义。 */
export function fetchGetPrintTemplateByCodeDetail(templateCode: string) {
  return request<Api.PrintTemplate.Entity>({
    method: 'get',
    url: `${PRINT_TEMPLATE_URLS.BY_CODE}/${templateCode}`
  });
}

/** 删除打印模板及其字段定义。 */
export function fetchDeletePrintTemplate(id: string) {
  return request<Api.PrintTemplate.Entity>({
    method: 'delete',
    url: `${PRINT_TEMPLATE_URLS.BASE}/${id}`
  });
}
