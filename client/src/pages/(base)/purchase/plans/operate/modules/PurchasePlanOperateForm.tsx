import type { TableColumnsType } from 'antd';
import { Suspense, lazy } from 'react';

import RemoteOptionSelect from '@/components/RemoteOptionSelect';
import { DETAIL_EMPTY, PurchasePatternSelect, displayText } from '@/features/crud';
import { useFormRules } from '@/features/form';
import { SELECTION_OPTION_RESOURCES, toOptions, usePurchaserOptions } from '@/service/hooks';

const PurchasePlanDetailLineModal = lazy(() => import('./PurchasePlanDetailLineModal'));

interface DetailFormItem {
  goodsCode?: string;
  goodsId: string;
  goodsName?: string;
  plannedQuantity: number;
  purchaseUnitId: string;
  purchaseUnitName?: string;
  remark?: string | null;
}

interface PurchasePlanOperateFormProps {
  form: Page.FormInstance;
  initialValues?: Partial<Api.PurchasePlan.CreateParams>;
}

function PurchasePlanOperateForm({ form, initialValues }: PurchasePlanOperateFormProps) {
  const { t } = useTranslation();
  const { defaultRequiredRule } = useFormRules();
  const { data: purchasers } = usePurchaserOptions();

  const [lineForm] = AForm.useForm<DetailFormItem>();
  const [lineOpen, setLineOpen] = useState(false);
  const [lineOperateType, setLineOperateType] = useState<AntDesign.TableOperateType>('add');
  const [editingIndex, setEditingIndex] = useState<number | null>(null);

  const watchedDetails = AForm.useWatch('details', form) as DetailFormItem[] | undefined;
  const details = useMemo(() => watchedDetails ?? [], [watchedDetails]);

  function openAddLine() {
    setLineOperateType('add');
    setEditingIndex(null);
    lineForm.resetFields();
    setLineOpen(true);
  }

  function openEditLine(record: DetailFormItem, index: number) {
    setLineOperateType('edit');
    setEditingIndex(index);
    lineForm.setFieldsValue(record);
    setLineOpen(true);
  }

  async function handleLineSubmit() {
    const values = await lineForm.validateFields();
    const row: DetailFormItem = { ...values, plannedQuantity: Number(values.plannedQuantity) };
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
    form.setFieldsValue({ details: details.filter((_, i) => i !== index) });
  }

  const columns: TableColumnsType<DetailFormItem> = [
    {
      align: 'center',
      dataIndex: 'goodsName',
      ellipsis: true,
      key: 'goodsName',
      render: (_, record) => displayText(record.goodsName),
      title: t('page.purchase.plan.goods'),
      width: 180
    },
    {
      align: 'center',
      dataIndex: 'goodsCode',
      ellipsis: true,
      key: 'goodsCode',
      render: value => displayText(value),
      title: t('page.purchase.plan.goodsCode'),
      width: 140
    },
    {
      align: 'center',
      dataIndex: 'purchaseUnitName',
      key: 'purchaseUnitName',
      render: (_, record) => displayText(record.purchaseUnitName || record.purchaseUnitId),
      title: t('page.purchase.plan.unit'),
      width: 120
    },
    {
      align: 'right',
      dataIndex: 'plannedQuantity',
      key: 'plannedQuantity',
      render: value => (value === null || value === undefined ? DETAIL_EMPTY : String(value)),
      title: t('page.purchase.plan.plannedQuantity'),
      width: 120
    },
    {
      align: 'center',
      dataIndex: 'remark',
      ellipsis: true,
      key: 'remark',
      render: value => displayText(value),
      title: t('page.purchase.plan.remark'),
      width: 140
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
          name="details"
          rules={[
            {
              validator: async (_, value: DetailFormItem[] | undefined) => {
                if (!value?.length) {
                  throw new Error(t('page.purchase.plan.operate.detailsRequired'));
                }
              }
            }
          ]}
        />

        <ACard
          className="card-wrapper"
          title={t('page.purchase.plan.operate.sectionBasic')}
          variant="borderless"
        >
          <ARow gutter={16}>
            <ACol
              lg={12}
              span={24}
            >
              <AForm.Item
                label={t('page.purchase.plan.planDate')}
                name="planDate"
                rules={[defaultRequiredRule]}
              >
                <ADatePicker
                  className="w-full"
                  format="YYYY-MM-DD"
                  placeholder={t('page.purchase.plan.form.planDate')}
                />
              </AForm.Item>
            </ACol>
            <ACol
              lg={12}
              span={24}
            >
              <AForm.Item
                label={t('page.purchase.plan.purchasePattern')}
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
                label={t('page.purchase.plan.supplier')}
                name="supplierId"
              >
                <RemoteOptionSelect
                  allowClear
                  placeholder={t('page.purchase.plan.form.supplierId')}
                  resource={SELECTION_OPTION_RESOURCES.SUPPLIER}
                />
              </AForm.Item>
            </ACol>
            <ACol
              lg={12}
              span={24}
            >
              <AForm.Item
                className="mb-0"
                label={t('page.purchase.plan.purchaser')}
                name="purchaserId"
              >
                <ASelect
                  allowClear
                  options={toOptions(purchasers)}
                  placeholder={t('page.purchase.plan.form.purchaserId')}
                />
              </AForm.Item>
            </ACol>
          </ARow>
        </ACard>

        <ACard
          className="card-wrapper"
          title={t('page.purchase.plan.operate.sectionDetails')}
          variant="borderless"
          extra={
            <AButton
              size="small"
              type="primary"
              onClick={openAddLine}
            >
              {t('page.purchase.plan.operate.addDetail')}
            </AButton>
          }
        >
          <ATable<DetailFormItem>
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
          title={t('page.purchase.plan.operate.sectionRemark')}
          variant="borderless"
        >
          <AForm.Item
            className="mb-0"
            label={t('page.purchase.plan.remark')}
            name="remark"
          >
            <AInput.TextArea
              maxLength={500}
              placeholder={t('page.purchase.plan.form.remark')}
              rows={3}
            />
          </AForm.Item>
        </ACard>
      </AForm>

      <Suspense fallback={null}>
        <PurchasePlanDetailLineModal
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

export default PurchasePlanOperateForm;
