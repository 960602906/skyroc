import { request } from '../request';
import { IMPORT_EXPORT_URLS } from '../urls';

/** 导出当前商品数据为 CSV；响应头 X-Import-Export-Job-Id 可用于查询本次任务状态。 */
export function fetchExportJob(jobType: string) {
  return request<Api.ImportExport.Result>({
    method: 'get',
    url: `${IMPORT_EXPORT_URLS.JOBS_EXPORT}/${jobType}`
  });
}

/** 读取商品 CSV 并在整文件校验通过后写入商品资料；失败时不写入任何商品行。 */
export function fetchImportJob(jobType: string, data?: Api.ImportExport.Payload) {
  return request<Api.ImportExport.Entity>({
    data,
    method: 'post',
    url: `${IMPORT_EXPORT_URLS.JOBS_IMPORT}/${jobType}`
  });
}

/** 下载指定业务类型的 CSV 导入模板；当前支持商品类型。 */
export function fetchGetImportTemplate(jobType: string) {
  return request<Api.ImportExport.Result>({
    method: 'get',
    url: `${IMPORT_EXPORT_URLS.JOBS_TEMPLATES}/${jobType}`
  });
}

/** 查询当前操作人创建的导入或导出任务状态，避免泄露其他用户的文件处理结果。 */
export function fetchGetImportExportJobDetail(id: string) {
  return request<Api.ImportExport.Entity>({
    method: 'get',
    url: `${IMPORT_EXPORT_URLS.JOBS}/${id}`
  });
}
