import { combineSlices, configureStore } from '@reduxjs/toolkit';
import type { Action, ThunkAction } from '@reduxjs/toolkit';

// 必须从具体 store 文件导入，禁止走 feature barrel：
// barrel 会连带执行 router.ts（顶层 navigator()），与 store 形成循环依赖。
import { appSlice } from '../features/app/app-store';
import { authSlice } from '../features/auth/auth-store';
import { routeSlice } from '../features/router/route-store';
import { tabSlice } from '../features/tab/tab-store';
import { themeSlice } from '../features/theme/theme-settings-store';

// `combineSlices` automatically combines the reducers using
// their `reducerPath`s, therefore we no longer need to call `combineReducers`.
const rootReducer = combineSlices(appSlice, authSlice, themeSlice, routeSlice, tabSlice);

// Infer the `RootState` type from the root reducer
export type RootState = ReturnType<typeof rootReducer>;

export const store = configureStore({
  reducer: rootReducer
});

// Infer the type of `store`
export type AppStore = typeof store;
// Infer the `AppDispatch` type from the store itself
export type AppDispatch = AppStore['dispatch'];
// eslint-disable-next-line @typescript-eslint/no-invalid-void-type
export type AppThunk<ReturnType = void> = ThunkAction<ReturnType, RootState, unknown, Action>;
