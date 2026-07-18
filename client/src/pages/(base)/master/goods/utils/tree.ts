type GoodsTypeTreeNode = Api.GoodsType.Entity & { children?: GoodsTypeTreeNode[] };

interface GoodsTypeTreeOption {
  children?: GoodsTypeTreeOption[];
  title: string;
  value: string;
}

export function mapGoodsTypeTree(nodes?: GoodsTypeTreeNode[]): GoodsTypeTreeOption[] {
  if (!nodes?.length) {
    return [];
  }

  return nodes.map(node => ({
    children: node.children?.length ? mapGoodsTypeTree(node.children) : undefined,
    title: node.name,
    value: node.id
  }));
}

/** 在分类树中按 id 查找名称，未找到返回 null */
export function findGoodsTypeName(
  nodes: GoodsTypeTreeNode[] | undefined,
  id: string | null | undefined
): string | null {
  if (!nodes?.length || !id) {
    return null;
  }

  for (const node of nodes) {
    if (node.id === id) {
      return node.name;
    }

    const childName = findGoodsTypeName(node.children, id);
    if (childName) {
      return childName;
    }
  }

  return null;
}
