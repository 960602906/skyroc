import type { PayloadAction } from '@reduxjs/toolkit';
import { createSlice } from '@reduxjs/toolkit';

interface InitialStateType {
  cacheRoutes: string[];
  removeCacheKey: string[] | string | null;
  routeHomePath: string;
}

const initialState: InitialStateType = {
  /** - 需要进行缓存的页面 */
  cacheRoutes: [],
  /** - 需要删除的缓存页面 */
  removeCacheKey: null,
  /** - 首页路由 */
  routeHomePath: import.meta.env.VITE_ROUTE_HOME
};

export const routeSlice = createSlice({
  initialState,
  name: 'route',
  reducers: {
    addCacheRoutes: (state, { payload }: PayloadAction<string>) => {
      state.cacheRoutes.push(payload);
    },
    resetRouteStore: () => initialState,
    setCacheRoutes: (state, { payload }: PayloadAction<string[]>) => {
      state.cacheRoutes = payload;
    },
    setHomePath: (state, { payload }: PayloadAction<string>) => {
      state.routeHomePath = payload;
    },
    setRemoveCacheKey: (state, { payload }: PayloadAction<InitialStateType['removeCacheKey']>) => {
      state.removeCacheKey = payload;
    }
  },
  selectors: {
    selectCacheRoutes: route => route.cacheRoutes,
    selectRemoveCacheKey: route => route.removeCacheKey,
    selectRouteHomePath: route => route.routeHomePath
  }
});

export const { addCacheRoutes, resetRouteStore, setCacheRoutes, setHomePath, setRemoveCacheKey } = routeSlice.actions;

export const { selectCacheRoutes, selectRemoveCacheKey, selectRouteHomePath } = routeSlice.selectors;
