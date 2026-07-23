import RemoteOptionSelect from '@/components/RemoteOptionSelect';
import { useFormRules } from '@/features/form';
import { SELECTION_OPTION_RESOURCES, useGoodsUnitsByGoodsOptions } from '@/service/hooks';

interface PurchaseStockInDetailModalProps {
  form: Page.FormInstance;
  onClose: () => void;
  onSubmit: () => void;
  open: boolean;
  operateType: AntDesign.TableOperateType;
}

/** 采购入库商品行弹窗：商品 / 单位 / 数量 / 单价 / 批次号 / 生产日期 / 备注 */
const PurchaseStockInDetailModal: FC<PurchaseStockInDetailModalProps> = memo(
  ({ form, onClose, onSubmit, open, operateType }) => {
    const { t } = useTranslation();
    const { defaultRequiredRule } = useFormRules();
    const goodsId = AForm.useWatch('goodsId', form) as string | undefined;
    const { data: units = [] } = useGoodsUnitsByGoodsOptions(open ? goodsId : undefined);

    const unitMap = useMemo(() => new Map(units.map(u => [u.value as string, u.label as string])), [units]);

    function handleGoodsUnitChange(value: string) {
      // 将单位名称存入隐藏字段，供父组件显示列使用
      form.setFieldValue('goodsUnitName', unitMap.get(value));
    }

    return (
      <AModal
        destroyOnClose
        open={open}
        width={600}
        title={
          operateType === 'add' ? t('page.storage.in.purchase.addDetail') : t('page.storage.in.purchase.editDetail')
        }
        onCancel={onClose}
        onOk={onSubmit}
      >
        <AForm
          form={form}
          layout="vertical"
        >
          <AForm.Item
            hidden
            name="id"
          />
          <AForm.Item
            hidden
            name="goodsName"
          />
          <AForm.Item
            hidden
            name="goodsCode"
          />
          <AForm.Item
            hidden
            name="goodsUnitName"
          />

          {/* 商品 */}
          <AForm.Item
            label={t('page.storage.in.purchase.goodsName')}
            name="goodsId"
            rules={[defaultRequiredRule]}
          >
            <RemoteOptionSelect
              placeholder={t('page.storage.in.form.goodsId')}
              resource={SELECTION_OPTION_RESOURCES.GOODS}
              onChange={(_value, option) => {
                const opt = Array.isArray(option) ? option[0] : option;
                const label = typeof opt?.label === 'string' ? opt.label : '';
                const [name, code] = label.split(' · ');
                form.setFieldsValue({
                  goodsCode: code?.trim() || undefined,
                  goodsName: name?.trim() || undefined,
                  goodsUnitId: undefined,
                  goodsUnitName: undefined
                });
              }}
            />
          </AForm.Item>

          <ARow gutter={16}>
            {/* 计量单位 */}
            <ACol span={12}>
              <AForm.Item
                label={t('page.storage.in.purchase.goodsUnitName')}
                name="goodsUnitId"
                rules={[defaultRequiredRule]}
              >
                <ASelect
                  disabled={!goodsId}
                  options={units}
                  placeholder={t('page.storage.in.form.goodsUnitId')}
                  onChange={handleGoodsUnitChange}
                />
              </AForm.Item>
            </ACol>

            {/* 入库数量 */}
            <ACol span={12}>
              <AForm.Item
                label={t('page.storage.in.purchase.quantity')}
                name="quantity"
                rules={[defaultRequiredRule]}
              >
                <AInputNumber
                  className="w-full"
                  min={0.000001}
                  placeholder={t('page.storage.in.form.quantity')}
                  precision={6}
                />
              </AForm.Item>
            </ACol>

            {/* 单价 */}
            <ACol span={12}>
              <AForm.Item
                label={t('page.storage.in.purchase.unitPrice')}
                name="unitPrice"
                rules={[defaultRequiredRule]}
              >
                <AInputNumber
                  className="w-full"
                  min={0}
                  placeholder={t('page.storage.in.form.unitPrice')}
                  precision={4}
                />
              </AForm.Item>
            </ACol>

            {/* 生产日期 */}
            <ACol span={12}>
              <AForm.Item
                label={t('page.storage.in.purchase.productDate')}
                name="productDate"
              >
                <ADatePicker
                  className="w-full"
                  placeholder={t('page.storage.in.form.productDate')}
                />
              </AForm.Item>
            </ACol>
          </ARow>

          {/* 备注 */}
          <AForm.Item
            className="mb-0"
            label={t('page.storage.in.remark')}
            name="remark"
          >
            <AInput.TextArea
              placeholder={t('page.storage.in.form.detailRemark')}
              rows={2}
            />
          </AForm.Item>
        </AForm>
      </AModal>
    );
  }
);

export default PurchaseStockInDetailModal;
