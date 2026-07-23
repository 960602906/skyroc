# SkyrocAdmin 统一编码代理规范

本文件是 `client/` 的共享客户端规范，适用于 Codex、Claude Code 与其他编码代理。开始前还必须阅读父目录的 [`AGENTS.md`](../AGENTS.md)；后端改动同时遵循 [`../CLAUDE.md`](../CLAUDE.md)。与此文件冲突时，以更近目录的规则和 `.cursor/rules/*.mdc` 为准。

## 必读规则

按任务范围读取以下规则，不能只依赖摘要：

- 所有客户端任务：`.cursor/rules/project.mdc`、`naming.mdc`、`typescript.mdc`、`comments.mdc`。
- API 改动：`.cursor/rules/api.mdc`。
- 页面或路由改动：`.cursor/rules/routing.mdc`；全页新增、编辑、详情还必须读 `operate-detail-layout.mdc`。
- 日期时间：`.cursor/rules/datetime-display.mdc`。
- 表单/筛选里的主数据下拉（客户、商品、供应商、报价等）：`.cursor/rules/remote-option-select.mdc`（`RemoteOptionSelect`，禁止全量拉取）。
- 状态管理：`.cursor/rules/reduxing.mdc`。
- 样式或主题：`.cursor/rules/styling.mdc`。
- 业务枚举：父目录 `.cursor/skills/enum-badge/SKILL.md`。
- 编辑/详情的路由首屏数据：父目录 `.cursor/skills/route-loader-data/SKILL.md`。

## 项目与命令

SkyrocAdmin 是 React 19、Vite 6、TypeScript 5、Ant Design 5、Redux Toolkit、TanStack Query、UnoCSS 和 i18next 构成的 pnpm monorepo。只能使用 pnpm，禁止 npm/yarn。

```bash
pnpm dev
pnpm build
pnpm typecheck
pnpm lint
```

`pnpm lint` 会自动修复格式；需要只检查改动文件时直接执行 `pnpm exec eslint <files>`。没有独立单测运行器，交付至少运行 `pnpm typecheck`、相关 ESLint 检查和适用的 `pnpm build`。

## 前端接口与页面续接（强制）

前端菜单页面的持续对接以以下文档为唯一事实来源：

- [`docs/接口对接与页面完善清单.md`](./docs/接口对接与页面完善清单.md)：63 个菜单叶子页的稳定任务序列、后端接口和完成条件。
- [`docs/前端开发进度.md`](./docs/前端开发进度.md)：当前唯一任务、状态统计、阻塞项和测试基线。

当用户说“继续前端对接”“完善界面”或“继续完成菜单页面”时，不要求用户重复给出文件名、路由或接口；执行以下流程：

1. 读取上述两份文档和 `git status`，保留已有用户改动。
2. 只领取清单中第一项状态不是「已完成」或「阻塞」的菜单页面；不得跳项或并行实现其他页面。
3. 按 `types → urls → api → hooks/调用页 → 路由/i18n/权限 → 验收` 完成该项。接口以后端 Controller/OpenAPI 为准，业务请求只能通过 `src/service/request`；`api-diff-report.json` 仅用于辅助发现遗漏。
4. 页面任务完成后执行清单中的静态核对、`pnpm typecheck`、`git diff --check`，并对 UI 主流程做本地手测。
5. 仅在所有验收通过后，将该项改为「已完成」，更新前端进度的断点、统计和测试基线；下一项成为唯一当前任务。缺少必要后端契约时改为「阻塞」并记录证据，不得跳过。

## 实现硬性约定

- TypeScript 保持 `strict`、`isolatedModules`；不用 `any`，API 类型先定义在 `src/service/types/*.d.ts` 的 `Api.*` 命名空间。
- 新接口按顺序维护：`types` → `urls` → `api` → hooks/调用页。只能用 `src/service/request`；业务代码不得解构 `response.data`。
- 分页 CRUD 列表用 `features/table` 的 `useTable`/`useTableOperate`；非分页缓存使用 TanStack Query hooks 与 `QUERY_KEYS`，禁止硬编码 query key。
- 动态编辑/详情页首屏数据用 React Router `loader` + `useLoaderData`；`useEffect` 只可回填表单，不可首屏按 id 拉详情。
- 全页新增/编辑/详情采用分区卡片布局；取消、返回、保存成功使用 `useCloseTabAndNavigate` 关闭当前页签后回列表。
- 新 UI 文案必须同步所有语言包；业务枚举需提供统一 Record、语义色 Badge/Select，页面不得内联枚举选项。
- 审计时间用 `displayDateTime`，业务日期用 `displayDate`；勿在页面手写时区转换。
- 增长型主数据下拉用 `RemoteOptionSelect` + `SELECTION_OPTION_RESOURCES`（search/resolve）；有界选项用 `options/bounded` hooks；禁止全量 list 伪装下拉。详见 `remote-option-select.mdc`。
- UnoCSS 优先，复用主题 token 和 `card-wrapper`；链接用 `AButton type="link"` 跟随主题色，禁止硬编码颜色。
- 路由生成文件由 Vite 插件维护；路由元数据放在 `build/plugins/router.ts` 的 `ROUTE_META_PRESETS`，不要手改生成结果。
- 不提交真实 API token 或 `client/.cursor/mcp.json`；仅提交 `.cursor/mcp.json.example`。

