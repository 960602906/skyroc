import { EnableStatusFormItem } from '@/features/crud';
import { useFormRules } from '@/features/form';
import { toOptions, useGoodsTypeOptions, useSupplierOptions, useWareOptions } from '@/service/hooks/useBaseDataOptions';

interface GoodsOperateFormProps {
  form: Page.FormInstance;
}

function GoodsOperateForm({ form }: GoodsOperateFormProps) {
  const { t } = useTranslation();
  const { defaultRequiredRule } = useFormRules();
  const { data: goodsTypes } = useGoodsTypeOptions();
  const { data: suppliers } = useSupplierOptions();
  const { data: wares } = useWareOptions();

  const goodsTypeOptions = toOptions(goodsTypes);
  const supplierOptions = toOptions(suppliers);
  const wareOptions = toOptions(wares);

  return (
    <AForm
      form={form}
      layout="vertical"
    >
      <AForm.Item
        hidden
        name="id"
      />

      <ATabs
        items={[
          {
            children: (
              <ARow gutter={16}>
                <ACol
                  lg={12}
                  span={24}
                >
                  <AForm.Item
                    label={t('page.goods.operate.name')}
                    name="name"
                    rules={[defaultRequiredRule]}
                  >
                    <AInput placeholder={t('page.goods.operate.form.name')} />
                  </AForm.Item>
                </ACol>
                <ACol
                  lg={12}
                  span={24}
                >
                  <AForm.Item
                    label={t('page.goods.operate.code')}
                    name="code"
                    rules={[defaultRequiredRule]}
                  >
                    <AInput placeholder={t('page.goods.operate.form.code')} />
                  </AForm.Item>
                </ACol>
                <ACol
                  lg={12}
                  span={24}
                >
                  <AForm.Item
                    label={t('page.goods.operate.goodsTypeId')}
                    name="goodsTypeId"
                    rules={[defaultRequiredRule]}
                  >
                    <ASelect
                      options={goodsTypeOptions}
                      placeholder={t('page.goods.operate.form.goodsTypeId')}
                    />
                  </AForm.Item>
                </ACol>
                <ACol
                  lg={12}
                  span={24}
                >
                  <AForm.Item
                    label={t('page.goods.operate.spec')}
                    name="spec"
                  >
                    <AInput placeholder={t('page.goods.operate.form.spec')} />
                  </AForm.Item>
                </ACol>
                <ACol
                  lg={12}
                  span={24}
                >
                  <AForm.Item
                    label={t('page.goods.operate.brand')}
                    name="brand"
                  >
                    <AInput placeholder={t('page.goods.operate.form.brand')} />
                  </AForm.Item>
                </ACol>
                <ACol
                  lg={12}
                  span={24}
                >
                  <AForm.Item
                    label={t('page.goods.operate.origin')}
                    name="origin"
                  >
                    <AInput placeholder={t('page.goods.operate.form.origin')} />
                  </AForm.Item>
                </ACol>
                <ACol span={24}>
                  <AForm.Item
                    label={t('page.goods.operate.description')}
                    name="description"
                  >
                    <AInput.TextArea
                      placeholder={t('page.goods.operate.form.description')}
                      rows={4}
                    />
                  </AForm.Item>
                </ACol>
              </ARow>
            ),
            key: 'basic',
            label: t('page.goods.operate.tabBasic')
          },
          {
            children: (
              <ARow gutter={16}>
                <ACol
                  lg={12}
                  span={24}
                >
                  <AForm.Item
                    label={t('page.goods.operate.baseUnitId')}
                    name="baseUnitId"
                  >
                    <AInput placeholder={t('page.goods.operate.form.baseUnitId')} />
                  </AForm.Item>
                </ACol>
                <ACol
                  lg={12}
                  span={24}
                >
                  <AForm.Item
                    label={t('page.goods.operate.defaultSupplierId')}
                    name="defaultSupplierId"
                  >
                    <ASelect
                      allowClear
                      options={supplierOptions}
                      placeholder={t('page.goods.operate.form.defaultSupplierId')}
                    />
                  </AForm.Item>
                </ACol>
                <ACol
                  lg={12}
                  span={24}
                >
                  <AForm.Item
                    label={t('page.goods.operate.defaultWareId')}
                    name="defaultWareId"
                  >
                    <ASelect
                      allowClear
                      options={wareOptions}
                      placeholder={t('page.goods.operate.form.defaultWareId')}
                    />
                  </AForm.Item>
                </ACol>
                <ACol
                  lg={12}
                  span={24}
                >
                  <AForm.Item
                    label={t('page.goods.operate.supplierIds')}
                    name="supplierIds"
                  >
                    <ASelect
                      allowClear
                      mode="multiple"
                      options={supplierOptions}
                      placeholder={t('page.goods.operate.form.supplierIds')}
                    />
                  </AForm.Item>
                </ACol>
                <ACol
                  lg={12}
                  span={24}
                >
                  <AForm.Item
                    label={t('page.goods.operate.taxRate')}
                    name="taxRate"
                  >
                    <AInputNumber
                      className="w-full"
                      max={100}
                      min={0}
                      placeholder={t('page.goods.operate.form.taxRate')}
                    />
                  </AForm.Item>
                </ACol>
                <ACol
                  lg={12}
                  span={24}
                >
                  <AForm.Item
                    label={t('page.goods.operate.isOnSale')}
                    name="isOnSale"
                    valuePropName="checked"
                  >
                    <ASwitch />
                  </AForm.Item>
                </ACol>
                <ACol span={24}>
                  <EnableStatusFormItem label={t('page.goods.operate.status')} />
                </ACol>
                <ACol span={24}>
                  <AForm.Item
                    label={t('page.goods.operate.remark')}
                    name="remark"
                  >
                    <AInput.TextArea
                      placeholder={t('page.goods.operate.form.remark')}
                      rows={3}
                    />
                  </AForm.Item>
                </ACol>
              </ARow>
            ),
            key: 'relation',
            label: t('page.goods.operate.tabRelation')
          }
        ]}
      />
    </AForm>
  );
}

export function createDefaultGoodsFormValues() {
  return {
    isOnSale: true,
    status: 1 as Api.Common.EnableStatus,
    supplierIds: undefined
  };
}

export default GoodsOperateForm;
