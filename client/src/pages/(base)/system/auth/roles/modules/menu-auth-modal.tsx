import { SimpleScrollbar } from '@sa/materials';

import { flattenLeafRoutes, getBaseChildrenRoutes, getFlatBaseRoutes } from '@/features/router/routes';
import { allRoutes } from '@/router';
import { fetchAssignMenu, fetchGetRoleDetail } from '@/service/api';
import { useMenuList } from '@/service/hooks';

import type { ModulesProps, SimpleRoute } from './type';

const flatRoutes = flattenLeafRoutes(getBaseChildrenRoutes(allRoutes));

/**
 * 递归查找选中节点及其所有父节点
 *
 * @param nodes 当前层级的节点数组
 * @param selectedKeys 已选中的key数组
 * @param parentKeys 当前路径上的父节点keys
 * @returns 所有需要选中的keys（包括选中节点和其父节点）
 */
type TreeNode = { children?: TreeNode[]; key: string };
function findSelectedNodesWithParents(nodes: TreeNode[], selectedKeys: string[], parentKeys: string[] = []): string[] {
  const result = new Set<string>();

  for (const node of nodes) {
    const currentPath = [...parentKeys, node.key];
    const isSelected = selectedKeys.includes(node.key);

    if (isSelected) {
      // 如果当前节点被选中，添加当前节点和所有父节点
      currentPath.forEach(key => result.add(key));
    }

    // 递归处理子节点
    if (node.children && node.children.length > 0) {
      const childResults = findSelectedNodesWithParents(node.children, selectedKeys, currentPath);
      childResults.forEach(key => result.add(key));
    }
  }

  return Array.from(result);
}

/**
 * 查找树中的节点
 *
 * @param nodes 节点数组
 * @param key 要查找的key
 * @returns 找到的节点或null
 */
function findNodeByKey(nodes: TreeNode[], key: string): TreeNode | null {
  for (const node of nodes) {
    if (node.key === key) {
      return node;
    }
    if (node.children && node.children.length > 0) {
      const found = findNodeByKey(node.children, key);
      if (found) {
        return found;
      }
    }
  }
  return null;
}

/**
 * 过滤掉所有有子节点的父节点，只保留叶子节点
 *
 * @param nodes 树节点数组
 * @param selectedKeys 已选中的key数组
 * @returns 过滤后的keys数组（只保留叶子节点）
 */
function filterParentNodes(nodes: TreeNode[], selectedKeys: string[]): string[] {
  return selectedKeys.filter(key => {
    const node = findNodeByKey(nodes, key);
    // 只保留没有子节点的节点（叶子节点）
    return !node || !node.children || node.children.length === 0;
  });
}

function filterAndFlattenRoutes(routes: Api.SystemManage.Menu[]): SimpleRoute[] {
  const result: SimpleRoute[] = [];

  for (const route of routes) {
    const newRoute: SimpleRoute = {
      buttons: route.buttons || [],
      icon: route.icon,
      key: route.id,
      title: route.title
    };

    if (route.children && route.children.length > 0) {
      newRoute.children = filterAndFlattenRoutes(route.children);
    }

    result.push(newRoute);
  }

  return result;
}

const MenuAuthModal: FC<ModulesProps> = memo(({ onClose, open, roleId }) => {
  const { t } = useTranslation();
  const { data: menuList } = useMenuList();
  const title = t('common.edit') + t('page.manage.role.menuAuth');

  const [home, setHome] = useState<string>();

  const [checks, setChecks] = useState<string[]>();

  const data = getFlatBaseRoutes(flatRoutes, t);

  const tree = filterAndFlattenRoutes(menuList?.records || []);

  async function getChecks() {
    // request
    const res = await fetchGetRoleDetail(roleId);
    const menuIds = res?.menu?.map(item => item.id) || [];

    // 回显的时候，如果有父节点和所有子节点都被选中，则过滤掉父节点，只保留子节点
    const filteredMenuIds = filterParentNodes(tree, menuIds);

    setChecks(filteredMenuIds);
  }

  async function handleSubmit() {
    if (!checks || checks.length === 0) {
      window.$message?.warning?.(t('common.pleaseSelect'));
      return;
    }

    // 遍历 tree 结构，如果只选择子节点，则把父节点也选中
    const allSelectedKeys = findSelectedNodesWithParents(tree, checks);

    await fetchAssignMenu(roleId, allSelectedKeys);
    window.$message?.success?.(t('common.modifySuccess'));

    onClose();
  }

  async function init() {
    setHome('/dashboard/overview');

    await getChecks();
  }

  useUpdateEffect(() => {
    if (open) {
      init();
    }
  }, [open]);

  return (
    <AModal
      className="w-480px"
      open={open}
      title={title}
      footer={
        <ASpace className="mt-16px">
          <AButton
            size="small"
            onClick={onClose}
          >
            {t('common.cancel')}
          </AButton>
          <AButton
            size="small"
            type="primary"
            onClick={handleSubmit}
          >
            {t('common.confirm')}
          </AButton>
        </ASpace>
      }
      onCancel={onClose}
    >
      <div className="flex-y-center gap-16px pb-12px">
        <div>{t('page.manage.menu.home')}</div>

        <ASelect
          className="w-240px"
          options={data}
          value={home}
          onChange={setHome}
        />
      </div>

      <SimpleScrollbar className="!h-270px">
        <ATree
          blockNode
          checkable
          multiple
          checkedKeys={checks}
          treeData={tree}
          onCheck={value => setChecks(value as string[])}
        />
      </SimpleScrollbar>
    </AModal>
  );
});

export default MenuAuthModal;