## 交付前

1. 保留已有用户改动，执行 `git diff --check`。
2. 完成类型与格式验证；涉及 UI 时通过本地开发服务器手动走关键交互。
3. 更新必要的国际化、路由元数据、类型与前端开发进度记录。

## 菜单页面接入错误复盘（强制检查）

以下反例来自真实续接事故。新增或完善菜单页面时必须逐项排除，不能等用户在页面上发现。

### 详情误做弹窗

```tsx
// ❌ 清单要求“详情”却在列表中临时拉取并弹窗
async function showDetail(id: string) {
  setDetail(await fetchGetXxxDetail(id));
  setDetailModalOpen(true);
}
```

除非任务明确要求 Drawer/Modal，菜单业务对象的“详情”必须是 `detail/[id].tsx` 独立页面：列表只传 id，目标页用 React Router `loader` 取数，返回时用 `useCloseTabAndNavigate` 关闭当前页签。

### 详情路由泄漏到菜单

```ts
// ❌ 只创建 detail/[id].tsx，未配置隐藏元数据
'(base)_xxx_list': { icon: '...' }

// ✅ 父详情路由和动态详情路由都隐藏，并保持列表菜单高亮
'(base)_xxx_list_detail': { activeMenu: '/xxx/list', hideInMenu: true },
'(base)_xxx_list_detail_[id]': { activeMenu: '/xxx/list', hideInMenu: true }
```

元数据唯一来源是 `build/plugins/router.ts` 的 `ROUTE_META_PRESETS`。禁止把菜单元数据写在页面里或手改 `src/router/elegant/*` 生成文件。构建后必须检查生成的父/子详情路由都含 `hideInMenu: true`。

### 误用路由脚手架

`pnpm gen-route` / `sa gen-route` 是交互式“新建路由”脚手架，不是“重新扫描已有页面”的命令。禁止用它刷新路由，否则可能创建或覆盖 `Component` 模板。新增文件路由后运行 `pnpm dev` 或 `pnpm build`，由 Vite 路由插件同步生成文件，再检查 `routes.ts`、`imports.ts`、`routeMap.ts` 和 `elegant-router.d.ts`。

### 国际化只补一半

页面新增字段、按钮、状态、表单 placeholder、详情分区和路由标题时，必须同时更新：

1. `locales/langs/zh-cn/page/` 与 `en-us/page/` 下的对应模块文件；聚合入口为各自的 `page/index.ts`；
2. `locales/langs/zh-cn/route.ts` 与 `en-us/route.ts`（新增路由时）；
3. `types/app.d.ts` 的 `App.I18n.Schema`；
4. 业务枚举的 Record/Badge/Select 文案映射。

禁止只补能通过当前页面渲染的少量字段，或复用字段标题充当“请选择/请输入”提示。交付前用 `rg` 枚举页面实际使用的 `page.*` / `route.*` key，并逐项对照两种语言。

### 空筛选参数进入 URL

```ts
// ❌ 未选择时重新写入 null，最终出现 ?purchaseStatus=null
return { ...params, purchaseStatus: params.purchaseStatus ? Number(params.purchaseStatus) : null };

// ✅ 空值删除，选中时才转换并发送
const next = { ...params };
if (next.purchaseStatus == null) delete next.purchaseStatus;
else next.purchaseStatus = Number(next.purchaseStatus);
return next;
```

所有可选查询参数都遵守“无值不发送”；静态检查之外，还必须在浏览器 Network 中核对首次查询、筛选和重置后的真实 URL。

### 主数据 ID 让用户手输

禁止用 `AInput`、`ASelect mode="tags"` 等方式让用户输入订单、商品、单位、供应商、采购员等 GUID。必须使用有契约支撑的选项接口；存在依赖关系时做联动查询（例如选择商品后仅加载该商品的采购单位）。缺少必要选项接口时记录阻塞，不得用手输 GUID 伪装页面已完成。

### 未完成运行时验收却宣称完成

运行时 OpenAPI 不可访问、缺少有权限会话或主流程未手测时，只能报告“静态实现完成、运行时验收未完成”。不得更新清单为「已完成」，不得用“已接入/已完成”掩盖尚未验证的操作。
