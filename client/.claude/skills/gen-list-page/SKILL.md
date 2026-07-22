---
name: gen-list-page
description: 为 Skyroc Admin 前端项目生成完整的列表页（CRUD），包括 service 层（types/urls/api）、列表页组件、搜索栏。严格遵循项目规范和架构约定。用于快速搭建新的业务模块列表页。
disable-model-invocation: true
allowed-tools: Read, Write, Edit, Glob, Grep
---

# 生成 Skyroc Admin 列表页

为 Skyroc Admin 前端项目生成完整的 CRUD 列表页，严格遵循 `AGENT.md` 和 `.cursor/rules/` 的规范。

## 参数格式

```
/gen-list-page <module-name> <feature-name> [--with-drawer]
```

**示例**：
```bash
/gen-list-page warehouse product        # 生成仓库商品列表
/gen-list-page purchase order --with-drawer  # 生成采购单列表（带抽屉）
```

---

## 生成流程

### 步骤 1：读取项目规范（必须）

开始前**必须**读取以下文件，确保遵循规范：

1. **`client/AGENT.md`** - 客户端核心规范
2. **`client/.cursor/rules/crud-list-conventions.mdc`** - CRUD 列表约定（如存在）
3. **`client/.cursor/rules/datetime-display.mdc`** - 日期时间显示规则
4. **参考实现**：选择最接近的已有列表页作为模板（采购单、订单、角色等）

### 步骤 2：生成 Service 层（按顺序）

#### 2.1 类型声明 (`src/service/types/<module-name>.d.ts`)

```typescript
declare namespace Api {
  namespace <ModuleName> {
    /** <实体名称>实体（继承 Common.CommonRecord） */
    type Entity = Common.CommonRecord<{
      // 业务字段
      code: string;
      name: string;
      status: number;
      // ... 根据需求添加
    }>;

    /** 创建请求 */
    type CreatePayload = {
      // 必填字段
      name: string;
      // 可选字段标记为 ? | null
      remark?: string | null;
    };

    /** 编辑请求 */
    type UpdatePayload = CreatePayload & { id: string };

    /** 搜索参数（所有字段均可为 null） */
    type SearchParams = CommonType.RecordNullable<
      Api.Base.SearchParams & {
        keyword?: string;
        status?: number;
        // ... 其他筛选条件
      }
    >;

    /** 分页返回 */
    type List = Common.PaginatingQueryRecord<Entity>;
  }
}
```

**规范要点**：
- 所有实体继承 `Common.CommonRecord`（自动包含 id/createTime/updateTime/status）
- 搜索参数用 `CommonType.RecordNullable` 包裹（支持 URL 回填 null 值）
- 日期时间字段用 `string`（后端 UTC 格式）
- 枚举类型引用 `import('../enums').XxxValue`

---

#### 2.2 URL 常量 (`src/service/urls/<module-name>.ts`)

```typescript
/** <模块中文名> URL */
export const <MODULE_NAME>_URLS = {
  BASE: '/<kebab-case-module>',
  LIST: '/<kebab-case-module>/list'
} as const;
```

---

#### 2.3 API 函数 (`src/service/api/<module-name>.ts`)

```typescript
import { request } from '../request';
import { <MODULE_NAME>_URLS } from '../urls';

/** 分页查询<实体名称>。需要<权限说明>。 */
export function fetchGet<FeatureName>List(params?: Api.<ModuleName>.SearchParams) {
  return request<Api.<ModuleName>.List>({
    method: 'get',
    params,
    url: <MODULE_NAME>_URLS.LIST
  });
}

/** 创建<实体名称>。需要<权限说明>。 */
export function fetchAdd<FeatureName>(data: Api.<ModuleName>.CreatePayload) {
  return request<Api.<ModuleName>.Entity>({
    method: 'post',
    data,
    url: <MODULE_NAME>_URLS.BASE
  });
}

/** 编辑<实体名称>。需要<权限说明>。 */
export function fetchUpdate<FeatureName>(data: Api.<ModuleName>.UpdatePayload) {
  return request<Api.<ModuleName>.Entity>({
    method: 'put',
    data,
    url: <MODULE_NAME>_URLS.BASE
  });
}

/** 删除<实体名称>。需要<权限说明>。 */
export function fetchDelete<FeatureName>(id: string) {
  return request<boolean>({
    method: 'delete',
    url: `${<MODULE_NAME>_URLS.BASE}/${id}`
  });
}

/** 查询<实体名称>详情。需要<权限说明>。 */
export function fetchGet<FeatureName>Detail(id: string) {
  return request<Api.<ModuleName>.Entity>({
    method: 'get',
    url: `${<MODULE_NAME>_URLS.BASE}/${id}`
  });
}
```

