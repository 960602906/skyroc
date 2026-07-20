# SkyrocAdmin 统一编码代理规范

本文件是 `client/` 的共享客户端规范，适用于 Codex、Claude Code 与其他编码代理。开始前还必须阅读父目录的 [`AGENTS.md`](../AGENTS.md)；后端改动同时遵循 [`../CLAUDE.md`](../CLAUDE.md)。与此文件冲突时，以更近目录的规则和 `.cursor/rules/*.mdc` 为准。

## 必读规则

按任务范围读取以下规则，不能只依赖摘要：

- 所有客户端任务：`.cursor/rules/project.mdc`、`naming.mdc`、`typescript.mdc`、`comments.mdc`。
- API 改动：`.cursor/rules/api.mdc`。
- 页面或路由改动：`.cursor/rules/routing.mdc`；全页新增、编辑、详情还必须读 `operate-detail-layout.mdc`。
- 日期时间：`.cursor/rules/datetime-display.mdc`。
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

## 实现硬性约定

- TypeScript 保持 `strict`、`isolatedModules`；不用 `any`，API 类型先定义在 `src/service/types/*.d.ts` 的 `Api.*` 命名空间。
- 新接口按顺序维护：`types` → `urls` → `api` → hooks/调用页。只能用 `src/service/request`；业务代码不得解构 `response.data`。
- 分页 CRUD 列表用 `features/table` 的 `useTable`/`useTableOperate`；非分页缓存使用 TanStack Query hooks 与 `QUERY_KEYS`，禁止硬编码 query key。
- 动态编辑/详情页首屏数据用 React Router `loader` + `useLoaderData`；`useEffect` 只可回填表单，不可首屏按 id 拉详情。
- 全页新增/编辑/详情采用分区卡片布局；取消、返回、保存成功使用 `useCloseTabAndNavigate` 关闭当前页签后回列表。
- 新 UI 文案必须同步所有语言包；业务枚举需提供统一 Record、语义色 Badge/Select，页面不得内联枚举选项。
- 审计时间用 `displayDateTime`，业务日期用 `displayDate`；勿在页面手写时区转换。
- UnoCSS 优先，复用主题 token 和 `card-wrapper`；链接用 `AButton type="link"` 跟随主题色，禁止硬编码颜色。
- 路由生成文件由 Vite 插件维护；路由元数据放在 `build/plugins/router.ts` 的 `ROUTE_META_PRESETS`，不要手改生成结果。
- 不提交真实 API token 或 `client/.cursor/mcp.json`；仅提交 `.cursor/mcp.json.example`。

## 交付前

1. 保留已有用户改动，执行 `git diff --check`。
2. 完成类型与格式验证；涉及 UI 时通过本地开发服务器手动走关键交互。
3. 更新必要的国际化、路由元数据、类型与前端开发进度记录。
