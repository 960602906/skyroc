import type { RequestInstance } from '@sa/axios';
import { BACKEND_ERROR_CODE } from '@sa/axios';
import type { AxiosError, AxiosInstance, AxiosResponse } from 'axios';

import { router } from '@/features/router';
import { $t } from '@/locales';

import { getAuthorization, handleExpiredRequest, showErrorMsg } from './shared';
import type { RequestInstanceState } from './type';

/** 解析 `.env` 中的业务码列表（逗号分隔，忽略空项） */
function parseServiceCodes(value: string | undefined): string[] {
  return (
    value
      ?.split(',')
      .map(code => code.trim())
      .filter(Boolean) ?? []
  );
}

/** - 后端业务失败：鉴权码弹 Modal 并登出，过期码尝试刷新，其余交给 onError 弹 toast */
export async function backEndFail(
  response: AxiosResponse<App.Service.Response<unknown>, any>,
  instance: AxiosInstance,
  request: RequestInstance<RequestInstanceState>
) {
  const responseCode = String(response.data.code);

  function handleLogout() {
    const location = router.reactRouter.state.location;
    const fullPath = location.pathname + location.search + location.hash;
    router.push('/login-out', { query: { redirect: fullPath } });
  }

  function logoutAndCleanup() {
    handleLogout();
    window.removeEventListener('beforeunload', handleLogout);

    request.state.errMsgStack = request.state.errMsgStack.filter(msg => msg !== response.data.msg);
  }

  // 静默登出（当前 ResponseCode 无专用码，预留）
  const logoutCodes = parseServiceCodes(import.meta.env.VITE_SERVICE_LOGOUT_CODES);
  if (logoutCodes.includes(responseCode)) {
    handleLogout();
    return null;
  }

  // 鉴权/Token 校验失败：401 / 402 / 403 → Modal 后登出（与 ResponseCode 对齐）
  const modalLogoutCodes = parseServiceCodes(import.meta.env.VITE_SERVICE_MODAL_LOGOUT_CODES);
  if (modalLogoutCodes.includes(responseCode) && !request.state.errMsgStack?.includes(response.data.msg)) {
    request.state.errMsgStack = [...(request.state.errMsgStack || []), response.data.msg];

    window.addEventListener('beforeunload', handleLogout);

    window.$modal?.error({
      content: response.data.msg,
      keyboard: false,
      maskClosable: false,
      okText: $t('common.confirm'),
      onClose() {
        logoutAndCleanup();
      },
      onOk() {
        logoutAndCleanup();
      },
      title: $t('common.error')
    });

    return null;
  }

  // Token 过期刷新重试；refreshToken 接口本身不得再返回这些码，否则会死循环
  const expiredTokenCodes = parseServiceCodes(import.meta.env.VITE_SERVICE_EXPIRED_TOKEN_CODES);
  if (expiredTokenCodes.includes(responseCode)) {
    const success = await handleExpiredRequest(request.state);
    if (success) {
      const Authorization = getAuthorization();
      Object.assign(response.config.headers, { Authorization });

      return instance.request(response.config) as Promise<AxiosResponse>;
    }
  }

  return null;
}

/** - 网络/业务错误兜底：鉴权 Modal 与 Token 刷新不重复 toast，其余弹出全局错误提示 */
export function handleError(
  error: AxiosError<App.Service.Response<unknown>, any>,
  request: RequestInstance<RequestInstanceState>
) {
  let message = error.message;
  let backendErrorCode = '';

  if (error.code === BACKEND_ERROR_CODE) {
    message = error.response?.data?.msg || message;
    backendErrorCode = String(error.response?.data?.code || '');
  }

  const modalLogoutCodes = parseServiceCodes(import.meta.env.VITE_SERVICE_MODAL_LOGOUT_CODES);
  if (modalLogoutCodes.includes(backendErrorCode)) {
    return;
  }

  const logoutCodes = parseServiceCodes(import.meta.env.VITE_SERVICE_LOGOUT_CODES);
  if (logoutCodes.includes(backendErrorCode)) {
    return;
  }

  const expiredTokenCodes = parseServiceCodes(import.meta.env.VITE_SERVICE_EXPIRED_TOKEN_CODES);
  if (expiredTokenCodes.includes(backendErrorCode)) {
    return;
  }

  showErrorMsg(request.state, message);
}
