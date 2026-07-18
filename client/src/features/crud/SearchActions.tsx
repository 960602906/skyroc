import type { ColProps } from 'antd';

import { getSearchActionsSpan } from './search-actions-span';

type SearchActionsProps = {
  onReset: () => void;
  onSearch: () => void;
};

const SearchActions: FC<SearchActionsProps> = memo(({ onReset, onSearch }) => {
  const { t } = useTranslation();

  return (
    <AFlex
      align="center"
      gap={12}
      justify="end"
    >
      <AButton
        icon={<IconIcRoundRefresh />}
        onClick={onReset}
      >
        {t('common.reset')}
      </AButton>
      <AButton
        ghost
        icon={<IconIcRoundSearch />}
        type="primary"
        onClick={onSearch}
      >
        {t('common.search')}
      </AButton>
    </AFlex>
  );
});

type SearchActionsColProps = SearchActionsProps & {
  /** 操作列之前的搜索字段数量。字段默认按 `lg=6` / `md=12` 排布时，用于计算操作列占满行剩余栅格。 */
  fieldCount: number;
  /** 搜索字段 lg 栅格，默认 6 */
  fieldLg?: number;
  /** 搜索字段 md 栅格，默认 12 */
  fieldMd?: number;
} & Omit<ColProps, 'children'>;

/** 搜索表单操作列：按字段数占满当前行剩余宽度，按钮右对齐。对齐参考 UserSearch：第二行剩余半行时用 lg=12 + justify end。 */
export const SearchActionsCol: FC<SearchActionsColProps> = memo(
  ({ fieldCount, fieldLg = 6, fieldMd = 12, onReset, onSearch, ...colProps }) => {
    return (
      <ACol
        lg={getSearchActionsSpan(fieldCount, fieldLg)}
        md={getSearchActionsSpan(fieldCount, fieldMd)}
        span={24}
        {...colProps}
      >
        <AForm.Item className="m-0">
          <SearchActions
            onReset={onReset}
            onSearch={onSearch}
          />
        </AForm.Item>
      </ACol>
    );
  }
);

export default SearchActions;
