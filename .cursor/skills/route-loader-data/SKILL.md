---
name: route-loader-data
description: >-
  Requires SkyrocAdmin (client/) route navigations that need server data to load
  it via React Router loader + useLoaderData. Use when creating/editing detail
  or operate pages, dynamic [id] routes, jump-to-edit/view pages, or refactoring
  useEffect/fetch-on-mount into loaders.
---

# 路由跳转 Loader 取数

**所有需要服务端数据才能渲染的跳转，都使用 `loader` 函数来处理数据。**

禁止在详情/编辑等「依赖路由参数」的页面里用 `useEffect` + `fetchXxx` 首屏拉数；首屏数据必须由页面导出的 `loader` 完成，组件用 `useLoaderData` 消费。

## 何时必须用 loader

| 场景 | 做法 |
| --- | --- |
| 编辑页 / 详情页（`[id].tsx`） | `loader` 调 `fetchGetXxxDetail(id)`，返回实体 |
| 跳转依赖路由参数的数据 | 在目标页 `loader` 取数，不在来源页 `navigate(state)` 塞大对象 |
| 缺参 / 查无 / 请求失败 | `return redirect('/module/.../list')`（或业务约定的回退路径） |
| 仅重定向的索引页 | `export const loader = () => redirect('list')` |

## 何时不用 loader

- **列表分页 / 筛选**：继续用 TanStack Query（`service/hooks`），不要塞进 loader。
- **按钮提交 / 变更**：`useMutation` 或直接 `fetchUpdateXxx`，不走 loader。
- **纯静态页 / 无远程数据**：无需 loader。
- **创建页**（无 id）：可用默认值 / `useMount` 初始化表单，不必 loader。

## 标准写法（权威样板）

参考：`client/src/pages/(base)/master/goods/operate/[id].tsx`

```tsx
import { type LoaderFunctionArgs, redirect, useLoaderData } from 'react-router-dom';

import { fetchGetXxxDetail, fetchUpdateXxx } from '@/service/api';

export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) {
    return redirect('/master/xxx/list');
  }

  try {
    const detail = await fetchGetXxxDetail(id);
    if (!detail) {
      return redirect('/master/xxx/list');
    }
    return detail;
  } catch {
    return redirect('/master/xxx/list');
  }
}

const XxxEdit = () => {
  const detail = useLoaderData() as Api.Xxx.Entity;
  // 用 detail 回填表单 / 渲染；提交用 detail.id
  // ...
};

export default XxxEdit;
```

演示页（随机数据，仅作能力展示）：`client/src/pages/(base)/system/auth/users/[id].tsx`

## 固定步骤

新建或改造「跳转后要带数据」的页面时：

1. **在页面文件导出 `loader`**（与默认组件同文件），签名用 `LoaderFunctionArgs`。
2. **从 `params` 取 id**；缺失则 `redirect` 回列表。
3. **调用 `service/api` 的 `fetch*`**（不要在 loader 里直接 `axios`）；成功返回实体 / DTO。
4. **空结果与异常**：`redirect` 回退，或按业务返回 `null` 并由组件兜底（优先 redirect，避免半空页）。
5. **组件**：`const data = useLoaderData() as Api.Xxx.Entity`；用 `useEffect`/`useMount` **只做表单回填**，不再发详情请求。
6. **去掉**页内 `ASpin` 首屏 loading（路由级 `loading.tsx` / 导航过渡已覆盖）。
7. **提交 / 删除**仍留在组件内；成功后 `nav` 回列表。

## 禁止事项

- 详情/编辑页 `useEffect(() => { fetchGetXxxDetail(id) }, [id])` 作为首屏数据源
- 用 `navigate('/edit', { state: record })` 传递整份业务数据代替目标页 loader（列表行可只传 id）
- loader 里写业务校验提示 UI；失败应 redirect 或抛给路由 error boundary
- `Task<IActionResult>` 式随意返回；保持返回类型清晰（实体或 `redirect`）
- 忘记从 `react-router-dom` 显式导入 `loader` / `useLoaderData` / `redirect`（二者未进 auto-import）

## 与现有栈的边界

| 能力 | 归属 |
| --- | --- |
| 路由首屏 / 跳转依赖数据 | **loader + useLoaderData** |
| 列表、缓存、条件刷新 | TanStack Query |
| 全局 UI 状态 | Redux |
| 表单提交 | 组件内 mutation / fetch |

## Checklist

- [ ] 页面导出 `loader`，组件用 `useLoaderData`
- [ ] 无 id / 无数据 / 异常 → `redirect` 回退路径
- [ ] 已删除页内首屏 `fetch` + 对应 loading spin
- [ ] API 走 `@/service/api` 的 `fetch*`，类型为 `Api.*`
- [ ] 列表跳转只带路由 id（如 `/operate/${id}`），不依赖 location.state 灌详情
