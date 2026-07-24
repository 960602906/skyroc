import { useQuery } from '@tanstack/react-query';
import { useDebounce } from 'ahooks';
import type { TableColumnsType } from 'antd';
import type { Dayjs } from 'dayjs';
import type { FC } from 'react';
import { Suspense, lazy, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';

import RemoteOptionSelect from '@/components/RemoteOptionSelect';
import { displayDate, displayText } from '@/features/crud';
import { useFormRules } from '@/features/form';
import { fetchGetAfterSaleDetail, fetchGetAfterSaleList, fetchGetAfterSalePickupTasks } from '@/service/api';
import { AfterSaleStatus, PickupTaskStatus } from '@/service/enums';
import { SELECTION_OPTION_RESOURCES, toOptions, useWareOptions } from '@/service/hooks';
import { QUERY_KEYS } from '@/service/keys';

const SalesReturnStockInDetailModal = lazy(() => import('./SalesReturnStockInDetailModal'));

/** 销售退货入库商品行表单值 */
export interface SalesReturnStockInDetailFormValue {
  expireDate?: Dayjs | null | string;
  goodsCode?: string;
  goodsId: string;
  goodsName?: string;
  goodsUnitId: string;
  goodsUnitName?: string;
  id?: null | string;
  /** 来源取货任务主键；手工退货时为空 */
  pickupTaskId?: null | string;
  productDate?: Dayjs | null | string;
  quantity: number;
  remark?: null | string;
  /** 取货任务编号（显示用） */
  taskNo?: string;
  unitPrice: number;
}

/** 销售退货入库表单值 */
export interface SalesReturnStockInFormValue {
  afterSaleId?: null | string;
  /** 售后单号快照（显示用） */
  afterSaleNo?: string;
  customerId: string;
  customerName?: string;
  departmentId?: null | string;
  details: SalesReturnStockInDetailFormValue[];
  id?: string;
  inTime: Dayjs | string;
  remark?: null | string;
  wareId: string;
}

interface SalesReturnStockInOperateFormProps {
  /** 编辑态锁定来源售后与取货任务行 */
  editMode?: boolean;
  form: Page.FormInstance;
}

const SalesReturnStockInOperateForm: FC<SalesReturnStockInOperateFormProps> = ({ editMode = false, form }) => {
  const { t } = useTranslation();
  const { defaultRequiredRule } = useFormRules();
  const { data: wares } = useWareOptions();
  const wareOptions = toOptions(wares);

  const [lineForm] = AForm.useForm<SalesReturnStockInDetailFormValue>();
  const [lineOpen, setLineOpen] = useState(false);
  const [lineOperateType, setLineOperateType] = useState<AntDesign.TableOperateType>('add');
  const [editingIndex, setEditingIndex] = useState<number | null>(null);
  const [afterSaleOpen, setAfterSaleOpen] = useState(false);
  const [afterSaleKeyword, setAfterSaleKeyword] = useState('');
  const [importing, setImporting] = useState(false);

  const debouncedAfterSaleKeyword = useDebounce(afterSaleKeyword, { wait: 300 });
  const watchedDetails = AForm.useWatch('details', form) as SalesReturnStockInDetailFormValue[] | undefined;
  const details = useMemo(() => watchedDetails ?? [], [watchedDetails]);
  const afterSaleId = AForm.useWatch('afterSaleId', form) as string | undefined | null;
  const linkedToAfterSale = Boolean(afterSaleId);

  const { data: afterSaleOptionsData, isFetching: afterSaleLoading } = useQuery({
    enabled: afterSaleOpen,
    queryFn: () =>
      fetchGetAfterSaleList({
        afterStatus: AfterSaleStatus.RETURN_PENDING,
        current: 1,
        keyword: debouncedAfterSaleKeyword || null,
        size: 20
      }),
    queryKey: QUERY_KEYS.AFTER_SALE.OPTIONS(debouncedAfterSaleKeyword)
  });

  const afterSaleOptions = useMemo(() => {
    const records = afterSaleOptionsData?.records ?? [];
    const options = records.map(item => ({
      label: `${item.afterSaleNo} · ${item.customerName}`,
      value: item.id
    }));

    // 编辑回填：当前选中售后可能不在最新搜索结果中
    const currentId = afterSaleId;
    const currentNo = form.getFieldValue('afterSaleNo') as string | undefined;
    if (currentId && currentNo && !options.some(item => item.value === currentId)) {
      options.unshift({
        label: currentNo,
        value: currentId
      });
    }

    return options;
  }, [afterSaleId, afterSaleOptionsData?.records, form]);

  async function handleAfterSaleChange(value: string | undefined) {
    if (!value) {
      form.setFieldsValue({
        afterSaleId: null,
        afterSaleNo: undefined,
        details: []
      });
      return;
    }

    setImporting(true);
    try {
      const detail = await fetchGetAfterSaleDetail(value);
      form.setFieldsValue({
        afterSaleId: value,
        afterSaleNo: detail.afterSaleNo,
        customerId: detail.customerId,
        customerName: detail.customerName,
        details: []
      });
    } finally {
      setImporting(false);
    }
  }

  async function handleImportPickupTasks() {
    if (!afterSaleId) {
      window.$message?.warning(t('page.storage.in.salesReturn.validation.selectAfterSaleFirst'));
      return;
    }

    setImporting(true);
    try {
      const [taskList, afterSale] = await Promise.all([
        fetchGetAfterSalePickupTasks({
          afterSaleId,
          current: 1,
          pickupStatus: PickupTaskStatus.COMPLETED,
          size: 50
        }),
        fetchGetAfterSaleDetail(afterSaleId)
      ]);

      const goodsById = new Map(afterSale.goods.map(item => [item.id, item]));
      const eligible = (taskList.records ?? []).filter(task => !task.stockInOrderId);

      if (eligible.length === 0) {
        window.$message?.warning(t('page.storage.in.salesReturn.validation.noEligiblePickupTasks'));
        return;
      }

      const nextDetails: SalesReturnStockInDetailFormValue[] = [];
      for (const task of eligible) {
        const goods = goodsById.get(task.afterSaleGoodsId);
        if (goods) {
          nextDetails.push({
            goodsCode: goods.goodsCode,
            goodsId: goods.goodsId,
            goodsName: goods.goodsName || task.goodsName,
            goodsUnitId: goods.goodsUnitId,
            goodsUnitName: goods.goodsUnitName || task.goodsUnitName,
            pickupTaskId: task.id,
            quantity: goods.actualRefundQuantity,
            taskNo: task.taskNo,
            unitPrice: goods.unitPrice
          });
        }
      }

      if (nextDetails.length === 0) {
        window.$message?.warning(t('page.storage.in.salesReturn.validation.noEligiblePickupTasks'));
        return;
      }

      form.setFieldsValue({ details: nextDetails });
      form.setFields([{ errors: [], name: 'details' }]);
      window.$message?.success(t('page.storage.in.salesReturn.importSuccess', { count: nextDetails.length }));
    } finally {
      setImporting(false);
    }
  }

  function openAddLine() {
    if (linkedToAfterSale) {
      window.$message?.warning(t('page.storage.in.salesReturn.validation.useImportForAfterSale'));
      return;
    }
    setLineOperateType('add');
    setEditingIndex(null);
    lineForm.resetFields();
    setLineOpen(true);
  }

  function openEditLine(record: SalesReturnStockInDetailFormValue, index: number) {
    setLineOperateType('edit');
    setEditingIndex(index);
    lineForm.setFieldsValue(record);
    setLineOpen(true);
  }

  async function handleLineSubmit() {
    await lineForm.validateFields();
    const allValues = lineForm.getFieldsValue(true) as SalesReturnStockInDetailFormValue;
    const row: SalesReturnStockInDetailFormValue = { ...allValues };
    const nextDetails = [...details];

    if (lineOperateType === 'edit' && editingIndex !== null) {
      nextDetails[editingIndex] = { ...nextDetails[editingIndex], ...row };
    } else {
      nextDetails.push(row);
    }

    form.setFieldsValue({ details: nextDetails });
    form.setFields([{ errors: [], name: 'details' }]);
    setLineOpen(false);
    lineForm.resetFields();
    setEditingIndex(null);
  }

  function handleRemoveLine(index: number) {
    if (linkedToAfterSale || editMode) {
      // 关联售后时不允许增删取货任务行
      if (details[index]?.pickupTaskId) {
        window.$message?.warning(t('page.storage.in.salesReturn.validation.pickupTaskLocked'));
        return;
      }
    }
    const nextDetails = details.filter((_, i) => i !== index);
    form.setFieldsValue({ details: nextDetails });
  }

  const editingFromPickup = editingIndex !== null ? Boolean(details[editingIndex]?.pickupTaskId) : false;

  const columns: TableColumnsType<SalesReturnStockInDetailFormValue> = [
    {
      align: 'center',
      dataIndex: 'taskNo',
      key: 'taskNo',
      render: value => displayText(value),
      title: t('page.storage.in.salesReturn.taskNo'),
      width: 140
    },
    {
      align: 'center',
      dataIndex: 'goodsId',
      ellipsis: true,
      key: 'goodsId',
      render: (_, record) => displayText(record.goodsName || record.goodsId),
      title: t('page.storage.in.salesReturn.goodsName'),
      width: 180
    },
    {
      align: 'center',
      dataIndex: 'goodsCode',
      key: 'goodsCode',
      render: value => displayText(value),
      title: t('page.storage.in.salesReturn.goodsCode'),
      width: 130
    },
    {
      align: 'center',
      dataIndex: 'goodsUnitId',
      key: 'goodsUnitId',
      render: (_, record) => displayText(record.goodsUnitName || record.goodsUnitId),
      title: t('page.storage.in.salesReturn.goodsUnitName'),
      width: 100
    },
    {
      align: 'right',
      dataIndex: 'quantity',
      key: 'quantity',
      render: value => displayText(value),
      title: t('page.storage.in.salesReturn.quantity'),
      width: 110
    },
    {
      align: 'right',
      dataIndex: 'unitPrice',
      key: 'unitPrice',
      render: (value: number) => value?.toFixed(4),
      title: t('page.storage.in.salesReturn.unitPrice'),
      width: 110
    },
    {
      align: 'center',
      dataIndex: 'productDate',
      key: 'productDate',
      render: value => displayDate(value),
      title: t('page.storage.in.salesReturn.productDate'),
      width: 120
    },
    {
      align: 'center',
      dataIndex: 'expireDate',
      key: 'expireDate',
      render: value => displayDate(value),
      title: t('page.storage.in.salesReturn.expireDate'),
      width: 120
    },
    {
      dataIndex: 'remark',
      ellipsis: true,
      key: 'remark',
      render: value => displayText(value),
      title: t('page.storage.in.remark'),
      width: 160
    },
    {
      align: 'center',
      fixed: 'right',
      key: 'operate',
      render: (_, record, index) => (
        <div className="flex-center gap-8px">
          <AButton
            ghost
            size="small"
            type="primary"
            onClick={() => openEditLine(record, index)}
          >
            {t('common.edit')}
          </AButton>
          {!record.pickupTaskId && (
            <APopconfirm
              title={t('common.confirmDelete')}
              onConfirm={() => handleRemoveLine(index)}
            >
              <AButton
                danger
                size="small"
              >
                {t('common.delete')}
              </AButton>
            </APopconfirm>
          )}
        </div>
      ),
      title: t('common.operate'),
      width: 150
    }
  ];

  return (
    <>
      <ACard
        className="card-wrapper"
        title={t('page.storage.in.salesReturn.sectionBasic')}
        variant="borderless"
      >
        <AForm.Item
          hidden
          name="id"
        >
          <AInput />
        </AForm.Item>
        <AForm.Item
          hidden
          name="afterSaleNo"
        >
          <AInput />
        </AForm.Item>
        <AForm.Item
          hidden
          name="customerName"
        >
          <AInput />
        </AForm.Item>

        <ARow gutter={16}>
          <ACol span={8}>
            <AForm.Item
              extra={t('page.storage.in.salesReturn.afterSaleHint')}
              label={t('page.storage.in.salesReturn.afterSale')}
              name="afterSaleId"
            >
              <ASelect
                allowClear
                showSearch
                disabled={editMode}
                filterOption={false}
                loading={afterSaleLoading || importing}
                options={afterSaleOptions}
                placeholder={t('page.storage.in.salesReturn.form.afterSaleId')}
                onChange={value => handleAfterSaleChange(value as string | undefined)}
                onSearch={setAfterSaleKeyword}
                onDropdownVisibleChange={open => {
                  setAfterSaleOpen(open);
                  if (!open) setAfterSaleKeyword('');
                }}
              />
            </AForm.Item>
          </ACol>

          <ACol span={8}>
            <AForm.Item
              label={t('page.storage.in.salesReturn.customer')}
              name="customerId"
              rules={[defaultRequiredRule]}
            >
              <RemoteOptionSelect
                disabled={linkedToAfterSale || editMode}
                placeholder={t('page.storage.in.salesReturn.form.customerId')}
                resource={SELECTION_OPTION_RESOURCES.CUSTOMER}
                onChange={(_value, option) => {
                  const opt = Array.isArray(option) ? option[0] : option;
                  const label = typeof opt?.label === 'string' ? opt.label : '';
                  const [name] = label.split(' · ');
                  form.setFieldValue('customerName', name?.trim() || undefined);
                }}
              />
            </AForm.Item>
          </ACol>

          <ACol span={8}>
            <AForm.Item
              label={t('page.storage.in.wareId')}
              name="wareId"
              rules={[defaultRequiredRule]}
            >
              <ASelect
                allowClear
                options={wareOptions}
                placeholder={t('page.storage.in.form.wareId')}
              />
            </AForm.Item>
          </ACol>

          <ACol span={8}>
            <AForm.Item
              label={t('page.storage.in.inTime')}
              name="inTime"
              rules={[defaultRequiredRule]}
            >
              <ADatePicker
                showTime
                className="w-full"
                placeholder={t('page.storage.in.form.inTime')}
              />
            </AForm.Item>
          </ACol>

          <ACol span={24}>
            <AForm.Item
              className="mb-0"
              label={t('page.storage.in.remark')}
              name="remark"
            >
              <AInput.TextArea
                placeholder={t('page.storage.in.form.remark')}
                rows={2}
              />
            </AForm.Item>
          </ACol>
        </ARow>
      </ACard>

      <ACard
        className="card-wrapper"
        title={t('page.storage.in.salesReturn.sectionDetails')}
        variant="borderless"
        extra={
          <div className="flex gap-8px">
            {linkedToAfterSale && !editMode && (
              <AButton
                loading={importing}
                size="small"
                type="primary"
                onClick={handleImportPickupTasks}
              >
                {t('page.storage.in.salesReturn.importPickupTasks')}
              </AButton>
            )}
            {!linkedToAfterSale && (
              <AButton
                size="small"
                type="primary"
                onClick={openAddLine}
              >
                {t('page.storage.in.salesReturn.addDetail')}
              </AButton>
            )}
          </div>
        }
      >
        <AForm.Item
          hidden
          name="details"
          rules={[
            {
              validator: async (_, value: SalesReturnStockInDetailFormValue[] | undefined) => {
                if (!value?.length) {
                  throw new Error(t('page.storage.in.validation.atLeastOneDetail'));
                }
              }
            }
          ]}
        >
          <AInput />
        </AForm.Item>
        <ATable<SalesReturnStockInDetailFormValue>
          columns={columns}
          dataSource={details}
          pagination={false}
          rowKey={(_, index) => String(index)}
          scroll={{ x: 'max-content' }}
          size="small"
        />
      </ACard>

      <Suspense fallback={null}>
        <SalesReturnStockInDetailModal
          form={lineForm}
          fromPickupTask={editingFromPickup}
          open={lineOpen}
          operateType={lineOperateType}
          onSubmit={handleLineSubmit}
          onClose={() => {
            setLineOpen(false);
            lineForm.resetFields();
            setEditingIndex(null);
          }}
        />
      </Suspense>
    </>
  );
};

export default SalesReturnStockInOperateForm;
