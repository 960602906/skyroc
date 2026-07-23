import type { TableColumnsType } from 'antd';
import dayjs from 'dayjs';
import { Suspense, lazy } from 'react';

import RemoteOptionSelect from '@/components/RemoteOptionSelect';
import { DETAIL_EMPTY, PurchasePatternSelect, displayText } from '@/features/crud';
import { useFormRules } from '@/features/form';
import { SELECTION_OPTION_RESOURCES, toOptions, usePurchaserOptions } from '@/service/hooks';

import type { PurchaseOrderDetailFormItem, PurchaseOrderFormValues } from './purchase-order-form-utils';
import { formatMoney } from './purchase-order-form-utils';

const PurchaseOrderDetailLineModal = lazy(() => import('./PurchaseOrderDetailLineModal'));

interface PurchaseOrderOperateFormProps {
  form: Page.FormInstance;
  initialValues?: PurchaseOrderFormValues;
}

/** 采购单新增/编辑表单（分卡片布局）。 */
function PurchaseOrderOperateForm({ form, initialValues }: PurchaseOrderOperateFormProps) {
  const { t } = useTranslation();
  const { defaultRequiredRule } = useFormRules();
  const { data: purchasers } = usePurchaserOptions();

  const [lineForm] = AForm.useForm<PurchaseOrderDetailFormItem>();
  const [lineOpen, setLineOpen] = useState(false);
  const [lineOperateType, setLineOperateType] = useState<AntDesign.TableOperateType>('add');
  const [editingIndex, setEditingIndex] = useState<number | null>(null);

  const watchedDetails = AForm.useWatch('details', form) as PurchaseOrderDetailFormItem[] | undefined;
  const details = useMemo(() => watchedDetails ?? [], [watchedDetails]);

  const estimatedAmount = useMemo(
    () => details.reduce((sum, item) => sum + Number(item.purchasePrice || 0) * Number(item.purchaseQuantity || 0), 0),
    [details]
  );

  function openAddLine() {
    setLineOperateType('add');
    setEditingIndex(null);
    lineForm.resetFields();
    setLineOpen(true);
  }

  function openEditLine(record: PurchaseOrderDetailFormItem, index: number) {
    setLineOperateType('edit');
    setEditingIndex(index);
    lineForm.setFieldsValue(record);
    setLineOpen(true);
  }

  async function handleLineSubmit() {
    const values = await lineForm.validateFields();
    const nextDetails = [...details];
    const row: PurchaseOrderDetailFormItem = {
      ...values,
      purchasePrice: Number(values.purchasePrice),
      purchaseQuantity: Number(values.purchaseQuantity)
    };

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
    form.setFieldsValue({ details: details.filter((_, i) => i !== index) });
  }

  const columns: TableColumnsType<PurchaseOrderDetailFormItem> = [
    {
      align: 'center',
      dataIndex: 'goodsName',
      ellipsis: true,
      key: 'goodsName',
      render: (_, record) => displayText(record.goodsName || record.goodsId),
      title: t('page.purchase.order.goodsName'),
      width: 170
    },
    {
      align: 'center',
      dataIndex: 'goodsCode',
      ellipsis: true,
      key: 'goodsCode',
      render: value => displayText(value),
      title: t('page.purchase.order.goodsCode'),
      width: 130
    },
    {
      align: 'center',
      dataIndex: 'purchaseUnitName',
      key: 'purchaseUnitName',
      render: (_, record) => displayText(record.purchaseUnitName || record.purchaseUnitId),
      title: t('page.purchase.order.unit'),
      width: 100
    },
    {
      align: 'right',
      dataIndex: 'purchaseQuantity',
      key: 'purchaseQuantity',
      render: value => displayText(value),
      title: t('page.purchase.order.purchaseQuantity'),
      width: 110
    },
    {
      align: 'right',
      dataIndex: 'purchasePrice',
      key: 'purchasePrice',
      render: value => formatMoney(value as number),
      title: t('page.purchase.order.purchasePrice'),
      width: 110
    },
    {
      align: 'right',
      key: 'totalPrice',
      render: (_, record) => {
        const qty = Number(record.purchaseQuantity);
        const price = Number(record.purchasePrice);
        return Number.isFinite(qty) && Number.isFinite(price) ? formatMoney(qty * price) : DETAIL_EMPTY;
      },
      title: t('page.purchase.order.purchaseTotalPrice'),
      width: 110
    },
    {
      align: 'center',
      dataIndex: 'productDate',
      key: 'productDate',
      render: value => displayText(value),
      title: t('page.purchase.order.productDate'),
      width: 120
    },
    {
      align: 'right',
      dataIndex: 'requiredQuantity',
      key: 'requiredQuantity',
      render: value => displayText(value),
      title: t('page.purchase.order.requiredQuantity'),
      width: 110
    },
    {
      align: 'center',
      dataIndex: 'remark',
      ellipsis: true,
      key: 'remark',
      render: value => displayText(value),
      title: t('page.purchase.order.remark'),
      width: 130
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
      width: 140
    }
  ];

  return (
    <>
      <AForm
        className="flex-col-stretch gap-16px"
        form={form}
        initialValues={initialValues}
        layout="vertical"
      >
        <AForm.Item
          hidden
          name="id"
        />
        <AForm.Item
          hidden
          name="details"
          rules={[
            {
              validator: async (_, value: PurchaseOrderDetailFormItem[] | undefined) => {
                if (!value?.length) {
                  throw new Error(t('page.purchase.order.operate.detailsRequired'));
                }
              }
            }
          ]}
        />

        <ACard
          className="card-wrapper"
          title={t('page.purchase.order.sectionBasic')}
          variant="borderless"
        >
          <ARow gutter={16}>
            <ACol
              lg={12}
              span={24}
            >
              <AForm.Item
                label={t('page.purchase.order.purchasePattern')}
                name="purchasePattern"
                rules={[defaultRequiredRule]}
              >
                <PurchasePatternSelect allowClear={false} />
              </AForm.Item>
            </ACol>
            <ACol
              lg={12}
              span={24}
            >
              <AForm.Item
                getValueFromEvent={(_, dateString) => dateString || null}
                getValueProps={value => ({ value: value ? dayjs(value) : undefined })}
                label={t('page.purchase.order.receiveTime')}
                name="receiveTime"
              >
                <ADatePicker
                  showTime
                  className="w-full"
                  placeholder={t('page.purchase.order.form.receiveTime')}
                />
              </AForm.Item>
            </ACol>
            <ACol
              lg={12}
              span={24}
            >
              <AForm.Item
                label={t('page.purchase.order.supplier')}
                name="supplierId"
              >
                <RemoteOptionSelect
                  allowClear
                  placeholder={t('page.purchase.order.form.supplierId')}
                  resource={SELECTION_OPTION_RESOURCES.SUPPLIER}
                />
              </AForm.Item>
            </ACol>
            <ACol
              lg={12}
              span={24}
            >
              <AForm.Item
                label={t('page.purchase.order.purchaser')}
                name="purchaserId"
              >
                <ASelect
                  allowClear
                  options={toOptions(purchasers)}
                  placeholder={t('page.purchase.order.form.purchaserId')}
                />
              </AForm.Item>
            </ACol>
            <ACol
              lg={12}
              span={24}
            >
              <AForm.Item
                label={t('page.purchase.order.supplierContactName')}
                name="supplierContactName"
              >
                <AInput
                  allowClear
                  maxLength={50}
                  placeholder={t('page.purchase.order.form.supplierContactName')}
                />
              </AForm.Item>
            </ACol>
            <ACol
              lg={12}
              span={24}
            >
              <AForm.Item
                className="mb-0"
                label={t('page.purchase.order.supplierContactPhone')}
                name="supplierContactPhone"
              >
                <AInput
                  allowClear
                  maxLength={30}
                  placeholder={t('page.purchase.order.form.supplierContactPhone')}
                />
              </AForm.Item>
            </ACol>
          </ARow>
        </ACard>

        <ACard
          className="card-wrapper"
          title={t('page.purchase.order.sectionGoods')}
          variant="borderless"
          extra={
            <ASpace>
              <span className="opacity-70">
                {t('page.purchase.order.operate.estimatedAmount')}：{formatMoney(estimatedAmount)}
              </span>
              <AButton
                size="small"
                type="primary"
                onClick={openAddLine}
              >
                {t('page.purchase.order.operate.addDetail')}
              </AButton>
            </ASpace>
          }
        >
          <ATable<PurchaseOrderDetailFormItem>
            columns={columns}
            dataSource={details}
            pagination={false}
            rowKey={(_, index) => String(index)}
            scroll={{ x: 'max-content' }}
            size="small"
          />
        </ACard>

        <ACard
          className="card-wrapper"
          title={t('page.purchase.order.sectionRemark')}
          variant="borderless"
        >
          <AForm.Item
            className="mb-0"
            label={t('page.purchase.order.remark')}
            name="remark"
          >
            <AInput.TextArea
              maxLength={500}
              placeholder={t('page.purchase.order.form.remark')}
              rows={3}
            />
          </AForm.Item>
        </ACard>
      </AForm>

      <Suspense fallback={null}>
        <PurchaseOrderDetailLineModal
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
}

export default PurchaseOrderOperateForm;
