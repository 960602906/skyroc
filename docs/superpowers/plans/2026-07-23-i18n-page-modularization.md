# `page` 国际化资源模块化拆分计划

> 文档状态：资源拆分已完成，静态验收通过；运行时验收限制见第 6 节。
>
> 编写日期：2026-07-23。

## 1. 背景与目标

拆分前，英文 `page.ts` 为 1,314 行，中文 `page.ts` 为 1,318 行；两种语言均包含 1,093 个叶子键，并与 `App.I18n.Schema['translation']['page']` 完全一致。

本次只调整页面语言资源的物理文件组织：按现有 13 个顶级键建立独立模块，降低单文件体积和多人维护冲突。所有文案、键名、插值占位符、页面调用路径、类型契约和运行时加载方式保持不变。

## 2. 固定方案

- 将 `client/src/locales/langs/en-us/page.ts` 与 `zh-cn/page.ts` 分别迁移为 `page/index.ts` 聚合入口。
- 两种语言均建立 `about.ts`、`after-sale.ts`、`customer.ts`、`dashboard.ts`、`function.ts`、`goods.ts`、`home.ts`、`login.ts`、`manage.ts`、`order.ts`、`pickup-task.ts`、`purchase.ts`、`storage.ts`。
- 每个模块只导出对应顶级键，并使用 `App.I18n.Schema['translation']['page'][模块名]` 独立校验；`function.ts` 使用变量名 `functionPage`。
- `page/index.ts` 按原顺序聚合全部模块，并继续声明完整的 `App.I18n.Schema['translation']['page']` 类型。
- `en-us/index.ts` 与 `zh-cn/index.ts` 继续通过 `import page from './page'` 加载，所有 `page.*` 调用保持不变。
- 更新 `client/AGENT.md` 的维护入口；历史任务文档中的旧路径不回写。

## 3. 范围与兼容性

- 不拆分或修改 `App.I18n.Schema`，不新增、删除或重命名国际化键。
- 不修改 `route.ts`、`common.ts`、`form.ts` 等其他语言资源。
- 不引入懒加载；聚合后的资源对象和现有同步加载行为保持一致。
- 不更新菜单开发进度或任务状态，本次重构不属于具体菜单页面交付。
- 无数据迁移和部署顺序要求。需要回滚时，恢复两份原始 `page.ts` 并撤销维护指引即可。

## 4. 验收标准

1. 拆分前后展平两种语言资源，键、值和插值占位符完全一致，各保持 1,093 个叶子键。
2. 中英文均包含相同的 13 个模块，且聚合入口满足完整 `page` 类型。
3. `pnpm typecheck`、新语言模块 ESLint、`pnpm build` 与 `git diff --check` 全部通过。
4. 中英文切换后，登录、客户、商品、订单、采购、入库与售后代表页面不显示原始国际化键。

## 5. 实施约束

- 以实施时最新工作树内容为准，机械迁移已有文案，不在拆分过程中顺带调整翻译。
- 保留 `page` 顶级键作为稳定模块边界，不重新设计业务命名。
- 若键值等价检查或任一质量门禁失败，先修复当前拆分，不扩大到其他国际化文件。

## 6. 实施与验收结果

- 中英文均已拆为 13 个模块和一个聚合入口；各 1,093 个叶子键，拆分前后键值内容 SHA-256 完全一致。
- `pnpm typecheck`、新语言模块 ESLint、`pnpm build` 与 `git diff --check` 通过；构建仅保留既有的大 chunk 警告。
- 中文登录、客户、商品、订单、采购计划和售后页面未显示原始国际化键。
- 采购入库页存在 13 个实施前已有的错误调用：页面将 `page.storage.in.*` 公共字段引用为不存在的 `page.storage.in.purchase.*`。本次计划明确禁止修改 key 和页面调用，因此只记录证据，不在本次机械拆分中修复。
- 当前浏览器控制未能操作仅通过悬停展开的语言菜单，英文资源完成了键值等价、类型、ESLint 与生产构建验证，但未完成英文界面切换手测。
