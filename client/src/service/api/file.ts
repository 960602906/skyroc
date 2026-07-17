import { request } from '../request';
import { FILE_URLS } from '../urls';

/** 上传一个不超过 10 MiB 的 PDF、PNG 或 JPEG；文件名、声明 MIME 类型和二进制签名必须一致。 */
export function fetchUploadFile(data?: Api.File.Payload) {
  return request<Api.File.Entity>({
    data,
    method: 'post',
    url: FILE_URLS.BASE
  });
}

/** 下载当前创建人上传的文件；不存在、物理文件缺失或其他用户文件均返回未找到，避免泄露存在性。 */
export function fetchDownloadFile(id: string) {
  return request<Api.File.Result>({
    method: 'get',
    url: `${FILE_URLS.BASE}/${id}/download`
  });
}
