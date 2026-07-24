import RemoteOptionSelect from '@/components/RemoteOptionSelect';
import { useFormRules } from '@/features/form';
import { SELECTION_OPTION_RESOURCES, useGoodsUnitsByGoodsOptions } from '@/service/hooks';

interface OtherStockInDetailModalProps {
  form: Page.FormInstance;
  onClose: () => void;
  onSubmit: () => void;
  open: boolean;
  operateType: AntDesign.TableOperateType;
}

/** 其他入库商品行弹窗：商品 / 单位 / 数量 / 单价 / 生产日期 / 到期日 / 备注 */
const OtherStockInDetailModal: FC<OtherStockInDetailModalProps> = memo(
  ({ form, onClose, onSubmit, open, operateType }) => {
    const { t } = useTranslation();
    const { defaultRequiredRule } = useFormRules();
    const goodsId = AForm.useWatch('goodsId', form) as string | undefined;
    const { data: units = [] } = useGoodsUnitsByGoodsOptions(open ? goodsId : undefined);

    const unitMap = useMemo(() => new Map(units.map(u => [u.value as string, u.label as string])), [units]);

    function handleGoodsUnitChange(value: string) {
      form.setFieldValue('goodsUnitName', unitMap.get(value));
    }

    return (
      <AModal
        destroyOnClose
        open={open}
        title={operateType === 'add' ? t('page.storage.in.other.addDetail') : t('page.storage.in.other.editDetail')}
        width={600}
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

          <AForm.Item
            label={t('page.storage.in.other.goodsName')}
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
            <ACol span={12}>
              <AForm.Item
                label={t('page.storage.in.other.goodsUnitName')}
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

            <ACol span={12}>
              <AForm.Item
                label={t('page.storage.in.other.quantity')}
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

            <ACol span={12}>
              <AForm.Item
                label={t('page.storage.in.other.unitPrice')}
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

            <ACol span={12}>
              <AForm.Item
                label={t('page.storage.in.other.productDate')}
                name="productDate"
              >
                <ADatePicker
                  className="w-full"
                  placeholder={t('page.storage.in.form.productDate')}
                />
              </AForm.Item>
            </ACol>

            <ACol span={12}>
              <AForm.Item
                label={t('page.storage.in.other.expireDate')}
                name="expireDate"
              >
                <ADatePicker
                  className="w-full"
                  placeholder={t('page.storage.in.form.expireDate')}
                />
              </AForm.Item>
            </ACol>
          </ARow>

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

export default OtherStockInDetailModal;