**命名规范**：
- 函数名：`fetch<动词><实体><可选操作>`
- 列表：`fetchGetXxxList`
- 新增：`fetchAddXxx`
- 编辑：`fetchUpdateXxx`
- 删除：`fetchDeleteXxx`
- 详情：`fetchGetXxxDetail`

---

### 步骤 3：生成列表页 (`src/pages/(base)/<module>/<feature>/index.tsx`)

```typescript
import { Suspense } from 'react';
import { useNavigate, useTranslation } from '@/hooks';
import {
  CrudPageLayout,
  createDefaultPagination,
  createIndexColumn,
  displayDateTime,
  displayText,
  renderEnableStatus
} from '@/features/crud';
import { TableHeaderOperation, useTable, useTableOperate } from '@/features/table';
import {
  fetchGet<FeatureName>List,
  fetchAdd<FeatureName>,
  fetchUpdate<FeatureName>,
  fetchDelete<FeatureName>,
  fetchGet<FeatureName>Detail
} from '@/service/api';

import <FeatureName>Search from './modules/<FeatureName>Search';

const <FeatureName>List = () => {
  const { t } = useTranslation();
  const nav = useNavigate();

  // 1. 搜索参数（所有可选字段设为 null）
  const searchParams = {
    current: 1,
    size: 10,
    keyword: null,
    status: null
    // ... 其他筛选字段
  };

  // 2. useTable hook
  const { columnChecks, data, run, searchProps, setColumnChecks, tableProps, tableWrapperRef } = useTable({
    apiFn: fetchGet<FeatureName>List,
    apiParams: searchParams,
    columns: () => [
      createIndexColumn(t),
      {
        align: 'center',
        dataIndex: 'code',
        fixed: 'left',
        key: 'code',
        render: (value: string, record) => (
          <AButton
            className="p-0"
            type="link"
            onClick={() => nav(`/<module>/<feature>/detail/${record.id}`)}
          >
            {value}
          </AButton>
        ),
        title: t('page.<module>.<feature>.code'),
        width: 150
      },
      {
        align: 'center',
        dataIndex: 'name',
        key: 'name',
        title: t('page.<module>.<feature>.name'),
        width: 200
      },
      {
        align: 'center',
        dataIndex: 'createTime',
        key: 'createTime',
        render: displayDateTime, // UTC → 本地时间
        title: t('page.<module>.<feature>.createTime'),
        width: 180
      },
      {
        align: 'center',
        dataIndex: 'status',
        key: 'status',
        render: renderEnableStatus,
        title: t('common.status'),
        width: 100
      },
      {
        align: 'center',
        fixed: 'right',
        key: 'operate',
        render: (_, record) => (
          <div className="flex-center flex-wrap gap-8px">
            <AButton ghost size="small" type="primary" onClick={() => handleEdit(record.id)}>
              {t('common.edit')}
            </AButton>
            <APopconfirm title={t('common.confirmDelete')} onConfirm={() => handleDelete(record.id)}>
              <AButton danger size="small">{t('common.delete')}</AButton>
            </APopconfirm>
          </div>
        ),
        title: t('common.operate'),
        width: 200
      }
    ],
    pagination: createDefaultPagination(),
    scroll: { x: 'max-content' },
    transformParams: params => {
      const next = { ...params } as Api.<ModuleName>.SearchParams;
      
      // 删除未填写字段（避免发送 null）
      if (next.status === null || next.status === undefined) {
        delete next.status;
      }
      if (next.keyword === null || next.keyword === undefined) {
        delete next.keyword;
      }
      
      return next;
    }
  });

  // 3. 操作逻辑
  async function handleEdit(id: string) {
    const detail = await fetchGet<FeatureName>Detail(id);
    // TODO: 表单回填逻辑（Dayjs 转换等）
    // handleEdit(formValue);
  }

  async function handleDelete(id: string) {
    await fetchDelete<FeatureName>(id);
    run(false); // 刷新当前页
  }

  // 4. 布局渲染
  return (
    <CrudPageLayout
      search={<<FeatureName>Search {...searchProps} />}
      tableWrapperRef={tableWrapperRef}
      title={t('page.<module>.<feature>.title')}
      extra={
        <TableHeaderOperation
          disabledDelete
          add={() => {/* TODO */}}
          columns={columnChecks}
          loading={tableProps.loading}
          refresh={run}
          setColumnChecks={setColumnChecks}
          onDelete={() => undefined}
        />
      }
      table={<ATable size="small" {...tableProps} />}
    />
  );
};

export default <FeatureName>List;
```

