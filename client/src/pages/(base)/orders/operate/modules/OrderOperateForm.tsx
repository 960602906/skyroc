import type { TableColumnsType } from 'antd';
import dayjs from 'dayjs';
import { Suspense, lazy } from 'react';

import RemoteOptionSelect from '@/components/RemoteOptionSelect';
import { PICKER_FORMATS } from '@/constants/datetime';
import { DETAIL_EMPTY, displayText } from '@/features/crud';
import { useFormRules } from '@/features/form';
import { fetchGetCustomerDetail } from '@/service/api';
import { SELECTION_OPTION_RESOURCES, toOptions, useWareOptions } from '@/service/hooks';

import { estimateDetailTotal, formatMoney } from './order-form-utils';

const OrderDetailLineModal = lazy(() => import('./OrderDetailLineModal'));

type RuleKey = 'customerId' | 'orderDate';

interface OrderOperateFormProps {
  form: Page.FormInstance;
  /** 新增页默认值；编辑页由父级 setFieldsValue 回填 */
  initialValues?: Api.Order.FormValues;
}

function OrderOperateForm({ form, initialValues }: OrderOperateFormProps) {
  const { t } = useTranslation();
  const { defaultRequiredRule } = useFormRules();

  const { data: wares } = useWareOptions();

  const [lineForm] = AForm.useForm<Api.Order.DetailFormItem>();
  const [lineOpen, setLineOpen] = useState(false);
  const [lineOperateType, setLineOperateType] = useState<AntDesign.TableOperateType>('add');
  const [editingIndex, setEditingIndex] = useState<number | null>(null);
  const [fillingCustomer, setFillingCustomer] = useState(false);

  const watchedDetails = AForm.useWatch('details', form) as Api.Order.DetailFormItem[] | undefined;
  const details = useMemo(() => watchedDetails ?? [], [watchedDetails]);

  const rules: Record<RuleKey, App.Global.FormRule> = {
    customerId: defaultRequiredRule,
    orderDate: defaultRequiredRule
  };

  const estimatedAmount = useMemo(
    () =>
      details.reduce((sum, item) => {
        const total = estimateDetailTotal(item);
        return sum + (total ?? 0);
      }, 0),
    [details]
  );

  async function handleCustomerChange(customerId: string) {
    if (!customerId) {
      return;
    }

    setFillingCustomer(true);
    try {
      const customer = await fetchGetCustomerDetail(customerId);
      if (!customer) {
        return;
      }

      // 切换客户时回填联系信息与默认仓库/报价（空值也覆盖，避免残留上一客户）
      form.setFieldsValue({
        contactName: customer.contactName ?? null,
        contactPhone: customer.contactPhone ?? null,
        deliveryAddress: customer.address ?? null,
        quotationId: customer.quotationId ?? null,
        wareId: customer.defaultWareId ?? null
      });
    } finally {
      setFillingCustomer(false);
    }
  }

  function openAddLine() {
    setLineOperateType('add');
    setEditingIndex(null);
    lineForm.resetFields();
    setLineOpen(true);
  }

  function openEditLine(record: Api.Order.DetailFormItem, index: number) {
    setLineOperateType('edit');
    setEditingIndex(index);
    lineForm.setFieldsValue(record);
    setLineOpen(true);
  }

  async function handleLineSubmit() {
    const values = await lineForm.validateFields();
    const nextDetails = [...details];
    const row: Api.Order.DetailFormItem = {
      ...values,
      fixedPrice: Number(values.fixedPrice),
      quantity: Number(values.quantity)
    };

    if (lineOperateType === 'edit' && editingIndex !== null) {
      nextDetails[editingIndex] = {
        ...nextDetails[editingIndex],
        ...row
      };
    } else {
      nextDetails.push(row);
    }

    form.setFieldsValue({ details: nextDetails });
    // 触发明细必填校验清除
    form.setFields([
      {
        errors: [],
        name: 'details'
      }
    ]);
    setLineOpen(false);
    lineForm.resetFields();
    setEditingIndex(null);
  }

  function handleRemoveLine(index: number) {
    const nextDetails = details.filter((_, i) => i !== index);
    form.setFieldsValue({ details: nextDetails });
  }

  const columns: TableColumnsType<Api.Order.DetailFormItem> = [
    {
      align: 'center',
      dataIndex: 'goodsName',
      ellipsis: true,
      key: 'goodsName',
      render: (_, record) => displayText(record.goodsName || record.goodsId),
      title: t('page.order.detail.goodsName'),
      width: 180
    },
    {
      align: 'center',
      dataIndex: 'goodsCode',
      ellipsis: true,
      key: 'goodsCode',
      render: value => displayText(value),
      title: t('page.order.detail.goodsCode'),
      width: 120
    },
    {
      align: 'center',
      dataIndex: 'goodsUnitName',
      key: 'goodsUnitName',
      render: (_, record) => displayText(record.goodsUnitName || record.goodsUnitId),
      title: t('page.order.detail.goodsUnitName'),
      width: 100
    },
    {
      align: 'right',
      dataIndex: 'quantity',
      key: 'quantity',
      render: value => displayText(value),
      title: t('page.order.detail.quantity'),
      width: 100
    },
    {
      align: 'right',
      dataIndex: 'fixedPrice',
      key: 'fixedPrice',
      render: value => formatMoney(value as number),
      title: t('page.order.detail.fixedPrice'),
      width: 110
    },
    {
      align: 'center',
      dataIndex: 'fixedGoodsUnitName',
      key: 'fixedGoodsUnitName',
      render: (_, record) => displayText(record.fixedGoodsUnitName || record.fixedGoodsUnitId),
      title: t('page.order.detail.fixedGoodsUnitName'),
      width: 100
    },
    {
      align: 'right',
      key: 'totalPrice',
      render: (_, record) => {
        const total = estimateDetailTotal(record);
        return total === null ? DETAIL_EMPTY : formatMoney(total);
      },
      title: t('page.order.detail.totalPrice'),
      width: 110
    },
    {
      align: 'center',
      dataIndex: 'remark',
      ellipsis: true,
      key: 'remark',
      render: value => displayText(value),
      title: t('page.order.detail.remark'),
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
      width: 150
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
              validator: async (_, value: Api.Order.DetailFormItem[] | undefined) => {
                if (!value?.length) {
                  throw new Error(t('page.order.operate.detailsRequired'));
                }
              }
            }
          ]}
        />

        <ACard
          className="card-wrapper"
          title={t('page.order.operate.sectionBasic')}
          variant="borderless"
        >
          <ARow gutter={16}>
            <ACol
              lg={12}
              span={24}
            >
              <AForm.Item
                label={t('page.order.list.customerId')}
                name="customerId"
                rules={[rules.customerId]}
              >
                <RemoteOptionSelect
                  loading={fillingCustomer}
                  placeholder={t('page.order.operate.form.customerId')}
                  resource={SELECTION_OPTION_RESOURCES.CUSTOMER}
                  onChange={handleCustomerChange}
                />
              </AForm.Item>
            </ACol>
            <ACol
              lg={12}
              span={24}
            >
              <AForm.Item
                label={t('page.order.detail.wareId')}
                name="wareId"
              >
                <ASelect
                  allowClear
                  showSearch
                  optionFilterProp="label"
                  options={toOptions(wares)}
                  placeholder={t('page.order.operate.form.wareId')}
                />
              </AForm.Item>
            </ACol>
            <ACol
              lg={12}
              span={24}
            >
              <AForm.Item
                label={t('page.order.detail.quotationId')}
                name="quotationId"
              >
                <RemoteOptionSelect
                  allowClear
                  placeholder={t('page.order.operate.form.quotationId')}
                  resource={SELECTION_OPTION_RESOURCES.QUOTATION}
                />
              </AForm.Item>
            </ACol>
            <ACol
              lg={12}
              span={24}
            >
              <AForm.Item
                getValueFromEvent={(_, dateString) => dateString || null}
                label={t('page.order.list.orderDate')}
                name="orderDate"
                rules={[rules.orderDate]}
                getValueProps={value => ({
                  value: value ? dayjs(value) : undefined
                })}
              >
                <ADatePicker
                  className="w-full"
                  format={PICKER_FORMATS.DATE}
                  placeholder={t('page.order.operate.form.orderDate')}
                />
              </AForm.Item>
            </ACol>
            <ACol
              lg={12}
              span={24}
            >
              <AForm.Item
                getValueFromEvent={(_, dateString) => dateString || null}
                label={t('page.order.list.receiveDate')}
                name="receiveDate"
                getValueProps={value => ({
                  value: value ? dayjs(value) : undefined
                })}
              >
                <ADatePicker
                  className="w-full"
                  format={PICKER_FORMATS.DATE}
                  placeholder={t('page.order.operate.form.receiveDate')}
                />
              </AForm.Item>
            </ACol>
            <ACol
              lg={12}
              span={24}
            >
              <AForm.Item
                label={t('page.order.list.contactName')}
                name="contactName"
              >
                <AInput
                  allowClear
                  maxLength={50}
                  placeholder={t('page.order.operate.form.contactName')}
                />
              </AForm.Item>
            </ACol>
            <ACol
              lg={12}
              span={24}
            >
              <AForm.Item
                label={t('page.order.list.contactPhone')}
                name="contactPhone"
              >
                <AInput
                  allowClear
                  maxLength={20}
                  placeholder={t('page.order.operate.form.contactPhone')}
                />
              </AForm.Item>
            </ACol>
            <ACol span={24}>
              <AForm.Item
                className="mb-0"
                label={t('page.order.detail.deliveryAddress')}
                name="deliveryAddress"
              >
                <AInput.TextArea
                  allowClear
                  maxLength={300}
                  placeholder={t('page.order.operate.form.deliveryAddress')}
                  rows={2}
                />
              </AForm.Item>
            </ACol>
          </ARow>
        </ACard>

        <ACard
          className="card-wrapper"
          title={t('page.order.operate.sectionDetails')}
          variant="borderless"
          extra={
            <ASpace>
              <span className="opacity-70">
                {t('page.order.operate.estimatedAmount')}：{formatMoney(estimatedAmount)}
              </span>
              <AButton
                size="small"
                type="primary"
                onClick={openAddLine}
              >
                {t('page.order.operate.addDetail')}
              </AButton>
            </ASpace>
          }
        >
          <ATable<Api.Order.DetailFormItem>
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
          title={t('page.order.operate.sectionRemark')}
          variant="borderless"
        >
          <ARow gutter={16}>
            <ACol
              lg={12}
              span={24}
            >
              <AForm.Item
                label={t('page.order.detail.remark')}
                name="remark"
              >
                <AInput.TextArea
                  maxLength={500}
                  placeholder={t('page.order.operate.form.remark')}
                  rows={3}
                />
              </AForm.Item>
            </ACol>
            <ACol
              lg={12}
              span={24}
            >
              <AForm.Item
                className="mb-0"
                label={t('page.order.detail.innerRemark')}
                name="innerRemark"
              >
                <AInput.TextArea
                  maxLength={500}
                  placeholder={t('page.order.operate.form.innerRemark')}
                  rows={3}
                />
              </AForm.Item>
            </ACol>
          </ARow>
        </ACard>
      </AForm>

      <Suspense fallback={null}>
        <OrderDetailLineModal
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

export default OrderOperateForm;
