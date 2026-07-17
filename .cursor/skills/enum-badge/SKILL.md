---
name: enum-badge
description: >-
  Encapsulates SkyrocAdmin (client/) business enum values as ABadge-based global
  components (Badge + Select + render helper) with semantic status colors. Use
  when adding or displaying enums, status/type columns, search filters,
  EnableStatus, YesOrNo, MenuType, UserGender, or any Record/options enum UI.
---

# 枚举值 Badge 封装

所有在表格、搜索栏、表单中展示的**业务枚举值**，必须按本约定封装，禁止在页面内联 `ATag` / `ABadge` / `translateOptions(xxxOptions)`。

## 参考实现（权威样板）

| 层级 | 路径 |
| --- | --- |
| 文案 Record + Options | `client/src/constants/business.ts`（`enableStatusRecord` / `enableStatusOptions`） |
| 颜色语义 Map | `client/src/constants/common.ts`（`ATG_MAP`） |
| Badge 组件 | `client/src/features/crud/EnableStatusBadge.tsx` |
| Select 组件 | `client/src/features/crud/EnableStatusSelect.tsx` |
| 表格 render | `client/src/features/crud/render-status.tsx` → `renderEnableStatus` |
| 导出 | `client/src/features/crud/index.ts` |

## 新建枚举时的固定步骤

对每个业务枚举 `Foo`（如 `EnableStatus`、`UserGender`、`MenuType`）：

1. **Record + Options**（若尚无）
   - `fooRecord: Record<Enum, I18nKey>`
   - `fooOptions = transformRecordToOption(fooRecord, true)`（数字枚举传 `true`）

2. **颜色 Map**（语义 → Ant Design Badge `status`）
   - 类型收窄为 `'success' | 'processing' | 'default' | 'error' | 'warning'`
   - 按业务含义选色，不要随意；常见约定：
     - 启用 / 成功 / 正向 → `success`
     - 禁用 / 待处理 / 弱警示 → `warning`
     - 进行中 → `processing`
     - 失败 / 危险 / 否（强调） → `error`
     - 中性 / 未知 → `default`

3. **`FooBadge` 组件**（`features/crud/FooBadge.tsx`）
   - props：`value: Enum | null | undefined`（字段名可按语义用 `status` / `type` / `gender`）
   - `value == null` 时返回 `null`
   - 渲染：`<ABadge status={FOO_MAP[value]} text={t(fooRecord[value])} />`

4. **`FooSelect` 组件**（`features/crud/FooSelect.tsx`）
   - 基于 `ASelect`，默认 `allowClear`
   - `options` 的 `label` 必须是 `<FooBadge ... />`，禁止纯文本
   - 禁止再暴露可覆盖的 `options` prop（用 `Omit<..., 'options'>`）

5. **`renderFoo`（可选）**
   - 表格列便捷函数：`return <FooBadge value={value} />`
   - 与 Badge 放在同 feature，并从 `features/crud/index.ts` 导出

6. **替换调用方**
   - 表格列：`render: (_, r) => renderFoo(r.xxx)` 或 `<FooBadge ... />`
   - 搜索栏：`<FooSelect placeholder={t('...')} />`
   - 删除页面内的 `tagMap`、`ATG_MAP` 内联、`translateOptions(fooOptions)`、本地 `ATag`

## 用法速查

```tsx
import { EnableStatusBadge, EnableStatusSelect, renderEnableStatus } from '@/features/crud';

// 表格
render: (_, record) => renderEnableStatus(record.status)

// 搜索 / 筛选
<EnableStatusSelect placeholder={t('page.xxx.form.status')} />

// 任意展示
<EnableStatusBadge status={record.status} />
```

## 禁止事项

- 在 `pages/**` 内联枚举颜色 Map + `ATag`/`ABadge`
- 搜索栏用 `options={translateOptions(enableStatusOptions)}` 等纯文本下拉
- Badge 用 `ATag` 代替（统一 `ABadge` 原点 + 文案）
- 颜色 Map 使用宽 `string` 类型（必须收窄为 Badge `status` 联合类型）
- 表单 Radio（如 `EnableStatusFormItem`）可保留；若展示选项文案，优先与 Badge 文案同源 `*Record`

## 已封装枚举

| 枚举 | Badge | Select | render |
| --- | --- | --- | --- |
| `EnableStatus` | `EnableStatusBadge` | `EnableStatusSelect` | `renderEnableStatus` |
| `UserGender` | `UserGenderBadge` | `UserGenderSelect` | `renderUserGender` |
| `MenuType` | `MenuTypeBadge` | `MenuTypeSelect` | `renderMenuType` |
| `YesOrNo` | `YesOrNoBadge` | `YesOrNoSelect` | `renderYesOrNo` |

自定义布尔文案（非标准 YesOrNo）用 `renderBooleanTag`（内部已是 `ABadge`）。

颜色 Map 集中在 `client/src/constants/common.ts`：`ATG_MAP`、`USER_GENDER_MAP`、`MENU_TYPE_MAP`、`YES_OR_NO_MAP`。

## Checklist（完成前自检）

- [ ] `*Record` / `*Options` / 颜色 `*_MAP` 已就位且类型收窄
- [ ] `*Badge` + `*Select` 已实现并从 `@/features/crud` 导出
- [ ] 表格、搜索、其它展示处已改用全局组件
- [ ] 页面无残留 `translateOptions(*Options)` 状态/枚举下拉
- [ ] `pnpm typecheck` 通过
