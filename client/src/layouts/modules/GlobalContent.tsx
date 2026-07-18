import clsx from 'clsx';
import KeepAlive, { useKeepAliveRef } from 'keepalive-for-react';

import { getReloadFlag } from '@/features/app';
import { selectCacheRoutes, selectRemoveCacheKey, setRemoveCacheKey, usePreviousRoute } from '@/features/router';
import { useThemeSettings } from '@/features/theme';
import './transition.css';

interface Props {
  /** Show padding for content */
  closePadding?: boolean;
}

const GlobalContent = ({ closePadding }: Props) => {
  const previousRoute = usePreviousRoute();

  const dispatch = useAppDispatch();

  const currentOutlet = useOutlet(previousRoute);

  const { pathname } = useLocation();

  const aliveRef = useKeepAliveRef();

  const removeCacheKey = useAppSelector(selectRemoveCacheKey);

  const cacheKeys = useAppSelector(selectCacheRoutes);

  const reload = useAppSelector(getReloadFlag);

  const themeSetting = useThemeSettings();

  const transitionName = themeSetting.page.animate ? themeSetting.page.animateMode : '';

  // 当前路由是否需要 KeepAlive 缓存（命中 include 列表）。
  // 非缓存页（详情/多数列表）不能走 KeepAlive 的 createPortal：离开时该节点是异步 destroy，
  // 销毁前旧组件会在“新 RouteContext”下再渲染一帧，详情页 useLoaderData() 读到 undefined 而抛错，
  // 触发路由 ErrorBoundary，表现为返回列表时短暂闪错误页。改为非缓存页直接渲染 outlet，交给 RR 正常挂卸。
  const isCacheRoute = cacheKeys.includes(pathname);

  useUpdateEffect(() => {
    if (!aliveRef.current || !removeCacheKey) return;

    aliveRef.current.destroy(removeCacheKey);

    // 有的时候用户打开同一页面输入在关闭 不去切换新的页面 会造成无法二次删除缓存
    dispatch(setRemoveCacheKey(null));
  }, [removeCacheKey]);

  useUpdateEffect(() => {
    aliveRef.current?.refresh();
  }, [reload, transitionName]);

  return (
    <div className={clsx('h-full flex-grow bg-layout', { 'p-16px': !closePadding })}>
      {/* 非缓存页直出 outlet，避免 KeepAlive portal 残留帧误读 loader 数据 */}
      {!isCacheRoute && !reload && <div className="h-full">{currentOutlet}</div>}

      <KeepAlive
        // 非缓存页用非命中哨兵，避免为详情/列表新建 cache node，同时保留已缓存页
        activeCacheKey={isCacheRoute ? pathname : '__no_cache__'}
        aliveRef={aliveRef}
        cacheNodeClassName={reload ? '' : transitionName}
        containerClassName={clsx('keep-alive-render', { 'overflow-hidden h-0!': !isCacheRoute })}
        include={cacheKeys}
      >
        {isCacheRoute && !reload ? currentOutlet : null}
      </KeepAlive>
    </div>
  );
};

export default GlobalContent;