**关键点**：
- 编号列渲染为链接，点击跳详情
- 审计时间用 `displayDateTime`（UTC → 本地）
- 业务日期用 `displayDate`（不做时区换算）
- `transformParams` 必须删除 `null` 值
- 固定列场景必须设置 `scroll.x`

---

### 步骤 4：生成搜索栏 (`src/pages/(base)/<module>/<feature>/modules/<FeatureName>Search.tsx`)

```typescript
import { memo } from 'react';
import type { FC } from 'react';
import { useTranslation } from '@/hooks';
import RemoteOptionSelect from '@/components/RemoteOptionSelect';
import { SearchActionsCol } from '@/features/crud';
import { SELECTION_OPTION_RESOURCES } from '@/service/hooks';

const <FeatureName>Search: FC<Page.SearchProps> = memo(({ form, reset, search, searchParams }) => {
  const { t } = useTranslation();

  return (
    <AForm form={form} initialValues={searchParams} labelCol={{ md: 7, span: 5 }}>
      <ARow wrap gutter={[16, 16]}>
        <ACol lg={6} md={12} span={24}>
          <AForm.Item className="m-0" label={t('page.<module>.<feature>.keyword')} name="keyword">
            <AInput allowClear placeholder={t('page.<module>.<feature>.form.keyword')} />
          </AForm.Item>
        </ACol>

        <ACol lg={6} md={12} span={24}>
          <AForm.Item className="m-0" label={t('common.status')} name="status">
            <ASelect
              allowClear
              placeholder={t('page.<module>.<feature>.form.status')}
              options={[
                { label: t('common.enable'), value: 1 },
                { label: t('common.disable'), value: 0 }
              ]}
            />
          </AForm.Item>
        </ACol>

        {/* 增长型主数据用 RemoteOptionSelect */}
        {/* <ACol lg={6} md={12} span={24}>
          <AForm.Item className="m-0" label={t('...')} name="xxxId">
            <RemoteOptionSelect
              allowClear
              placeholder={t('...')}
              resource={SELECTION_OPTION_RESOURCES.XXX}
            />
          </AForm.Item>
        </ACol> */}

        <SearchActionsCol fieldCount={2} onReset={reset} onSearch={search} />
      </ARow>
    </AForm>
  );
});

export default <FeatureName>Search;
```

**规范**：
- 使用 `Page.SearchProps` 类型
- `initialValues={searchParams}` 用于 URL 回填
- 增长型主数据用 `RemoteOptionSelect`
- 有界选项用 `ASelect` 或现成的 `XxxSelect` 组件
- `SearchActionsCol` 自动占满剩余栅格

---

### 步骤 5：生成抽屉（可选，`--with-drawer`）

如果指定了 `--with-drawer`，生成 `src/pages/(base)/<module>/<feature>/modules/<FeatureName>OperateDrawer.tsx`：

```typescript
import { memo } from 'react';
import type { FC } from 'react';
import { useTranslation } from '@/hooks';
import { useFormRules } from '@/features/form';
import RemoteOptionSelect from '@/components/RemoteOptionSelect';
import { SELECTION_OPTION_RESOURCES } from '@/service/hooks';

const <FeatureName>OperateDrawer: FC<Page.OperateDrawerProps> = memo(
  ({ form, handleSubmit, onClose, open, operateType }) => {
    const { t } = useTranslation();
    const { defaultRequiredRule } = useFormRules();
    const isEdit = operateType === 'edit';

    return (
      <ADrawer
        open={open}
        title={isEdit ? t('page.<module>.<feature>.edit') : t('page.<module>.<feature>.add')}
        width={700}
        footer={
          <AFlex justify="space-between">
            <AButton onClick={onClose}>{t('common.cancel')}</AButton>
            <AButton type="primary" onClick={handleSubmit}>{t('common.confirm')}</AButton>
          </AFlex>
        }
        onClose={onClose}
      >
        <AForm form={form} layout="vertical">
          <AForm.Item hidden name="id"><AInput /></AForm.Item>

          <AForm.Item label={t('page.<module>.<feature>.name')} name="name" rules={[defaultRequiredRule]}>
            <AInput placeholder={t('page.<module>.<feature>.form.name')} />
          </AForm.Item>

          <AForm.Item label={t('page.<module>.<feature>.remark')} name="remark">
            <AInput.TextArea placeholder={t('page.<module>.<feature>.form.remark')} rows={3} />
          </AForm.Item>
        </AForm>
      </ADrawer>
    );
  }
);

export default <FeatureName>OperateDrawer;
```

