import RemoteOptionSelect from '@/components/RemoteOptionSelect';
import { useFormRules } from '@/features/form';
import { fetchGetGoodsDetail } from '@/service/api';
import { SELECTION_OPTION_RESOURCES, useGoodsUnitsByGoodsOptions } from '@/service/hooks';

interface PurchasePlanDetailLineModalProps {
  form: Page.FormInstance;
  onClose: () => void;
  onSubmit: () => void;
  open: boolean;
  operateType: AntDesign.TableOperateType;
}

/** 采购计划明细行弹窗：商品 / 采购单位 / 计划数量 */
const PurchasePlanDetailLineModal: FC<PurchasePlanDetailLineModalProps> = memo(
  ({ form, onClose, onSubmit, open, operateType }) => {
    const { t } = useTranslation();
    const { defaultRequiredRule } = useFormRules();
    const goodsId = AForm.useWatch('goodsId', form) as string | undefined;
    const { data: units = [] } = useGoodsUnitsByGoodsOptions(open ? goodsId : null);

    const prevGoodsIdRef = useRef<string | undefined>(undefined);

    useEffect(() => {
      if (!open) {
        prevGoodsIdRef.current = undefined;
        return;
      }
      if (prevGoodsIdRef.current !== undefined && prevGoodsIdRef.current !== goodsId) {
        form.setFieldsValue({
          goodsCode: undefined,
          goodsName: undefined,
          purchaseUnitId: undefined,
          purchaseUnitName: undefined
        });
      }
      prevGoodsIdRef.current = goodsId;
    }, [form, goodsId, open]);

    async function handleGoodsChange(value: string) {
      const goods = await fetchGetGoodsDetail(value);
      form.setFieldsValue({
        goodsCode: goods.code,
        goodsName: goods.name
      });
    }

    return (
      <AModal
        destroyOnClose
        open={open}
        width={520}
        title={
          operateType === 'add' ? t('page.purchase.plan.operate.addDetail') : t('page.purchase.plan.operate.editDetail')
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
            name="goodsName"
          />
          <AForm.Item
            hidden
            name="goodsCode"
          />
          <AForm.Item
            hidden
            name="purchaseUnitName"
          />

          <AForm.Item
            label={t('page.purchase.plan.goods')}
            name="goodsId"
            rules={[defaultRequiredRule]}
          >
            <RemoteOptionSelect
              placeholder={t('page.purchase.plan.form.goodsId')}
              resource={SELECTION_OPTION_RESOURCES.GOODS}
              onChange={handleGoodsChange}
            />
          </AForm.Item>

          <ARow gutter={16}>
            <ACol span={12}>
              <AForm.Item
                label={t('page.purchase.plan.unit')}
                name="purchaseUnitId"
                rules={[defaultRequiredRule]}
              >
                <ASelect
                  disabled={!goodsId}
                  options={units}
                  placeholder={t('page.purchase.plan.form.purchaseUnitId')}
                  onChange={(value: string) => {
                    const unit = units.find(u => u.value === value);
                    form.setFieldsValue({ purchaseUnitName: unit?.label });
                  }}
                />
              </AForm.Item>
            </ACol>
            <ACol span={12}>
              <AForm.Item
                label={t('page.purchase.plan.plannedQuantity')}
                name="plannedQuantity"
                rules={[defaultRequiredRule]}
              >
                <AInputNumber
                  className="w-full"
                  min={0.000001}
                  placeholder={t('page.purchase.plan.form.plannedQuantity')}
                />
              </AForm.Item>
            </ACol>
          </ARow>

          <AForm.Item
            className="mb-0"
            label={t('page.purchase.plan.remark')}
            name="remark"
          >
            <AInput.TextArea
              placeholder={t('page.purchase.plan.form.remark')}
              rows={2}
            />
          </AForm.Item>
        </AForm>
      </AModal>
    );
  }
);

export default PurchasePlanDetailLineModal;
