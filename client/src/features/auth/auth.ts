import { useLoading } from '@sa/hooks';

import { globalConfig } from '@/config';
import { router, useRouter } from '@/features/router';
import { useLogin, useUserInfo } from '@/service/hooks';
import { QUERY_KEYS } from '@/service/keys';
import { queryClient } from '@/service/queryClient';
import { store } from '@/store';
import { localStg } from '@/utils/storage';

import { resetRouteStore } from '../router/route-store';
import { clearTabs, selectTabs } from '../tab/tab-store';
import { getThemeSettings } from '../theme/theme-settings-store';

import { getIsLogin, resetAuth as resetAuthAction, setToken } from './auth-store';
import { clearAuthStorage, getUserInfo } from './shared';

const { VITE_AUTH_ROUTE_MODE, VITE_STATIC_SUPER_ROLE } = import.meta.env;

export function useAuth() {
  const { data } = useUserInfo();

  const isLogin = useAppSelector(getIsLogin);

  const isStaticSuper = VITE_AUTH_ROUTE_MODE === 'static' && data?.roles.includes(VITE_STATIC_SUPER_ROLE);

  function hasAuth(codes: string | string[]) {
    if (!isLogin || !data) {
      return false;
    }

    if (typeof codes === 'string') {
      return data.buttons.includes(codes);
    }

    return codes.some(code => data.buttons.includes(code));
  }

  return {
    hasAuth,
    isStaticSuper
  };
}

export function useInitAuth() {
  const { endLoading, loading, startLoading } = useLoading();

  const [searchParams] = useSearchParams();

  const { mutateAsync: login } = useLogin();

  const { refetch: refetchUserInfo } = useUserInfo();

  const { t } = useTranslation();

  const dispatch = useAppDispatch();

  const { replace } = useRouter();

  const redirectUrl = searchParams.get('redirect');

  async function toLogin(params: Api.Auth.LoginParams, redirect = true) {
    if (loading) return;

    startLoading();

    login(params, {
      onSuccess: async data => {
        localStg.set('token', data.token);

        localStg.set('refreshToken', data.refreshToken);

        const { data: info, error } = await refetchUserInfo();

        if (!error && info) {
          const previousUserId = localStg.get('previousUserId');

          localStg.set('userInfo', info);

          dispatch(setToken(data.token));

          if (previousUserId !== info.userId || !previousUserId) {
            localStg.remove('globalTabs');

            replace(globalConfig.homePath);
          } else if (redirect) {
            if (redirectUrl) {
              replace(redirectUrl);
            } else {
              replace(globalConfig.homePath);
            }
          }

          window.$notification?.success({
            description: t('page.login.common.welcomeBack', { userName: info.userName }),
            message: t('page.login.common.loginSuccess')
          });
        } else {
          endLoading();
        }
      }
    }).finally(endLoading);
  }

  return {
    loading,
    toLogin
  };
}

/**
 * Reset auth - ??????????????????
 *
 * ???????????????????????
 */
export function resetAuth() {
  // ??????
  clearAuthStorage();

  // ??????
  store.dispatch(resetAuthAction());

  // ?????
  store.dispatch(clearTabs());

  // ??????
  store.dispatch(resetRouteStore());

  // ??????????? localStorage?
  const userInfo = queryClient.getQueryData<Api.Auth.UserInfo>(QUERY_KEYS.AUTH.USER_INFO) || getUserInfo();

  // ??????? ID
  localStg.set('previousUserId', userInfo?.userId || '');

  // ????
  router.resetRoutes();

  // ???????????
  const themeSettings = getThemeSettings(store.getState());
  const tabs = selectTabs(store.getState());
  if (themeSettings.tab.cache) {
    localStg.set('globalTabs', tabs);
  }

  // ??????
  queryClient.clear();

  const location = router.reactRouter.state.location;

  const fullPath = location.pathname + location.search + location.hash;
  // ??????
  const currentPath = location.pathname + location.search;
  const isLoginPage = currentPath.includes('/login');

  // ????????????????? redirect ??
  if (!isLoginPage) {
    router.push('/login', { query: { redirect: fullPath }, replace: true });
  } else {
    router.replace('/login');
  }
}
