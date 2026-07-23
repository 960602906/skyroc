import { Suspense, lazy } from 'react';

import {
  CrudPageLayout,
  createDefaultPagination,
  createDefaultSearchParams,
  createIndexColumn,
  renderEnableStatus,
  renderUserGender
} from '@/features/crud';
import { TableHeaderOperation, useTable, useTableOperate } from '@/features/table';
import {
  fetchAddUser,
  fetchBatchDeleteUser,
  fetchDeleteUser,
  fetchGetUserDetail,
  fetchGetUserList,
  fetchUpdateUser
} from '@/service/api';

import UserSearch from './modules/UserSearch';

const UserOperateDrawer = lazy(() => import('./modules/UserOperateDrawer'));

const UserManage = () => {
  const { t } = useTranslation();

  const nav = useNavigate();

  const { columnChecks, data, run, searchProps, setColumnChecks, tableProps, tableWrapperRef } = useTable({
    apiFn: fetchGetUserList,
    apiParams: {
      ...createDefaultSearchParams(),
      email: null,
      gender: null,
      nickName: null,
      phone: null,
      status: null,
      username: null
    } satisfies Api.SystemManage.UserSearchParams,
    columns: () => [
      createIndexColumn(t),
      {
        align: 'center',
        dataIndex: 'username',
        key: 'username',
        minWidth: 100,
        title: t('page.manage.user.userName')
      },
      {
        align: 'center',
        dataIndex: 'gender',
        key: 'gender',
        render: (_, record) => renderUserGender(record.gender),
        title: t('page.manage.user.userGender'),
        width: 100
      },
      {
        align: 'center',
        dataIndex: 'nickName',
        key: 'nickName',
        minWidth: 100,
        title: t('page.manage.user.nickName')
      },
      {
        align: 'center',
        dataIndex: 'phone',
        key: 'phone',
        title: t('page.manage.user.userPhone'),
        width: 120
      },
      {
        align: 'center',
        dataIndex: 'email',
        key: 'email',
        minWidth: 200,
        title: t('page.manage.user.userEmail')
      },
      {
        align: 'center',
        dataIndex: 'status',
        key: 'status',
        render: (_, record) => renderEnableStatus(record.status),
        title: t('page.manage.user.userStatus'),
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
              onClick={() => nav(`/system/auth/users/${record.id}`)}
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

  const { checkedRowKeys, generalPopupOperation, handleAdd, handleEdit, onBatchDeleted, onDeleted, rowSelection } =
    useTableOperate(data, run, async (res, type) => {
      if (type === 'add') {
        // add request 调用新增的接口
        await fetchAddUser(res);
      } else {
        // edit request 调用编辑的接口
        await fetchUpdateUser(res);
      }
    });

  async function handleBatchDelete() {
    // request
    await fetchBatchDeleteUser(checkedRowKeys.map(key => key as string));
    onBatchDeleted();
  }

  async function handleDelete(id: string) {
    // request
    await fetchDeleteUser(id);

    onDeleted();
  }

  async function edit(id: string) {
    const userDetail = await fetchGetUserDetail(id);
    handleEdit({ ...(userDetail ?? {}), index: 0 });
  }

  return (
    <CrudPageLayout
      search={<UserSearch {...searchProps} />}
      tableWrapperRef={tableWrapperRef}
      title={t('page.manage.user.title')}
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
            <UserOperateDrawer {...generalPopupOperation} />
          </Suspense>
        </>
      }
    />
  );
};

export default UserManage;
