import type { RouterNavigateOptions, To } from 'react-router-dom';
import { createBrowserRouter, createHashRouter, matchRoutes } from 'react-router-dom';

import { globalConfig } from '@/config';
import { initCacheRoutes, routes } from '@/router';
import { store } from '@/store';

import { getIsLogin } from '../auth/auth-store';

import { initAuthRoutes } from './initRouter';
import { type LocationQueryRaw, stringifyQuery } from './query';
import { setCacheRoutes } from './route-store';

/**
 * ??????????
 *
 * ?? history ? hash ?????? globalConfig.routerMode ??
 */
function createRouterInstance() {
  const routerCreator = globalConfig.routerMode === 'hash' ? createHashRouter : createBrowserRouter;

  return routerCreator;
}

function initRouter() {
  let isAlreadyPatch = false;

  function getIsNeedPatch(path: string) {
    if (!getIsLogin(store.getState())) return false;

    if (isAlreadyPatch) return false;

    const matchRoute = matchRoutes(routes, { pathname: path }, import.meta.env.VITE_BASE_URL);

    if (!matchRoute) return true;

    if (matchRoute) {
      return matchRoute[1].route.path === '*';
    }

    return false;
  }

  const routerCreator = createRouterInstance();

  const reactRouter = routerCreator(routes, {
    basename: import.meta.env.VITE_BASE_URL,
    patchRoutesOnNavigation: async ({ patch, path }) => {
      if (getIsNeedPatch(path)) {
        isAlreadyPatch = true;

        await initAuthRoutes(patch);
      }
    }
  });

  store.dispatch(setCacheRoutes(initCacheRoutes));

  if (getIsLogin(store.getState()) && !isAlreadyPatch) {
    initAuthRoutes(reactRouter.patchRoutes);
  }

  function resetRoutes() {
    isAlreadyPatch = false;
    reactRouter._internalSetRoutes(routes);
  }

  return {
    reactRouter,
    resetRoutes
  };
}

/** ?????????? query ?? */
type ExtendedNavigateOptions = RouterNavigateOptions & {
  query?: LocationQueryRaw;
};

/** ?????????? */
function buildPathWithQuery(path: To, query?: LocationQueryRaw): To {
  if (!query) return path;

  const pathStr = typeof path === 'string' ? path : path.pathname || '';
  const search = stringifyQuery(query);

  return `${pathStr}?${search}` as To;
}

function navigator() {
  const { reactRouter, resetRoutes } = initRouter();

  async function navigate(path: To | null, options?: RouterNavigateOptions) {
    reactRouter.navigate(path, options);
  }

  function back() {
    reactRouter.navigate(-1);
  }

  function forward() {
    reactRouter.navigate(1);
  }

  function go(delta: number) {
    reactRouter.navigate(delta);
  }

  /** ??????????????? ????? RouterNavigateOptions ? query ?? */
  function replace(path: To, options?: ExtendedNavigateOptions) {
    const { query, ...navigateOptions } = options || {};
    const finalPath = buildPathWithQuery(path, query);

    reactRouter.navigate(finalPath, { ...navigateOptions, replace: true });
  }

  function reload() {
    reactRouter.navigate(0);
  }

  function navigateUp() {
    reactRouter.navigate('..');
  }

  function goHome(options?: RouterNavigateOptions) {
    reactRouter.navigate(globalConfig.homePath, options);
  }

  /**
   * ??????????????? ????? RouterNavigateOptions ? query ??
   *
   * @example
   *   // ????
   *   router.push('/users');
   *
   *   // ?????
   *   router.push('/users', { query: { page: 1, size: 10 } });
   *
   *   // ??????
   *   router.push('/users', {
   *     query: { page: 1 },
   *     state: { from: 'home' },
   *     preventScrollReset: true
   *   });
   *
   *   // ??????????
   *   router.push('/users', { replace: true });
   */
  function push(path: To, options?: ExtendedNavigateOptions) {
    const { query, ...navigateOptions } = options || {};
    const finalPath = buildPathWithQuery(path, query);

    reactRouter.navigate(finalPath, navigateOptions);
  }

  /** ????????navigate ??????? */
  function goTo(path: To, options?: ExtendedNavigateOptions) {
    const { query, ...navigateOptions } = options || {};
    const finalPath = buildPathWithQuery(path, query);

    reactRouter.navigate(finalPath, navigateOptions);
  }

  /** ???????? */
  function getLocation() {
    return reactRouter.state.location;
  }

  /** ??????? */
  function getPathname() {
    return reactRouter.state.location.pathname;
  }

  /** ???????? */
  function getSearch() {
    return reactRouter.state.location.search;
  }

  /** ???? hash */
  function getHash() {
    return reactRouter.state.location.hash;
  }

  /** ?????? */
  function getState() {
    return reactRouter.state.location.state;
  }

  /** ??????????????????? */
  function canGoBack() {
    return window.history.length > 1;
  }

  return {
    back,
    canGoBack,
    forward,
    getHash,
    getLocation,
    getPathname,
    getSearch,
    getState,
    go,
    goHome,
    goTo,
    navigate,
    navigateUp,
    push,
    reactRouter,
    reload,
    replace,
    resetRoutes
  };
}

export const router = navigator();

export type RouterContextType = Awaited<ReturnType<typeof navigator>>;
