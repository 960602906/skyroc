import { Suspense } from 'react';

import { enableStatusRecord, menuTypeRecord } from '@/constants/business';
import { ATG_MAP, YesOrNo_Map, yesOrNoRecord } from '@/constants/common';
import { TableHeaderOperation, useTable, useTableOperate, useTableScroll } from '@/features/table';
import { pages } from '@/router/elegant/imports';
import {
  fetchAddMenu,
  fetchBatchDeleteMenu,
  fetchDeleteMenu,
  fetchGetMenuDetail,
  fetchGetMenuList,
  fetchUpdateMenu
} from '@/service/api';
import { IconType, MenuType } from '@/service/enums';

import MenuOperateModal from './modules/menu-operate-modal';
import type { OperateType } from './modules/shared';
import { createDefaultModel, flattenMenu, getPathParamFromRoutePath } from './modules/shared';

const Menu = () => {
  const { t } = useTranslation();

  const { scrollConfig, tableWrapperRef } = useTableScroll();

  const allPages = Object.keys(pages);

  const { columnChecks, data, run, setColumnChecks, tableProps } = useTable({
    apiFn: fetchGetMenuList,
    columns: () => [
      {
        align: 'center',
        dataIndex: 'index',
        key: 'index',
        title: t('page.manage.menu.index')
      },
      {
        align: 'center',
        key: 'menuType',
        render: (_, record) => {
          if (record.status === null) {
            return null;
          }

          const tagMap: Record<Api.SystemManage.MenuType, string> = {
            1: 'default',
            2: 'processing'
          };

          const label = t(menuTypeRecord[record.menuType]);
          return <ATag color={tagMap[record.menuType]}>{label}</ATag>;
        },
        title: t('page.manage.menu.menuType'),
        width: 80
      },
      {
        align: 'center',
        key: 'title',
        minWidth: 120,
        render: (_, record) => {
          const { i18nKey, title } = record;

          const label = i18nKey ? t(i18nKey) : title;

          return <span>{label}</span>;
        },
        title: t('page.manage.menu.menuName')
      },
      {
        align: 'center',
        key: 'icon',
        render: (_, record) => {
          const icon = record.iconType === IconType.ICONIFY ? record.icon : undefined;

          const localIcon = record.iconType === IconType.LOCAL ? record.icon : undefined;

          return (
            <div className="flex-center">
              <SvgIcon
                className="text-icon"
                icon={icon}
                localIcon={localIcon}
              />
            </div>
          );
        },
        title: t('page.manage.menu.icon'),
        width: 60
      },
      {
        align: 'center',
        dataIndex: 'name',
        key: 'name',
        minWidth: 120,
        title: t('page.manage.menu.routeName')
      },
      {
        align: 'center',
        dataIndex: 'path',
        key: 'path',
        minWidth: 120,
        title: t('page.manage.menu.routePath')
      },
      {
        align: 'center',
        dataIndex: 'status',
        key: 'status',
        render: (_, record) => {
          if (record.status === null) {
            return null;
          }

          const label = t(enableStatusRecord[record.status]);

          return <ATag color={ATG_MAP[record.status]}>{label}</ATag>;
        },
        title: t('page.manage.menu.menuStatus'),
        width: 80
      },
      {
        align: 'center',
        dataIndex: 'hideInMenu',
        key: 'hideInMenu',
        render: (_, record) => {
          const hide: CommonType.YesOrNo = record.hideInMenu ? 'Y' : 'N';

          const label = t(yesOrNoRecord[hide]);

          return <ATag color={YesOrNo_Map[hide]}>{label}</ATag>;
        },
        title: t('page.manage.menu.hideInMenu'),
        width: 80
      },

      {
        align: 'center',
        dataIndex: 'order',
        key: 'order',
        title: t('page.manage.menu.order'),
        width: 60
      },
      {
        align: 'center',
        key: 'operate',
        render: (_, record, index) => (
          <div className="flex-center justify-end gap-8px">
            {record.menuType === MenuType.DIRECTORY && (
              <AButton
                ghost
                size="small"
                type="primary"
                onClick={() => handleAddChildMenu(record.id)}
              >
                {t('page.manage.menu.addChildMenu')}
              </AButton>
            )}
            <AButton
              size="small"
              onClick={() => edit(record, index)}
            >
              {t('common.edit')}
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
      hideOnSinglePage: true
    }
  });

  const menuList = useMemo(() => flattenMenu(data), [data]);

  const {
    checkedRowKeys,
    generalPopupOperation,
    handleAdd,
    handleEdit,
    onBatchDeleted,
    onDeleted,
    openDrawer,
    rowSelection
  } = useTableOperate(data, run, async (res, type) => {
    if (type === 'add') {
      // add request 调用新增的接口
      await fetchAddMenu(res);
    } else {
      // edit request 调用编辑的接口
      await fetchUpdateMenu(res);
    }
  });

  const [operateType, setOperateType] = useState<OperateType>('add');

  async function handleBatchDelete() {
    // request
    await fetchBatchDeleteMenu(checkedRowKeys.map(key => key) as string[]);
    onBatchDeleted();
  }

  function onAdd() {
    setOperateType('add');

    handleAdd();
  }

  async function handleDelete(id: string) {
    // request
    await fetchDeleteMenu(id);
    onDeleted();
  }

  async function edit(item: Api.SystemManage.Menu, index: number) {
    const menuDetail = await fetchGetMenuDetail(item.id);
    const { param, path } = getPathParamFromRoutePath(menuDetail.path);

    const itemData = Object.assign(createDefaultModel(), menuDetail, {
      index,
      path,
      pathParam: param
    });
    handleEdit(itemData);
    setOperateType('edit');
  }

  function handleAddChildMenu(id: string) {
    generalPopupOperation.form.setFieldsValue(Object.assign(createDefaultModel(), { parentId: id }));

    setOperateType('addChild');

    openDrawer();
  }

  return (
    <div className="h-full min-h-500px flex-col-stretch gap-16px overflow-hidden lt-sm:overflow-auto">
      <ACard
        className="flex-col-stretch sm:flex-1-hidden card-wrapper"
        ref={tableWrapperRef}
        title={t('page.manage.role.title')}
        variant="borderless"
        extra={
          <TableHeaderOperation
            add={onAdd}
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
          <MenuOperateModal
            {...Object.assign(generalPopupOperation, { operateType })}
            allPages={allPages || []}
            menuList={menuList || []}
          />
        </Suspense>
      </ACard>
    </div>
  );
};

export default Menu;
