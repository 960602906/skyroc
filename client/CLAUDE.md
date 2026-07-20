# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **统一客户端规范入口：** 开始客户端任务前必须阅读 [`AGENT.md`](./AGENT.md)、父目录的 [`AGENTS.md`](../AGENTS.md)，以及匹配任务范围的 [`.cursor/rules/`](./.cursor/rules/) 规则。
> `AGENT.md` 是 Claude Code、Codex 与其他代理共享的客户端规范；本文件仅保留 Claude 导航与背景说明，不得形成独立规则版本。

SkyrocAdmin (`client/`) is the React 19 admin frontend for the SkyRoc fresh-produce supply-chain platform (the .NET backend lives one level up — see `../CLAUDE.md`). It is based on the Soybean Admin template. Most comments and docs are in Chinese; keep comments in Chinese to match.

## Commands

Use **pnpm** only (never npm/yarn — this is a pnpm monorepo with `workspace:*` deps):

```bash
pnpm i                  # install
pnpm dev                # dev server (vite --mode test), http://localhost:9528
pnpm dev:prod           # dev server with prod env
pnpm build              # production build (--mode prod)
pnpm build:test         # test-environment build
pnpm preview            # preview built output on :9725
pnpm typecheck          # tsc --noEmit --skipLibCheck (strict)
pnpm lint               # eslint . --fix
pnpm gen-route          # regenerate file-system route declarations from src/pages
pnpm commit             # optional Conventional Commits helper (sa git-commit)
```

There is no unit-test runner configured. Verification = `pnpm typecheck` + `pnpm lint`. The pre-commit hook runs `pnpm typecheck && pnpm lint-staged`. Commit messages are not enforced.

## Architecture

pnpm monorepo: the root is the app shell (`src/`), `packages/` holds reusable workspace packages. Prefer changing the relevant `@sa/*` package for shared logic, then consume it from `src/`.

- `@sa/axios` — HTTP client wrapper (interceptors, backend-success detection, error handling)
- `@sa/fetch` — ofetch wrapper（目录名 `packages/ofetch`）
- `@sa/hooks` `@sa/utils` `@sa/color` — shared hooks / utilities / color tools
- `@sa/materials` — shared layout & UI components (AdminLayout, PageTab, etc.)
- `@sa/scripts` — the `sa` CLI (route gen, git-commit, release, cleanup)
- `@sa/uno-preset` — UnoCSS preset

### `src/` layout

- `pages/` — file-system routes (see Routing below).
- `features/` — domain modules (app、auth、theme、menu、tab、router、lang…); each bundles its own hooks/store/components and exposes `index.ts`. Put business logic here, not in pages. Cross-cutting layout UI state lives in `features/app`（`appSlice`）。
- `layouts/` — layout skeletons and global modules (menu, tabs, theme drawer).
- `components/` — cross-feature reusable components.
- `router/` — route initialization, auth guards, keep-alive cache; merges generated routes with permissions.
- `service/` — API layer (see below).
- `store/` — Redux store (`combineSlices`)。
- `theme/`, `styles/` — theme tokens, CSS/SCSS, UnoCSS vars.
- `config.ts` — central config sourced from `import.meta.env`.

Path aliases: `@/*` → `src/*`, `~/*` → repo root. Prefer aliases over deep relative paths.

### Service layer (`src/service/`)

Strict layering — when adding an endpoint, touch each layer in order:

1. `types/*.d.ts` — declare request/response types under the `Api.<Module>` namespace **first**.
2. `urls/` — add the path constant (`<MODULE>_URLS`, e.g. `AUTH_URLS`).
3. `api/*.ts` — one file per backend module; functions prefixed `fetch` (e.g. `fetchGetUserInfo`). Call the shared `request` from `src/service/request`; return `Promise<业务类型>` — never destructure `response.data` in business code (`transformBackendResponse` already returns `response.data.data`).
4. **数据消费（两条路径，勿混用）：**
   - **CRUD 列表/分页页**：用 `features/table` 的 `useTable` / `useTableOperate`（ahooks）。mutation 后用 ahooks 的 `run` / `refresh` 刷新列表，不要求 TanStack `invalidateQueries`。
   - **非分页缓存场景**（auth、route、下拉选项、树等）：在 `hooks/` 写 TanStack Query hooks（`useResource`），并用 `keys/` 的 `QUERY_KEYS`。页面内联 `useQuery` 时 key 也必须引用 `QUERY_KEYS`，禁止硬编码。

