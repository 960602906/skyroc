declare namespace AntDesign {
  // ==================== 基础类型（Ant Design 原生） ====================

  type TableColumnType<T> = import('antd').TableColumnType<T>;
  type TableColumnGroupType<T> = import('antd').TableColumnGroupType<T>;
  type TablePaginationConfig = import('antd').TablePaginationConfig;
  type TableProps = import('antd').TableProps;

  // ==================== 列相关类型 ====================

  type TableColumnCheck = import('@sa/hooks').TableColumnCheck;

  /**
   * 自定义列的 key 枚举
   *
   * 如需新增自定义列，在此联合类型中添加对应 key
   */
  type CustomColumnKey = 'operate';

  type SetTableColumnKey<C, T> = Omit<C, 'key'> & { key?: keyof T | CustomColumnKey };

  type TableColumn<T> = SetTableColumnKey<TableColumnType<T>, T> | SetTableColumnKey<TableColumnGroupType<T>, T>;

  // ==================== 数据与 API 类型 ====================

  type TableDataWithIndex<T> = import('@sa/hooks').TableDataWithIndex<T>;
  type FlatResponseData<T> = import('@sa/axios').FlatResponseData<T>;

  type TableData = Api.Common.CommonRecord<object>;

  type TableApiFn<T = any, R = Api.Common.CommonSearchParams> = (
    params: R
  ) => Promise<App.Service.Response<Api.Common.PaginatingQueryRecord<T>>['data']>;

  type GetTableData<A extends TableApiFn> = A extends TableApiFn<infer T> ? T : never;

  // ==================== 表格配置类型 ====================

  /**
   * 表格操作类型
   *
   * - add: 新增行
   * - edit: 编辑行
   */
  type TableOperateType = 'add' | 'edit';

  type TableOnChange = Parameters<NonNullable<TableProps['onChange']>>;

  type AntDesignTableConfig<A extends TableApiFn> = Pick<
    import('@sa/hooks').TableConfig<A, GetTableData<A>, TableColumn<TableDataWithIndex<GetTableData<A>>>>,
    'apiFn' | 'apiParams' | 'columns' | 'immediate' | 'isChangeURL' | 'transformParams'
  > & {
    onChange?: (
      ...args: TableOnChange
    ) =>
      | undefined
      | import('@sa/hooks').TableConfig<
          A,
          GetTableData<A>,
          TableColumn<TableDataWithIndex<GetTableData<A>>>
        >['apiParams'];
    rowKey?: keyof GetTableData<A> | ((record: GetTableData<A>) => string | number);
  } & Omit<TableProps, 'columns' | 'dataSource' | 'loading' | 'onChange' | 'rowKey'>;
}
