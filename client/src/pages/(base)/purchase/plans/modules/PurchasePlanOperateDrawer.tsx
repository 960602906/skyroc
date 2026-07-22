import type { FormListFieldData } from 'antd/es/form/FormList';

import RemoteOptionSelect from '@/components/RemoteOptionSelect';
import { PurchasePatternSelect } from '@/features/crud';
import { useFormRules } from '@/features/form';
import {
  SELECTION_OPTION_RESOURCES,
  toOptions,
  useGoodsUnitsByGoodsOptions,
  usePurchaserOptions
} from '@/service/hooks';

interface PurchasePlanOperateDrawerProps {
  onClose: () => void;
  onSubmit: (values: Record<string, any>) => Promise<void> | void;
  open: boolean;
}

/** 采购计划商品行：商品远程搜索，单位随当前商品联动加载。 */
function PurchasePlanDetailRow({
  field,
  form,
  remove
}: {
  field: FormListFieldData;
  form: Page.FormInstance;
  remove: (index: number | number[]) => void;
}) {
  const { t } = useTranslation();
  const goodsId = AForm.useWatch(['details', field.name, 'goodsId'], form) as string | undefined;
  const { data: units = [] } = useGoodsUnitsByGoodsOptions(goodsId);
  const { defaultRequiredRule } = useFormRules();

  return (
    <ARow gutter={8}>
      <ACol span={9}>
        <AForm.Item
          {...field}
          name={[field.name, 'goodsId']}
          rules={[defaultRequiredRule]}
        >
          <RemoteOptionSelect
            placeholder={t('page.purchase.plan.form.goodsId')}
            resource={SELECTION_OPTION_RESOURCES.GOODS}
            onChange={() => form.setFieldValue(['details', field.name, 'purchaseUnitId'], undefined)}
          />
        </AForm.Item>
      </ACol>
      <ACol span={7}>
        <AForm.Item
          {...field}
          name={[field.name, 'purchaseUnitId']}
          rules={[defaultRequiredRule]}
        >
          <ASelect
            disabled={!goodsId}
            options={units}
            placeholder={t('page.purchase.plan.form.purchaseUnitId')}
          />
        </AForm.Item>
      </ACol>
      <ACol span={6}>
        <AForm.Item
          {...field}
          name={[field.name, 'plannedQuantity']}
          rules={[defaultRequiredRule]}
        >
          <AInputNumber
            className="w-full"
            min={0.0001}
            placeholder={t('page.purchase.plan.form.plannedQuantity')}
          />
        </AForm.Item>
      </ACol>
      <ACol span={2}>
        <AButton
          block
          danger
          onClick={() => remove(field.name)}
        >
          -
        </AButton>
      </ACol>
    </ARow>
  );
}

/** 采购计划手工新增抽屉。 */
const PurchasePlanOperateDrawer: FC<PurchasePlanOperateDrawerProps> = memo(({ onClose, onSubmit, open }) => {
  const { t } = useTranslation();
  const { defaultRequiredRule } = useFormRules();
  const [form] = AForm.useForm();
  const { data: purchasers } = usePurchaserOptions();
  const purchaserOptions = toOptions(purchasers);

  // 打开时初始化默认值
  useEffect(() => {
    if (open) {
      form.setFieldsValue({ details: [{}], planDate: undefined, purchasePattern: 1 });
    }
  }, [form, open]);

  async function handleConfirm() {
    const values = await form.validateFields();
    await onSubmit(values);
    form.resetFields();
  }

  function handleClose() {
    form.resetFields();
    onClose();
  }

  return (
    <ADrawer
      destroyOnClose
      open={open}
      title={t('page.purchase.plan.add')}
      width={900}
      footer={
        <AFlex justify="space-between">
          <AButton onClick={handleClose}>{t('common.cancel')}</AButton>
          <AButton
            type="primary"
            onClick={handleConfirm}
          >
            {t('common.confirm')}
          </AButton>
        </AFlex>
      }
      onClose={handleClose}
    >
      <AForm
        form={form}
        layout="vertical"
      >
        <ARow gutter={16}>
          <ACol span={12}>
            <AForm.Item
              label={t('page.purchase.plan.planDate')}
              name="planDate"
              rules={[defaultRequiredRule]}
            >
              <ADatePicker
                className="w-full"
                placeholder={t('page.purchase.plan.form.planDate')}
              />
            </AForm.Item>
          </ACol>
          <ACol span={12}>
            <AForm.Item
              label={t('page.purchase.plan.purchasePattern')}
              name="purchasePattern"
              rules={[defaultRequiredRule]}
            >
              <PurchasePatternSelect allowClear={false} />
            </AForm.Item>
          </ACol>
          <ACol span={12}>
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
          <ACol span={12}>
            <AForm.Item
              label={t('page.purchase.plan.purchaser')}
              name="purchaserId"
            >
              <ASelect
                allowClear
                options={purchaserOptions}
                placeholder={t('page.purchase.plan.form.purchaserId')}
              />
            </AForm.Item>
          </ACol>
        </ARow>

        <AForm.Item label={t('page.purchase.plan.details')}>
          <AForm.List name="details">
            {(fields, { add, remove }) => (
              <>
                {fields.map(field => (
                  <PurchasePlanDetailRow
                    field={field}
                    form={form}
                    key={field.key}
                    remove={remove}
                  />
                ))}
                <AButton
                  block
                  type="dashed"
                  onClick={() => add({})}
                >
                  + {t('common.add')}
                </AButton>
              </>
            )}
          </AForm.List>
        </AForm.Item>
      </AForm>
    </ADrawer>
  );
});

export default PurchasePlanOperateDrawer;
