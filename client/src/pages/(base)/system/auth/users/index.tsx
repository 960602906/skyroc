import { Suspense, lazy } from 'react';

import { renderEnableStatus, renderUserGender } from '@/features/crud';
import { TableHeaderOperation, useTable, useTableOperate, useTableScroll } from '@/features/table';
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

  const { scrollConfig, tableWrapperRef } = useTableScroll();

  const nav = useNavigate();

  const isMobile = useMobile();

  const { columnChecks, data, run, searchProps, setColumnChecks, tableProps } = useTable({
    apiFn: fetchGetUserList,
    apiParams: {
      current: 1,
      email: null,
      gender: null,
      nickName: null,
      phone: null,
      size: 10,
      // if you want to use the searchParams in Form, you need to define the following properties, and the value is null
      // the value can not be undefined, otherwise the property in Form will not be reactive
      status: null,
      username: null
    },
    columns: () => [
      {
        align: 'center',
        dataIndex: 'index',
        key: 'index',
        title: t('common.index'),
        width: 64
      },
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
    pagination: {
      showQuickJumper: true
    }
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
    <div className="h-full min-h-500px flex-col-stretch gap-16px overflow-hidden lt-sm:overflow-auto">
      <ACollapse
        bordered={false}
        className="card-wrapper"
        defaultActiveKey={isMobile ? undefined : '1'}
        items={[
          {
            children: <UserSearch {...searchProps} />,
            key: '1',
            label: t('common.search')
          }
        ]}
      />

      <ACard
        className="flex-col-stretch card-wrapper sm:flex-1-hidden"
        ref={tableWrapperRef}
        title={t('page.manage.user.title')}
        variant="borderless"
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
      >
        <ATable
          rowSelection={rowSelection}
          scroll={scrollConfig}
          size="small"
          {...tableProps}
        />
        <Suspense>
          <UserOperateDrawer {...generalPopupOperation} />
        </Suspense>
      </ACard>
    </div>
  );
};

export default UserManage;
