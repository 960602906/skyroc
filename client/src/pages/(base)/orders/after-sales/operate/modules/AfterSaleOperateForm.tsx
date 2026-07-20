import type { TableColumnsType } from 'antd';

import { AfterSaleHandleTypeSelect, AfterSaleReasonTypeSelect, AfterSaleTypeSelect } from '@/features/crud';

export type AfterSaleGoodsFormItem = Api.AfterSale.GoodsPayload & {
  enabled: boolean;
  goodsCode: string;
  goodsName: string;
  goodsUnitName: string;
  maxQuantity: number;
};

export type AfterSaleFormValues = Omit<Api.AfterSale.CreatePayload, 'goods'> & {
  goods: AfterSaleGoodsFormItem[];
};

interface AfterSaleOperateFormProps {
  form: Page.FormInstance;
  onSaleOrderChange?: (saleOrderId: string) => void;
  orderNo?: string | null;
  orderOptions?: { label: string; value: string }[];
  sourceEditable: boolean;
}

function AfterSaleOperateForm({
  form,
  onSaleOrderChange,
  orderNo,
  orderOptions,
  sourceEditable
}: AfterSaleOperateFormProps) {
  const { t } = useTranslation();

  const columns: TableColumnsType<AfterSaleGoodsFormItem> = [
    {
      dataIndex: 'enabled',
      key: 'enabled',
      render: (_, __, index) => (
        <AForm.Item
          className="mb-0"
          name={['goods', index, 'enabled']}
          valuePropName="checked"
        >
          <ACheckbox />
        </AForm.Item>
      ),
      title: t('page.afterSale.operate.apply'),
      width: 64
    },
    { dataIndex: 'goodsCode', key: 'goodsCode', title: t('page.afterSale.detail.goodsCode'), width: 120 },
    { dataIndex: 'goodsName', key: 'goodsName', title: t('page.afterSale.detail.goodsName'), width: 180 },
    { dataIndex: 'goodsUnitName', key: 'goodsUnitName', title: t('page.afterSale.operate.goodsUnit'), width: 90 },
    {
      key: 'actualRefundQuantity',
      render: (_, __, index) => (
        <AForm.Item
          className="mb-0"
          name={['goods', index, 'actualRefundQuantity']}
          rules={[{ message: '请输入大于 0 的申请数量', required: true }]}
        >
          <AInputNumber
            min={0.0001}
            precision={4}
          />
        </AForm.Item>
      ),
      title: t('page.afterSale.operate.refundQuantity'),
      width: 130
    },
    {
      key: 'afterSaleType',
      render: (_, __, index) => (
        <AForm.Item
          className="mb-0"
          name={['goods', index, 'afterSaleType']}
        >
          <AfterSaleTypeSelect />
        </AForm.Item>
      ),
      title: t('page.afterSale.operate.type'),
      width: 140
    },
    {
      key: 'reasonType',
      render: (_, __, index) => (
        <AForm.Item
          className="mb-0"
          name={['goods', index, 'reasonType']}
        >
          <AfterSaleReasonTypeSelect />
        </AForm.Item>
      ),
      title: t('page.afterSale.operate.reasonType'),
      width: 130
    },
    {
      key: 'handleType',
      render: (_, __, index) => (
        <AForm.Item
          className="mb-0"
          name={['goods', index, 'handleType']}
        >
          <AfterSaleHandleTypeSelect />
        </AForm.Item>
      ),
      title: t('page.afterSale.operate.handleType'),
      width: 150
    },
    {
      key: 'remark',
      render: (_, __, index) => (
        <AForm.Item
          className="mb-0"
          name={['goods', index, 'remark']}
        >
          <AInput maxLength={500} />
        </AForm.Item>
      ),
      title: t('page.afterSale.operate.remark'),
      width: 180
    }
  ];

  const goods = (AForm.useWatch('goods', form) as AfterSaleGoodsFormItem[] | undefined) ?? [];

  return (
    <AForm
      className="flex-col-stretch gap-16px"
      form={form}
      layout="vertical"
    >
      <AForm.Item
        hidden
        name="source"
      />
      <AForm.Item
        hidden
        name="goods"
        rules={[
          {
            validator: async () => {
              if (!goods.some(item => item.enabled)) throw new Error('请至少选择一条售后商品');
            }
          }
        ]}
      />
      <ACard
        className="card-wrapper"
        title={t('page.afterSale.operate.sectionBasic')}
        variant="borderless"
      >
        <ARow gutter={16}>
          <ACol
            lg={12}
            span={24}
          >
            <AForm.Item
              label={t('page.afterSale.operate.saleOrder')}
              name="saleOrderId"
              rules={[{ required: true }]}
            >
              {sourceEditable ? (
                <ASelect
                  showSearch
                  optionFilterProp="label"
                  options={orderOptions}
                  placeholder={t('page.afterSale.operate.saleOrderPlaceholder')}
                  onChange={onSaleOrderChange}
                />
              ) : (
                <AInput
                  disabled
                  value={orderNo ?? '-'}
                />
              )}
            </AForm.Item>
          </ACol>
          <ACol
            lg={12}
            span={24}
          >
            <AForm.Item
              label={t('page.afterSale.operate.contactName')}
              name="contactName"
            >
              <AInput
                allowClear
                maxLength={100}
              />
            </AForm.Item>
          </ACol>
          <ACol
            lg={12}
            span={24}
          >
            <AForm.Item
              label={t('page.afterSale.operate.contactPhone')}
              name="contactPhone"
            >
              <AInput
                allowClear
                maxLength={30}
              />
            </AForm.Item>
          </ACol>
          <ACol span={24}>
            <AForm.Item
              label={t('page.afterSale.operate.pickupAddress')}
              name="pickupAddress"
            >
              <AInput.TextArea
                allowClear
                maxLength={500}
                rows={2}
              />
            </AForm.Item>
          </ACol>
          <ACol span={24}>
            <AForm.Item
              className="mb-0"
              label={t('page.afterSale.operate.remark')}
              name="remark"
            >
              <AInput.TextArea
                allowClear
                maxLength={500}
                rows={2}
              />
            </AForm.Item>
          </ACol>
        </ARow>
      </ACard>
      <ACard
        className="card-wrapper"
        title={t('page.afterSale.operate.sectionGoods')}
        variant="borderless"
      >
        <ATable
          columns={columns}
          dataSource={goods}
          pagination={false}
          rowKey="saleOrderDetailId"
          scroll={{ x: 'max-content' }}
          size="small"
        />
      </ACard>
    </AForm>
  );
}

export default AfterSaleOperateForm;
