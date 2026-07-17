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
