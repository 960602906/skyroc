import type { TableColumnsType } from 'antd';
import type { Dayjs } from 'dayjs';
import type { FC } from 'react';
import { Suspense, lazy, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { displayDate, displayText } from '@/features/crud';
import { useFormRules } from '@/features/form';
import { toOptions, useWareOptions } from '@/service/hooks';

const OtherStockInDetailModal = lazy(() => import('./OtherStockInDetailModal'));

/** 其他入库商品行表单值 */
export interface OtherStockInDetailFormValue {
  /** 商品到期日期，仅记录自然日；无保质期或未知时为空 */
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
  /** 商品生产日期，仅记录自然日；未知时为空 */
  productDate?: Dayjs | null | string;
  /** 按入库单位计量的入库数量 */
  quantity: number;
  /** 当前入库商品行的业务备注 */
  remark?: null | string;
  /** 入库单价，按系统业务币种和入库单位计量 */
  unitPrice: number;
}

/** 其他入库表单值 */
export interface OtherStockInFormValue {
  /** 发起入库业务的部门主键 */
  departmentId?: null | string;
  /** 其他入库商品行，至少包含一项 */
  details: OtherStockInDetailFormValue[];
  /** 待编辑的其他入库单主键 */
  id?: string;
  /** 计划或实际入库时间（UTC） */
  inTime: Dayjs | string;
  /** 入库单级业务备注 */
  remark?: null | string;
  /** 接收入库商品的仓库主键 */
  wareId: string;
}

interface OtherStockInOperateFormProps {
  form: Page.FormInstance;
}

const OtherStockInOperateForm: FC<OtherStockInOperateFormProps> = ({ form }) => {
  const { t } = useTranslation();
  const { defaultRequiredRule } = useFormRules();
  const { data: wares } = useWareOptions();
  const wareOptions = toOptions(wares);

  const [lineForm] = AForm.useForm<OtherStockInDetailFormValue>();
  const [lineOpen, setLineOpen] = useState(false);
  const [lineOperateType, setLineOperateType] = useState<AntDesign.TableOperateType>('add');
  const [editingIndex, setEditingIndex] = useState<number | null>(null);

  const watchedDetails = AForm.useWatch('details', form) as OtherStockInDetailFormValue[] | undefined;
  const details = useMemo(() => watchedDetails ?? [], [watchedDetails]);

  function openAddLine() {
    setLineOperateType('add');
    setEditingIndex(null);
    lineForm.resetFields();
    setLineOpen(true);
  }

  function openEditLine(record: OtherStockInDetailFormValue, index: number) {
    setLineOperateType('edit');
    setEditingIndex(index);
    lineForm.setFieldsValue(record);
    setLineOpen(true);
  }

  async function handleLineSubmit() {
    await lineForm.validateFields();
    const allValues = lineForm.getFieldsValue(true) as OtherStockInDetailFormValue;
    const row: OtherStockInDetailFormValue = { ...allValues };
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

  const columns: TableColumnsType<OtherStockInDetailFormValue> = [
    {
      align: 'center',
      dataIndex: 'goodsId',
      ellipsis: true,
      key: 'goodsId',
      render: (_, record) => displayText(record.goodsName || record.goodsId),
      title: t('page.storage.in.other.goodsName'),
      width: 180
    },
    {
      align: 'center',
      dataIndex: 'goodsCode',
      key: 'goodsCode',
      render: value => displayText(value),
      title: t('page.storage.in.other.goodsCode'),
      width: 130
    },
    {
      align: 'center',
      dataIndex: 'goodsUnitId',
      key: 'goodsUnitId',
      render: (_, record) => displayText(record.goodsUnitName || record.goodsUnitId),
      title: t('page.storage.in.other.goodsUnitName'),
      width: 100
    },
    {
      align: 'right',
      dataIndex: 'quantity',
      key: 'quantity',
      render: value => displayText(value),
      title: t('page.storage.in.other.quantity'),
      width: 110
    },
    {
      align: 'right',
      dataIndex: 'unitPrice',
      key: 'unitPrice',
      render: (value: number) => value?.toFixed(4),
      title: t('page.storage.in.other.unitPrice'),
      width: 110
    },
    {
      align: 'center',
      dataIndex: 'productDate',
      key: 'productDate',
      render: value => displayDate(value),
      title: t('page.storage.in.other.productDate'),
      width: 120
    },
    {
      align: 'center',
      dataIndex: 'expireDate',
      key: 'expireDate',
      render: value => displayDate(value),
      title: t('page.storage.in.other.expireDate'),
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
      <ACard
        className="card-wrapper"
        title={t('page.storage.in.other.sectionBasic')}
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
        title={t('page.storage.in.other.sectionDetails')}
        variant="borderless"
        extra={
          <AButton
            size="small"
            type="primary"
            onClick={openAddLine}
          >
            {t('page.storage.in.other.addDetail')}
          </AButton>
        }
      >
        <AForm.Item
          hidden
          name="details"
          rules={[
            {
              validator: async (_, value: OtherStockInDetailFormValue[] | undefined) => {
                if (!value?.length) {
                  throw new Error(t('page.storage.in.validation.atLeastOneDetail'));
                }
              }
            }
          ]}
        >
          <AInput />
        </AForm.Item>
        <ATable<OtherStockInDetailFormValue>
          columns={columns}
          dataSource={details}
          pagination={false}
          rowKey={(_, index) => String(index)}
          scroll={{ x: 'max-content' }}
          size="small"
        />
      </ACard>

      <Suspense fallback={null}>
        <OtherStockInDetailModal
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

export default OtherStockInOperateForm;
