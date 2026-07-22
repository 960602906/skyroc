import { useBoolean, useHookTable } from '@sa/hooks';
import type { TablePaginationConfig, TableProps } from 'antd';
import { Form } from 'antd';

import { getIsMobile } from '@/features/app';
import { parseQuery } from '@/features/router/query';

type TableData = AntDesign.TableData;
type GetTableData<A extends AntDesign.TableApiFn> = AntDesign.GetTableData<A>;

type TableColumn<T> = AntDesign.TableColumn<T>;

type Config<A extends AntDesign.TableApiFn> = AntDesign.AntDesignTableConfig<A>;

const TABLE_LOADING_DELAY_MS = 200;

type CustomTableProps<A extends AntDesign.TableApiFn> = Omit<
  TableProps<AntDesign.TableDataWithIndex<GetTableData<A>>>,
  'loading'
> & {
  loading: NonNullable<TableProps<AntDesign.TableDataWithIndex<GetTableData<A>>>['loading']>;
};

export function useTable<A extends AntDesign.TableApiFn>(config: Config<A>) {
  const isMobile = useAppSelector(getIsMobile);

  const {
    apiFn,
    apiParams,
    columns: columnsFactory,
    immediate,
    isChangeURL = true,
    onChange: onChangeCallback,
    pagination: paginationConfig,
    rowKey = 'id',
    scroll: scrollFromConfig,
    transformParams,
    ...rest
  } = config;

  const [form] = Form.useForm<AntDesign.AntDesignTableConfig<A>['apiParams']>();

  const { search } = useLocation();

  const query = parseQuery(search) as unknown as Parameters<A>[0];

  const {
    columnChecks,
    columns,
    data,
    empty,
    loading,
    pageNum,
    pageSize,
    resetSearchParams,
    searchParams,
    setColumnChecks,
    total,
    updateSearchParams
  } = useHookTable<A, GetTableData<A>, TableColumn<AntDesign.TableDataWithIndex<GetTableData<A>>>>({
    apiFn,
    apiParams: { ...apiParams, ...query },
    columns: columnsFactory,
    getColumnChecks: cols => {
      const checks: AntDesign.TableColumnCheck[] = [];

      cols.forEach(column => {
        if (column.key) {
          checks.push({
            checked: true,
            key: column.key as string,
            title: column.title as string
          });
        }
      });

      return checks;
    },
    getColumns: (cols, checks) => {
      const columnMap = new Map<string, TableColumn<AntDesign.TableDataWithIndex<GetTableData<A>>>>();

      cols.forEach(column => {
        if (column.key) {
          columnMap.set(column.key as string, column);
        }
      });

      const filteredColumns = checks.filter(item => item.checked).map(check => columnMap.get(check.key));

      return filteredColumns as TableColumn<AntDesign.TableDataWithIndex<GetTableData<A>>>[];
    },
    immediate,
    isChangeURL,
    transformer: res => {
      const { current = 1, records = [], size = 10, total: totalNum = 0 } = res || {};

      const recordsWithIndex = records.map((item, index) => {
        return {
          ...item,
          index: (current - 1) * size + index + 1
        };
      });

      return {
        data: recordsWithIndex,
        pageNum: current,
        pageSize: size,
        total: totalNum
      };
    },
    transformParams
  });

  // this is for mobile, if the system does not support mobile, you can use `pagination` directly
  const pagination: TablePaginationConfig = {
    current: pageNum,
    pageSize,
    pageSizeOptions: ['10', '15', '20', '25', '30'],
    showSizeChanger: true,
    simple: isMobile,
    total,
    ...paginationConfig
  };

  const tableWrapperRef = useRef<HTMLDivElement>(null);
  const size = useSize(tableWrapperRef);

  /** 仅按容器高度计算纵向滚动；不自动算 scroll.x，避免 fixed 布局压扁列宽 */
  const scrollConfig = useMemo(() => {
    const height = size?.height;
    const y = height ? height - 160 : undefined;

    return {
      ...scrollFromConfig,
      y: scrollFromConfig?.y ?? y
    };
  }, [scrollFromConfig, size?.height]);

  function reset() {
    form.setFieldsValue(apiParams as NonNullable<Parameters<A>[0]>);

    resetSearchParams();
  }

  async function run(isResetCurrent: boolean = true) {
    const res = await form.validateFields();

    if (res) {
      if (isResetCurrent) {
        const { current = 1, ...other } = res;
        updateSearchParams({ current, ...other });
      } else {
        updateSearchParams(res);
      }
    }
  }

  function handleChange(...args: AntDesign.TableOnChange) {
    const [paginationContext, ...otherParams] = args;

    let other: Parameters<A>[0] = {
      current: paginationContext.current,
      size: paginationContext.pageSize
    } as Parameters<A>[0];

    if (onChangeCallback) {
      const params = onChangeCallback(paginationContext, ...otherParams);
      if (params) {
        other = params;
      }
    }

    updateSearchParams(other);
  }

  return {
    columnChecks,
    data,
    empty,
    form,
    run,
    scrollConfig,
    searchParams,
    searchProps: {
      form,
      reset,
      search: run,
      searchParams: searchParams as NonNullable<Parameters<A>[0]>
    },
    setColumnChecks,
    tableProps: {
      columns,
      dataSource: data,
      // 短请求不展示遮罩，避免表格透明度动画快速反转造成视觉抖动。
      loading: { delay: TABLE_LOADING_DELAY_MS, spinning: loading },
      onChange: handleChange,
      pagination,
      rowKey,
      ...rest,
      // 放在 rest 之后，避免被页面配置覆盖
      scroll: scrollConfig
    } as CustomTableProps<A>,
    tableWrapperRef
  };
}

