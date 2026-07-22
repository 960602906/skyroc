import type { FormListFieldData } from 'antd/es/form/FormList';
import type { Dayjs } from 'dayjs';

import RemoteOptionSelect from '@/components/RemoteOptionSelect';
import { PurchasePatternSelect } from '@/features/crud';
import { useFormRules } from '@/features/form';
import { PurchasePattern } from '@/service/enums';
import {
  SELECTION_OPTION_RESOURCES,
  toOptions,
  useGoodsUnitsByGoodsOptions,
  usePurchaserOptions
} from '@/service/hooks';

/** 采购单商品行表单值 */
export interface PurchaseOrderDetailFormValue {
  goodsId: string;
  id?: string | null;
  productDate?: Dayjs | null;
  purchasePrice: number;
  purchaseQuantity: number;
  purchaseUnitId: string;
  remark?: string | null;
  requiredQuantity?: number | null;
}

/** 采购单抽屉表单值 */
export interface PurchaseOrderFormValue {
  details: PurchaseOrderDetailFormValue[];
  id?: string;
  purchasePattern: PurchasePattern;
  purchaserId?: string | null;
  receiveTime?: Dayjs | null;
  remark?: string | null;
  supplierContactName?: string | null;
  supplierContactPhone?: string | null;
  supplierId?: string | null;
}

/** 采购单商品行：商品远程搜索，单位随当前商品联动加载。 */
function PurchaseOrderDetailRow({
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
            placeholder={t('page.purchase.order.form.goodsId')}
            resource={SELECTION_OPTION_RESOURCES.GOODS}
            onChange={() => form.setFieldValue(['details', field.name, 'purchaseUnitId'], undefined)}
          />
        </AForm.Item>
      </ACol>
      <ACol span={5}>
        <AForm.Item
          {...field}
          name={[field.name, 'purchaseUnitId']}
          rules={[defaultRequiredRule]}
        >
          <ASelect
            disabled={!goodsId}
            options={units}
            placeholder={t('page.purchase.order.form.purchaseUnitId')}
          />
        </AForm.Item>
      </ACol>
      <ACol span={3}>
        <AForm.Item
          {...field}
          name={[field.name, 'purchaseQuantity']}
          rules={[defaultRequiredRule]}
        >
          <AInputNumber
            className="w-full"
            min={0.0001}
            placeholder={t('page.purchase.order.form.purchaseQuantity')}
          />
        </AForm.Item>
      </ACol>
      <ACol span={3}>
        <AForm.Item
          {...field}
          name={[field.name, 'purchasePrice']}
          rules={[defaultRequiredRule]}
        >
          <AInputNumber
            className="w-full"
            min={0}
            placeholder={t('page.purchase.order.form.purchasePrice')}
            precision={4}
          />
        </AForm.Item>
      </ACol>
      <ACol span={3}>
        <AForm.Item
          {...field}
          name={[field.name, 'productDate']}
        >
          <ADatePicker
            className="w-full"
            placeholder={t('page.purchase.order.form.productDate')}
          />
        </AForm.Item>
      </ACol>
      <ACol span={1}>
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

/** 采购单新增/编辑抽屉。 */
const PurchaseOrderOperateDrawer: FC<Page.OperateDrawerProps> = memo(
  ({ form, handleSubmit, onClose, open, operateType }) => {
    const { t } = useTranslation();
    const { defaultRequiredRule } = useFormRules();
    const { data: purchasers } = usePurchaserOptions();
    const purchaserOptions = toOptions(purchasers);
    const isEdit = operateType === 'edit';

    return (
      <ADrawer
        open={open}
        title={isEdit ? t('page.purchase.order.edit') : t('page.purchase.order.add')}
        width={900}
        footer={
          <AFlex justify="space-between">
            <AButton onClick={onClose}>{t('common.cancel')}</AButton>
            <AButton
              type="primary"
              onClick={handleSubmit}
            >
              {t('common.confirm')}
            </AButton>
          </AFlex>
        }
        onClose={onClose}
      >
        <AForm
          form={form}
          initialValues={{ details: [{}], purchasePattern: PurchasePattern.SUPPLIER_DIRECT }}
          layout="vertical"
        >
          <AForm.Item
            hidden
            name="id"
          >
            <AInput />
          </AForm.Item>

          <ARow gutter={16}>
            <ACol span={12}>
              <AForm.Item
                label={t('page.purchase.order.purchasePattern')}
                name="purchasePattern"
                rules={[defaultRequiredRule]}
              >
                <PurchasePatternSelect allowClear={false} />
              </AForm.Item>
            </ACol>
            <ACol span={12}>
              <AForm.Item
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
            <ACol span={12}>
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
            <ACol span={12}>
              <AForm.Item
                label={t('page.purchase.order.purchaser')}
                name="purchaserId"
              >
                <ASelect
                  allowClear
                  options={purchaserOptions}
                  placeholder={t('page.purchase.order.form.purchaserId')}
                />
              </AForm.Item>
            </ACol>
            <ACol span={12}>
              <AForm.Item
                label={t('page.purchase.order.supplierContactName')}
                name="supplierContactName"
              >
                <AInput placeholder={t('page.purchase.order.form.supplierContactName')} />
              </AForm.Item>
            </ACol>
            <ACol span={12}>
              <AForm.Item
                label={t('page.purchase.order.supplierContactPhone')}
                name="supplierContactPhone"
              >
                <AInput placeholder={t('page.purchase.order.form.supplierContactPhone')} />
              </AForm.Item>
            </ACol>
          </ARow>

          <AForm.Item label={t('page.purchase.order.details')}>
            <AForm.List name="details">
              {(fields, { add, remove }) => (
                <>
                  {fields.map(field => (
                    <PurchaseOrderDetailRow
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

          <AForm.Item
            label={t('page.purchase.order.remark')}
            name="remark"
          >
            <AInput.TextArea
              placeholder={t('page.purchase.order.form.remark')}
              rows={2}
            />
          </AForm.Item>
        </AForm>
      </ADrawer>
    );
  }
);

export default PurchaseOrderOperateDrawer;
