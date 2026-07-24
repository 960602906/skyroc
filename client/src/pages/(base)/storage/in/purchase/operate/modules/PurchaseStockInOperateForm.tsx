import type { TableColumnsType } from 'antd';
import type { Dayjs } from 'dayjs';
import type { FC } from 'react';
import { Suspense, lazy, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';

import RemoteOptionSelect from '@/components/RemoteOptionSelect';
import { PurchasePatternSelect, displayDate, displayText } from '@/features/crud';
import { useFormRules } from '@/features/form';
import { PurchasePattern } from '@/service/enums';
import { SELECTION_OPTION_RESOURCES, toOptions, usePurchaserOptions, useWareOptions } from '@/service/hooks';

const PurchaseStockInDetailModal = lazy(() => import('./PurchaseStockInDetailModal'));

/** 采购入库商品行表单值 */
export interface PurchaseStockInDetailFormValue {
  /** 商品到期日期，仅记录自然日；无保质期或未知时为空（DateOnly 格式：yyyy-MM-dd） */
  expireDate?: Dayjs | null | string;
  /** 商品编码（显示用，不提交） */
  goodsCode?: string;
  /** 入库商品主键 */
  goodsId: string;
  /** 商品名称（显示用，不提交） */
  goodsName?: string;
  /** 入库计量单位主键 */
  goodsUnitId: string;
  /** 单位名称（显示用，不提交） */
  goodsUnitName?: string;
  /** 已存在的入库商品行主键；为空表示新增商品行 */
  id?: null | string;
  /** 商品生产日期，仅记录自然日；未知时为空（DateOnly 格式：yyyy-MM-dd） */
  productDate?: Dayjs | null | string;
  /** 来源采购单商品明细主键；仅采购入库回填，用于追溯到货来源 */
  purchaseOrderDetailId?: null | string;
  /** 按入库单位计量的入库数量 */
  quantity: number;
  /** 当前入库商品行的业务备注 */
  remark?: null | string;
  /** 入库单价，按系统业务币种和入库单位计量 */
  unitPrice: number;
}

/** 采购入库表单值 */
export interface PurchaseStockInFormValue {
  /** 发起入库业务的部门主键 */
  departmentId?: null | string;
  /** 采购入库商品行，至少包含一项 */
  details: PurchaseStockInDetailFormValue[];
  /** 预计到货时间（UTC）；尚未确认时可为空 */
  expectedArrivalTime?: Dayjs | null | string;
  /** 待编辑的采购入库单主键 */
  id?: string;
  /** 计划或实际入库时间（UTC） */
  inTime: Dayjs | string;
  /** 来源采购单主键；用于回填供应商、采购员和采购模式并支持追溯 */
  purchaseOrderId?: null | string;
  /** 采购模式：供应商直供或市场自采 */
  purchasePattern: Api.StockIn.PurchasePattern;
  /** 负责采购到货的采购员主键 */
  purchaserId?: null | string;
  /** 入库单级业务备注 */
  remark?: null | string;
  /** 供货供应商主键；供应商直供采购入库时必填 */
  supplierId?: null | string;
  /** 接收入库商品的仓库主键 */
  wareId: string;
}

interface PurchaseStockInOperateFormProps {
  form: Page.FormInstance;
}

const PurchaseStockInOperateForm: FC<PurchaseStockInOperateFormProps> = ({ form }) => {
  const { t } = useTranslation();
  const { defaultRequiredRule } = useFormRules();
  const { data: wares } = useWareOptions();
  const { data: purchasers } = usePurchaserOptions();
  const wareOptions = toOptions(wares);
  const purchaserOptions = toOptions(purchasers);

  const [lineForm] = AForm.useForm<PurchaseStockInDetailFormValue>();
  const [lineOpen, setLineOpen] = useState(false);
  const [lineOperateType, setLineOperateType] = useState<AntDesign.TableOperateType>('add');
  const [editingIndex, setEditingIndex] = useState<number | null>(null);

  const watchedDetails = AForm.useWatch('details', form) as PurchaseStockInDetailFormValue[] | undefined;
  const details = useMemo(() => watchedDetails ?? [], [watchedDetails]);
  const purchasePattern = AForm.useWatch('purchasePattern', form) as Api.StockIn.PurchasePattern | undefined;
  const supplierRequired = purchasePattern === PurchasePattern.SUPPLIER_DIRECT;

  function openAddLine() {
    setLineOperateType('add');
    setEditingIndex(null);
    lineForm.resetFields();
    setLineOpen(true);
  }

  function openEditLine(record: PurchaseStockInDetailFormValue, index: number) {
    setLineOperateType('edit');
    setEditingIndex(index);
    lineForm.setFieldsValue(record);
    setLineOpen(true);
  }

  async function handleLineSubmit() {
    await lineForm.validateFields();
    // getFieldsValue(true) 包含隐藏字段（goodsName / goodsCode / goodsUnitName / id）
    const allValues = lineForm.getFieldsValue(true) as PurchaseStockInDetailFormValue;
    const row: PurchaseStockInDetailFormValue = { ...allValues };
    const nextDetails = [...details];

    if (lineOperateType === 'edit' && editingIndex !== null) {
      nextDetails[editingIndex] = { ...nextDetails[editingIndex], ...row };
    } else {
      nextDetails.push(row);
    }

    // eslint-disable-next-line react/prop-types
    form.setFieldsValue({ details: nextDetails });
    // eslint-disable-next-line react/prop-types
    form.setFields([{ errors: [], name: 'details' }]);
    setLineOpen(false);
    lineForm.resetFields();
    setEditingIndex(null);
  }

  function handleRemoveLine(index: number) {
    const nextDetails = details.filter((_, i) => i !== index);
    // eslint-disable-next-line react/prop-types
    form.setFieldsValue({ details: nextDetails });
  }

  const columns: TableColumnsType<PurchaseStockInDetailFormValue> = [
    {
      align: 'center',
      dataIndex: 'goodsId',
      ellipsis: true,
      key: 'goodsId',
      render: (_, record) => displayText(record.goodsName || record.goodsId),
      title: t('page.storage.in.purchase.goodsName'),
      width: 180
    },
    {
      align: 'center',
      dataIndex: 'goodsCode',
      key: 'goodsCode',
      render: value => displayText(value),
      title: t('page.storage.in.purchase.goodsCode'),
      width: 130
    },
    {
      align: 'center',
      dataIndex: 'goodsUnitId',
      key: 'goodsUnitId',
      render: (_, record) => displayText(record.goodsUnitName || record.goodsUnitId),
      title: t('page.storage.in.purchase.goodsUnitName'),
      width: 100
    },
    {
      align: 'right',
      dataIndex: 'quantity',
      key: 'quantity',
      render: value => displayText(value),
      title: t('page.storage.in.purchase.quantity'),
      width: 110
    },
    {
      align: 'right',
      dataIndex: 'unitPrice',
      key: 'unitPrice',
      render: (value: number) => value?.toFixed(4),
      title: t('page.storage.in.purchase.unitPrice'),
      width: 110
    },
    {
      align: 'center',
      dataIndex: 'productDate',
      key: 'productDate',
      render: value => displayDate(value),
      title: t('page.storage.in.purchase.productDate'),
      width: 120
    },
    {
      align: 'center',
      dataIndex: 'expireDate',
      key: 'expireDate',
      render: value => displayDate(value),
      title: t('page.storage.in.purchase.expireDate'),
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
        </div>
      ),
      title: t('common.operate'),
      width: 150
    }
  ];

  return (
    <>
      {/* 基本信息卡片 */}
      <ACard
        className="card-wrapper"
        title={t('page.storage.in.purchase.sectionBasic')}
        variant="borderless"
      >
        <AForm.Item
          hidden
          name="id"
        >
          <AInput />
        </AForm.Item>

        <ARow gutter={16}>
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
              label={t('page.storage.in.purchasePattern')}
              name="purchasePattern"
              rules={[defaultRequiredRule]}
            >
              <PurchasePatternSelect allowClear={false} />
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

          <ACol span={8}>
            <AForm.Item
              label={t('page.storage.in.supplier')}
              name="supplierId"
              rules={supplierRequired ? [defaultRequiredRule] : undefined}
            >
              <RemoteOptionSelect
                allowClear
                placeholder={t('page.storage.in.form.supplierId')}
                resource={SELECTION_OPTION_RESOURCES.SUPPLIER}
              />
            </AForm.Item>
          </ACol>

          <ACol span={8}>
            <AForm.Item
              label={t('page.storage.in.purchaser')}
              name="purchaserId"
            >
              <ASelect
                allowClear
                options={purchaserOptions}
                placeholder={t('page.storage.in.form.purchaserId')}
              />
            </AForm.Item>
          </ACol>

          <ACol span={8}>
            <AForm.Item
              label={t('page.storage.in.expectedArrivalTime')}
              name="expectedArrivalTime"
            >
              <ADatePicker
                showTime
                className="w-full"
                placeholder={t('page.storage.in.form.expectedArrivalTime')}
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

      {/* 商品明细卡片 */}
      <ACard
        className="card-wrapper"
        title={t('page.storage.in.purchase.sectionDetails')}
        variant="borderless"
        extra={
          <AButton
            size="small"
            type="primary"
            onClick={openAddLine}
          >
            {t('page.storage.in.purchase.addDetail')}
          </AButton>
        }
      >
        {/* 隐藏的 Form.Item 承载 details 数组，用于必填校验 */}
        <AForm.Item
          hidden
          name="details"
          rules={[
            {
              validator: async (_, value: PurchaseStockInDetailFormValue[] | undefined) => {
                if (!value?.length) {
                  throw new Error(t('page.storage.in.validation.atLeastOneDetail'));
                }
              }
            }
          ]}
        >
          <AInput />
        </AForm.Item>
        <ATable<PurchaseStockInDetailFormValue>
          columns={columns}
          dataSource={details}
          pagination={false}
          rowKey={(_, index) => String(index)}
          scroll={{ x: 'max-content' }}
          size="small"
        />
      </ACard>

      <Suspense fallback={null}>
        <PurchaseStockInDetailModal
          form={lineForm}
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

export default PurchaseStockInOperateForm;
