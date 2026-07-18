import { EnableStatusFormItem } from '@/features/crud';
import { useFormRules } from '@/features/form';
import {
  toOptions,
  useGoodsTypeTreeOptions,
  useGoodsUnitOptions,
  useSupplierOptions,
  useWareOptions
} from '@/service/hooks';

import { mapGoodsTypeTree } from '../../utils/tree';

interface GoodsOperateFormProps {
  form: Page.FormInstance;
}

function GoodsOperateForm({ form }: GoodsOperateFormProps) {
  const { t } = useTranslation();
  const { defaultRequiredRule } = useFormRules();
  const { data: goodsTypeTree } = useGoodsTypeTreeOptions();
  const { data: unitOptions = [] } = useGoodsUnitOptions();
  const { data: suppliers } = useSupplierOptions();
  const { data: wares } = useWareOptions();

  const goodsTypeTreeOptions = useMemo(() => mapGoodsTypeTree(goodsTypeTree), [goodsTypeTree]);
  const supplierOptions = toOptions(suppliers);
  const wareOptions = toOptions(wares);

  // 树数据异步到达后，重新写入 goodsTypeId，确保 TreeSelect 能解析出分类名称
  useEffect(() => {
    if (!goodsTypeTreeOptions.length) {
      return;
    }

    const goodsTypeId = form.getFieldValue('goodsTypeId') as string | undefined;
    if (goodsTypeId) {
      form.setFieldsValue({ goodsTypeId });
    }
  }, [form, goodsTypeTreeOptions]);

  function handleDefaultSupplierChange(value: string | null) {
    if (!value) {
      return;
    }

    const supplierIds = (form.getFieldValue('supplierIds') as string[] | undefined) ?? [];
    if (!supplierIds.includes(value)) {
      form.setFieldsValue({ supplierIds: [...supplierIds, value] });
    }
  }

  function handleSupplierIdsChange(values: string[]) {
    const defaultSupplierId = form.getFieldValue('defaultSupplierId') as string | null | undefined;
    if (defaultSupplierId && !values.includes(defaultSupplierId)) {
      form.setFieldsValue({ defaultSupplierId: null });
    }
  }

  return (
    <AForm
      className="flex-col-stretch gap-16px"
      form={form}
      layout="vertical"
    >
      <AForm.Item
        hidden
        name="id"
      />

      <ACard
        className="card-wrapper"
        title={t('page.goods.operate.sectionBasic')}
        variant="borderless"
      >
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
              <ATreeSelect
                allowClear
                showSearch
                treeDefaultExpandAll
                placeholder={t('page.goods.operate.form.goodsTypeId')}
                treeData={goodsTypeTreeOptions}
                treeNodeFilterProp="title"
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
              className="mb-0"
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
      </ACard>

      <ACard
        className="card-wrapper"
        title={t('page.goods.operate.sectionSupply')}
        variant="borderless"
      >
        <ARow gutter={16}>
          <ACol
            lg={12}
            span={24}
          >
            <AForm.Item
              label={t('page.goods.operate.baseUnitId')}
              name="baseUnitId"
            >
              <ASelect
                allowClear
                showSearch
                optionFilterProp="label"
                options={unitOptions}
                placeholder={t('page.goods.operate.form.baseUnitId')}
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
              label={t('page.goods.operate.defaultSupplierId')}
              name="defaultSupplierId"
            >
              <ASelect
                allowClear
                options={supplierOptions}
                placeholder={t('page.goods.operate.form.defaultSupplierId')}
                onChange={handleDefaultSupplierChange}
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
                onChange={handleSupplierIdsChange}
              />
            </AForm.Item>
          </ACol>
          <ACol
            lg={12}
            span={24}
          >
            <AForm.Item
              className="mb-0"
              label={t('page.goods.operate.taxRate')}
              name="taxRate"
            >
              <AInputNumber
                addonAfter="%"
                className="w-full"
                max={100}
                min={0}
                placeholder={t('page.goods.operate.form.taxRate')}
              />
            </AForm.Item>
          </ACol>
        </ARow>
      </ACard>

      <ACard
        className="card-wrapper"
        title={t('page.goods.operate.sectionSale')}
        variant="borderless"
      >
        <ARow gutter={16}>
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
          <ACol
            lg={12}
            span={24}
          >
            <EnableStatusFormItem label={t('page.goods.operate.status')} />
          </ACol>
          <ACol span={24}>
            <AForm.Item
              className="mb-0"
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
      </ACard>
    </AForm>
  );
}

export default GoodsOperateForm;
