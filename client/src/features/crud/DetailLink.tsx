import { useNavigate } from 'react-router-dom';

import { DETAIL_EMPTY } from './display-text';

interface DetailLinkProps {
  /** 显示文本，空值时回退到 `-` */
  text: string | number | null | undefined;
  /** 跳转路径，空值（null/undefined/''）时展示纯文本 */
  to?: string | null;
}

/** 详情字段中的关联实体链接：有 id 渲染为跳转按钮，否则回退纯文本。 */
function DetailLink({ text, to }: DetailLinkProps) {
  const nav = useNavigate();
  const label = text === null || text === undefined || text === '' ? DETAIL_EMPTY : String(text);

  if (!to) {
    return label;
  }

  return (
    <AButton
      className="h-auto p-0 leading-normal"
      size="small"
      type="link"
      onClick={() => nav(to)}
    >
      {label}
    </AButton>
  );
}

export default DetailLink;
