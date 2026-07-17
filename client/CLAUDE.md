# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

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
pnpm commit             # Conventional Commits helper (sa git-commit)
```

There is no unit-test runner configured. Verification = `pnpm typecheck` + `pnpm lint`. The pre-commit hook runs `pnpm typecheck && pnpm lint-staged`, and commit messages are validated against Conventional Commits — do not bypass these.

## Architecture

pnpm monorepo: the root is the app shell (`src/`), `packages/` holds reusable workspace packages. Prefer changing the relevant `@sa/*` package for shared logic, then consume it from `src/`.

- `@sa/axios` — HTTP client wrapper (interceptors, backend-success detection, error handling)
- `@sa/hooks` `@sa/utils` `@sa/color` — shared hooks / utilities / color tools
- `@sa/materials` — shared layout & UI components (AdminLayout, PageTab, etc.)
- `@sa/scripts` — the `sa` CLI (route gen, git-commit, release, cleanup)
- `@sa/uno-preset` — UnoCSS preset

### `src/` layout

- `pages/` — file-system routes (see Routing below).
- `features/` — domain modules (auth, theme, menu, tab, router, lang…); each bundles its own hooks/store/components and exposes `index.ts`. Put business logic here, not in pages.
- `layouts/` — layout skeletons and global modules (menu, tabs, theme drawer).
- `components/` — cross-feature reusable components.
- `router/` — route initialization, auth guards, keep-alive cache; merges generated routes with permissions.
- `service/` — API layer (see below).
- `store/` — Redux store (`combineSlices`) and `createAppSlice`.
- `theme/`, `styles/` — theme tokens, CSS/SCSS, UnoCSS vars.
- `config.ts` — central config sourced from `import.meta.env`.

Path aliases: `@/*` → `src/*`, `~/*` → repo root. Prefer aliases over deep relative paths.

### Service layer (`src/service/`)

Strict layering — when adding an endpoint, touch each layer in order:

1. `types/*.d.ts` — declare request/response types under the `Api.<Module>` namespace **first**.
2. `urls/` — add the path constant (`<MODULE>_URLS`, e.g. `AUTH_URLS`).
3. `api/*.ts` — one file per backend module; functions prefixed `fetch` (e.g. `fetchGetUserInfo`). Call the shared `request` from `src/service/request`; return `Promise<业务类型>` — never destructure `response.data` in business code (`transformBackendResponse` already returns `response.data.data`).
4. `hooks/` — TanStack Query hooks named `useResource`; combine `fetch` fns with `keys/` `QUERY_KEYS`.

`request` (from `@sa/axios`) auto-injects the `Authorization` header from `localStg`, treats `VITE_SERVICE_SUCCESS_CODE` as success, and calls `backEndFail` otherwise. `scripts/generate-service-layer.mjs` scaffolds CRUD modules across all these layers.

### State management

- **Redux Toolkit** for local/global UI state: create slices with `createAppSlice`, place them under the owning feature, register in `src/store/index.ts`. Access via `useAppDispatch`/`useAppSelector` (`src/hooks/business/useStore.ts`) — never call `store.dispatch` directly. `RootState` is inferred, don't hand-write it.
- **TanStack Query** for server data (lists, cached requests). Combine the two by invalidating queries after mutations that change Redux state (e.g. re-fetch user info after login).
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
- **Components:** function components + hooks; single default export per component, named exports for groups; import order React/libs → third-party hooks → `@sa/*` → `@/` → relative → styles. Wrap error-prone pages in `ErrorBoundary`.
- **i18n always on:** new UI text goes in `src/locales` language packs (`route.*`, `page.*`, `common.*`), updated across all languages.

## Backend integration

The dev proxy is driven by `.env` (`VITE_SERVICE_BASE_URL`, `VITE_OTHER_SERVICE_BASE_URL`) via `build/config/proxy.ts` + `src/utils/service.ts`; toggle with `VITE_HTTP_PROXY=Y`. Backend response codes are configured in `.env`: `VITE_SERVICE_SUCCESS_CODE` (success), `VITE_SERVICE_LOGOUT_CODES` / `VITE_SERVICE_MODAL_LOGOUT_CODES` (force logout), `VITE_SERVICE_EXPIRED_TOKEN_CODES` (trigger token refresh). Env files: `.env` (shared/test), `.env.test`, `.env.prod`. Do not commit real API tokens (note: `.cursor/mcp.json` currently contains an Apifox token — treat such values as secrets).
