import { Suspense } from 'react';

import {
  CrudPageLayout,
  createDefaultPagination,
  createDefaultSearchParams,
  createIndexColumn,
  renderEnableStatus
} from '@/features/crud';
import { TableHeaderOperation, useTable, useTableOperate } from '@/features/table';
import { fetchAddRole, fetchBatchDeleteRole, fetchDeleteRole, fetchGetRoleList, fetchUpdateRole } from '@/service/api';

import RoleSearch from './modules/role-search';

const RoleOperateDrawer = lazy(() => import('./modules/role-operate-drawer'));

const Role = () => {
  const { t } = useTranslation();

  const nav = useNavigate();

  const { columnChecks, data, run, searchProps, setColumnChecks, tableProps, tableWrapperRef } = useTable({
    apiFn: fetchGetRoleList,
    apiParams: {
      ...createDefaultSearchParams(),
      code: null,
      name: null,
      status: null
    } satisfies Api.SystemManage.RoleSearchParams,
    columns: () => [
      createIndexColumn(t),
      {
        align: 'center',
        dataIndex: 'name',
        key: 'name',
        title: t('page.manage.role.roleName'),
        width: 240
      },
      {
        align: 'center',
        dataIndex: 'code',
        key: 'code',
        minWidth: 120,
        title: t('page.manage.role.roleCode')
      },
      {
        dataIndex: 'desc',
        key: 'desc',
        minWidth: 120,
        title: t('page.manage.role.roleDesc')
      },
      {
        align: 'center',
        dataIndex: 'status',
        key: 'status',
        render: (_, record) => renderEnableStatus(record.status),
        title: t('page.manage.role.roleStatus'),
        width: 100
      },
      {
        align: 'center',
        key: 'operate',
        render: (_, record) => (
          <div className="flex-center gap-8px">
            <AButton
              ghost
              size="small"
              type="primary"
              onClick={() => edit(record.id)}
            >
              {t('common.edit')}
            </AButton>
            <AButton
              size="small"
              onClick={() => nav(`/system/auth/roles/${record.id}/${record.name}/${record.status}`)}
            >
              详情
            </AButton>
            <APopconfirm
              title={t('common.confirmDelete')}
              onConfirm={() => handleDelete(record.id)}
            >
              <AButton
                danger
                size="small"
              >
                {t('common.delete')}
              </AButton>
            </APopconfirm>
          </div>
        ),
        title: t('common.operate'),
        width: 195
      }
    ],
    pagination: createDefaultPagination(),
    scroll: { x: 'max-content' }
  });

  const {
    checkedRowKeys,
    editingData,
    generalPopupOperation,
    handleAdd,
    handleEdit,
    onBatchDeleted,
    onDeleted,
    rowSelection
  } = useTableOperate(data, run, async (res, type) => {
    if (type === 'add') {
      // add request 调用新增的接口
      await fetchAddRole(res);
    } else {
      // edit request 调用编辑的接口
      await fetchUpdateRole(res);
    }
  });

  async function handleBatchDelete() {
    // request
    await fetchBatchDeleteRole(checkedRowKeys.map(key => key) as string[]);
    onBatchDeleted();
  }

  async function handleDelete(id: string) {
    // request
    await fetchDeleteRole(id);

    onDeleted();
  }

  function edit(id: string) {
    handleEdit(id);
  }

  return (
    <CrudPageLayout
      search={<RoleSearch {...searchProps} />}
      tableWrapperRef={tableWrapperRef}
      title={t('page.manage.role.title')}
      extra={
        <TableHeaderOperation
          add={handleAdd}
          columns={columnChecks}
          disabledDelete={checkedRowKeys.length === 0}
          loading={tableProps.loading}
          refresh={run}
          setColumnChecks={setColumnChecks}
          onDelete={handleBatchDelete}
        />
      }
      table={
        <>
          <ATable
            rowSelection={rowSelection}
            size="small"
            {...tableProps}
          />

          <Suspense>
            <RoleOperateDrawer
              {...generalPopupOperation}
              rowId={editingData?.id || ''}
            />
          </Suspense>
        </>
      }
    />
  );
};

export default Role;