export function useTableOperate<T extends TableData = TableData>(
  data: T[],
  getData: (isResetCurrent?: boolean) => Promise<void>,
  executeResActions: (res: T, operateType: AntDesign.TableOperateType) => void
) {
  const { bool: drawerVisible, setFalse: closeDrawer, setTrue: openDrawer } = useBoolean();

  const { t } = useTranslation();

  const [operateType, setOperateType] = useState<AntDesign.TableOperateType>('add');

  const [form] = Form.useForm<T>();

  function handleAdd() {
    setOperateType('add');
    openDrawer();
  }

  /** the editing row data */
  const [editingData, setEditingData] = useState<T>();

  function handleEdit(idOrData: T['id'] | T) {
    if (typeof idOrData === 'object') {
      setEditingData(idOrData);
      form.setFieldsValue(idOrData);
    } else {
      const findItem = data.find(item => item.id === idOrData);
      if (findItem) {
        setEditingData(findItem);
        form.setFieldsValue(findItem);
      }
    }

    setOperateType('edit');
    openDrawer();
  }

  /** the checked row keys of table */
  const [checkedRowKeys, setCheckedRowKeys] = useState<React.Key[]>([]);

  function onSelectChange(keys: React.Key[]) {
    setCheckedRowKeys(keys);
  }

  const rowSelection: TableProps<T>['rowSelection'] = {
    columnWidth: 48,
    fixed: true,
    onChange: onSelectChange,
    selectedRowKeys: checkedRowKeys,
    type: 'checkbox'
  };

  function onClose() {
    closeDrawer();

    form.resetFields();
  }

  /** the hook after the batch delete operation is completed */
  async function onBatchDeleted() {
    window.$message?.success(t('common.deleteSuccess'));
    setCheckedRowKeys([]);

    await getData(false);
  }

  /** the hook after the delete operation is completed */
  async function onDeleted() {
    window.$message?.success(t('common.deleteSuccess'));

    await getData(false);
  }

  async function handleSubmit() {
    const res = await form.validateFields();

    // request
    await executeResActions(res, operateType);

    window.$message?.success(t('common.updateSuccess'));

    onClose();
    getData();
  }

  return {
    checkedRowKeys,
    closeDrawer,
    drawerVisible,
    editingData,
    generalPopupOperation: {
      form,
      handleSubmit,
      onClose,
      open: drawerVisible,
      operateType
    },
    handleAdd,
    handleEdit,
    onBatchDeleted,
    onDeleted,
    onSelectChange,
    openDrawer,
    operateType,
    rowSelection
  };
}

/** 表格容器尺寸与滚动配置（默认仅纵向；需要横向时自行传 scrollX） */
export function useTableScroll(scrollX?: number) {
  const tableWrapperRef = useRef<HTMLDivElement>(null);

  const size = useSize(tableWrapperRef);

  function getTableScrollY() {
    const height = size?.height;

    if (!height) return undefined;

    return height - 160;
  }

  const scrollConfig = {
    x: scrollX,
    y: getTableScrollY()
  };

  return {
    scrollConfig,
    tableWrapperRef
  };
}