并在列表页中添加：
- 导入抽屉组件：`const <FeatureName>OperateDrawer = lazy(() => import('./modules/<FeatureName>OperateDrawer'));`
- 调用 `useTableOperate` hook
- 在 `table` 中渲染：`<Suspense><FeatureName>OperateDrawer {...generalPopupOperation} /></Suspense>`

---

### 步骤 6：更新国际化（提示即可）

提示用户需要在语言包中添加文案（不自动生成，避免破坏 JSON 格式）：

```
📝 请手动添加以下国际化文案到 src/locales/langs/zh-cn.json：

"page": {
  "<module>": {
    "<feature>": {
      "title": "<标题>",
      "code": "<编号>",
      "name": "<名称>",
      "keyword": "关键字",
      "form": {
        "keyword": "请输入关键字",
        "status": "请选择状态"
      }
    }
  }
}
```

---

## 执行前检查清单

生成代码前必须：

- [ ] 读取 `client/AGENT.md`（核心规范）
- [ ] 读取 `client/.cursor/rules/crud-list-conventions.mdc`（如存在）
- [ ] 读取 `client/.cursor/rules/datetime-display.mdc`（日期时间规则）
- [ ] 找到最相似的参考实现（采购单/订单/角色等）
- [ ] 确认命名约定：
  - 模块名：kebab-case（如 `purchase-order`）
  - 实体名：PascalCase（如 `PurchaseOrder`）
  - 类型命名空间：PascalCase（如 `Api.PurchaseOrder`）
  - URL 常量：SCREAMING_SNAKE_CASE（如 `PURCHASE_ORDER_URLS`）
  - 函数名：camelCase（如 `fetchGetPurchaseOrderList`）

---

## 参考实现路径

**采购单列表（完整 CRUD + 抽屉）**：
- 列表页：`src/pages/(base)/purchase/orders/index.tsx`
- 搜索栏：`src/pages/(base)/purchase/orders/modules/PurchaseOrderSearch.tsx`
- 抽屉：`src/pages/(base)/purchase/orders/modules/PurchaseOrderOperateDrawer.tsx`
- 类型：`src/service/types/purchase-order.d.ts`
- API：`src/service/api/purchase-order.ts`
- URL：`src/service/urls/purchase-order.ts`

**订单列表（复杂筛选 + 行内操作）**：
- 列表页：`src/pages/(base)/orders/list/index.tsx`

**角色管理（简单 CRUD）**：
- 列表页：`src/pages/(base)/system/auth/roles/index.tsx`

---

## 注意事项

1. **强制删除 null 值**：`transformParams` 中必须删除未填写字段，禁止发送 `?status=null`
2. **日期时间约定**：
   - 审计时间（createTime/updateTime）：用 `displayDateTime`（UTC → 本地）
   - 业务日期（orderDate/receiveDate）：用 `displayDate`（不做时区换算）
3. **固定列必须设置 scroll.x**：左右固定列场景必须配置 `scroll: { x: 'max-content' }`
4. **编号列跳转**：主编号列（如 `orderNo`、`purchaseNo`）渲染为链接，点击跳详情
5. **表单 Dayjs 转换**：编辑场景需先调用详情接口，再转换 `string` → `Dayjs`
6. **国际化全覆盖**：所有 UI 文案必须走 `t()`，不得硬编码中文

---

## 执行完毕输出

生成完成后输出：

```
✅ 列表页生成完成

📁 已创建文件：
- src/service/types/<module-name>.d.ts
- src/service/urls/<module-name>.ts
- src/service/api/<module-name>.ts
- src/pages/(base)/<module>/<feature>/index.tsx
- src/pages/(base)/<module>/<feature>/modules/<FeatureName>Search.tsx
[- src/pages/(base)/<module>/<feature>/modules/<FeatureName>OperateDrawer.tsx]

📝 后续步骤：
1. 在 src/locales/langs/ 各语言包中添加文案（见上方提示）
2. 运行 pnpm gen-route 生成路由声明
3. 运行 pnpm typecheck 检查类型
4. 调整列定义、搜索字段、表单字段以匹配实际业务需求
5. 实现抽屉的提交逻辑（useTableOperate 的回调函数）

🔍 参考规范：
- client/AGENT.md
- client/.cursor/rules/crud-list-conventions.mdc
- client/.cursor/rules/datetime-display.mdc
```