`request` (from `@sa/axios`) auto-injects the `Authorization` header from `localStg`, treats `VITE_SERVICE_SUCCESS_CODE` as success, and calls `backEndFail` otherwise. `scripts/generate-service-layer.mjs` scaffolds CRUD modules across layers 1–3.

### State management

- **Redux Toolkit** for local/global UI state: create slices with RTK `createSlice`，放在 owning feature 下，并在 `src/store/index.ts` 注册。组件内用 `useAppDispatch`/`useAppSelector`（`src/hooks/business/useStore.ts`）。**例外：** 非组件模块（axios 错误处理、路由 bootstrap）可直接 `store.dispatch`。`RootState` 由 `ReturnType` 推断，勿手写。
- **TanStack Query** 仅用于上述非分页缓存场景；CRUD 列表以 ahooks `useTable` 为主。
- Persist via `localStg`/`sessionStg` (`src/utils/storage.ts`), never raw `localStorage`.

### Routing

File-system routing via `@soybean-react/vite-plugin-react-router`:

1. Add page/layout files under `src/pages` (`page.tsx`, `layout.tsx`, `loading.tsx`, folder + `index.tsx` for complex pages).
2. Run `pnpm gen-route` to regenerate declarations under `src/router/elegant/routes`.
3. `src/router/index.ts` merges generated config with auth/keep-alive.

Segments: `(base)` = main layout at `/`, `(blank)` = minimal layout (login), `_builtin` = routes not shown in menus (error/iframe pages). Dynamic params use Next.js style: `[id].tsx`, `[...all].tsx`. Export a `handle` object from a page to declare menu text, `i18nKey`, icon, order, and `keepAlive`. `VITE_AUTH_ROUTE_MODE` (static|dynamic) controls how routes/permissions resolve.

## Conventions

- **TypeScript:** `strict` + `isolatedModules` are on — no `any` (use `unknown`/generics), explicit exported types, `Promise<具体类型>`, generic-constrained Query hooks (`useQuery<Api.SystemManage.RoleList>`), `satisfies` for typed constants. Don't edit generated `.d.ts` (`auto-imports.d.ts`).
- **Naming:** directories kebab-case; component files PascalCase; hooks/utils `use-xxx.ts`/camelCase; constants SCREAMING_SNAKE_CASE; API types under `Api.*`, app-global types under `App.*`.
- **Styling:** UnoCSS-first (config in `uno.config.ts`, reuse defined shortcuts like `card-wrapper`); theme tokens in `src/theme/vars.ts`/`settings.ts`; Ant Design theme via `ConfigProvider`. Avoid inline magic numbers — extend `App.Theme.ThemeToken`. Icons via `@iconify/react` `Icon` or local svg (`vite-plugin-svg-icons`).
- **Operate / detail layout:** 档案类新增、编辑、详情用分区卡片（一区一卡，对齐商品档案）；取消 / 返回列表 / 保存成功须 `useCloseTabAndNavigate` 关当前签再回列表。见 `.cursor/rules/operate-detail-layout.mdc`。
- **Date / time display:** 后端审计时间（`createTime`/`updateTime`/`auditTime` 等）为 UTC、JSON 无时区后缀；列表与详情须用 `displayDateTime`（UTC→本地）。业务日历日（`orderDate`/`receiveDate` 等）用 `displayDate`（只取日期、不做时区换算）。见 `.cursor/rules/datetime-display.mdc` 与 `src/utils/datetime.ts`。
- **Components:** function components + hooks; single default export per component, named exports for groups; import order React/libs → third-party hooks → `@sa/*` → `@/` → relative → styles. Wrap error-prone pages in `ErrorBoundary`.
- **i18n always on:** new UI text goes in `src/locales` language packs (`route.*`, `page.*`, `common.*`), updated across all languages.

## Backend integration

The dev proxy is driven by `.env` (`VITE_SERVICE_BASE_URL`, `VITE_OTHER_SERVICE_BASE_URL`) via `build/config/proxy.ts` + `src/utils/service.ts`; toggle with `VITE_HTTP_PROXY=Y`. Backend response codes are configured in `.env`: `VITE_SERVICE_SUCCESS_CODE` (success), `VITE_SERVICE_LOGOUT_CODES` / `VITE_SERVICE_MODAL_LOGOUT_CODES` (force logout), `VITE_SERVICE_EXPIRED_TOKEN_CODES` (trigger token refresh). Env files: `.env` (shared/test), `.env.test`, `.env.prod`. Do not commit real API tokens — copy `client/.cursor/mcp.json.example` to the gitignored `client/.cursor/mcp.json` and fill locally.
