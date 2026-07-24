import { useQuery } from '@tanstack/react-query';
import dayjs from 'dayjs';

import RemoteOptionSelect from '@/components/RemoteOptionSelect';
import { PICKER_FORMATS } from '@/constants/datetime';
import { useFormRules } from '@/features/form';
import { fetchGetGoodsDetail, fetchGetGoodsUnitsByGoods } from '@/service/api';
import { SELECTION_OPTION_RESOURCES } from '@/service/hooks';
import { QUERY_KEYS } from '@/service/keys';

interface PurchaseOrderDetailLineModalProps {
  form: Page.FormInstance;
  onClose: () => void;
  onSubmit: () => void;
  open: boolean;
  operateType: AntDesign.TableOperateType;
}

/** 采购单商品明细弹窗：商品 / 单位 / 数量 / 单价 / 生产日期 */
const PurchaseOrderDetailLineModal: FC<PurchaseOrderDetailLineModalProps> = memo(
  ({ form, onClose, onSubmit, open, operateType }) => {
    const { t } = useTranslation();
    const { defaultRequiredRule } = useFormRules();
    const goodsId = AForm.useWatch('goodsId', form) as string | undefined;

    const { data: unitEntities = [] } = useQuery({
      enabled: open && Boolean(goodsId),
      queryFn: () => fetchGetGoodsUnitsByGoods(goodsId!),
      queryKey: QUERY_KEYS.BASE.GOODS_UNITS_BY_GOODS(goodsId ?? ''),
      staleTime: 30_000
    });

    const unitOptions = unitEntities.map(item => ({
      label: item.name ?? item.code ?? item.id,
      value: item.id
    }));

    const unitMap = useMemo(() => new Map(unitEntities.map(item => [item.id, item])), [unitEntities]);
    const prevGoodsIdRef = useRef<string | undefined>(undefined);

    useEffect(() => {
      if (!open) {
        prevGoodsIdRef.current = undefined;
        return;
      }
      if (prevGoodsIdRef.current !== undefined && prevGoodsIdRef.current !== goodsId) {
        form.setFieldsValue({ purchaseUnitId: undefined, purchaseUnitName: undefined });
      }
      prevGoodsIdRef.current = goodsId;
    }, [form, goodsId, open]);

    async function handleGoodsChange(value: string) {
      const selected = await fetchGetGoodsDetail(value);
      form.setFieldsValue({ goodsCode: selected.code, goodsName: selected.name });
    }

    function handleUnitChange(value: string) {
      const unit = unitMap.get(value);
      form.setFieldsValue({ purchaseUnitName: unit?.name });
    }

    return (
      <AModal
        destroyOnClose
        open={open}
        width={600}
        title={
          operateType === 'add'
            ? t('page.purchase.order.operate.addDetail')
            : t('page.purchase.order.operate.editDetail')
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
            name="purchaseUnitName"
          />

          <AForm.Item
            label={t('page.purchase.order.goodsName')}
            name="goodsId"
            rules={[defaultRequiredRule]}
          >
            <RemoteOptionSelect
              placeholder={t('page.purchase.order.form.goodsId')}
              resource={SELECTION_OPTION_RESOURCES.GOODS}
              onChange={handleGoodsChange}
            />
          </AForm.Item>

          <ARow gutter={16}>
            <ACol span={12}>
              <AForm.Item
                label={t('page.purchase.order.unit')}
                name="purchaseUnitId"
                rules={[defaultRequiredRule]}
              >
                <ASelect
                  disabled={!goodsId}
                  options={unitOptions}
                  placeholder={t('page.purchase.order.form.purchaseUnitId')}
                  onChange={handleUnitChange}
                />
              </AForm.Item>
            </ACol>
            <ACol span={12}>
              <AForm.Item
                label={t('page.purchase.order.purchaseQuantity')}
                name="purchaseQuantity"
                rules={[defaultRequiredRule]}
              >
                <AInputNumber
                  className="w-full"
                  min={0.000001}
                  placeholder={t('page.purchase.order.form.purchaseQuantity')}
                />
              </AForm.Item>
            </ACol>
            <ACol span={12}>
              <AForm.Item
                label={t('page.purchase.order.purchasePrice')}
                name="purchasePrice"
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
            <ACol span={12}>
              <AForm.Item
                getValueFromEvent={(_, dateString) => dateString || null}
                getValueProps={value => ({ value: value ? dayjs(value) : undefined })}
                label={t('page.purchase.order.productDate')}
                name="productDate"
              >
                <ADatePicker
                  className="w-full"
                  format={PICKER_FORMATS.DATE}
                  placeholder={t('page.purchase.order.form.productDate')}
                />
              </AForm.Item>
            </ACol>
            <ACol span={12}>
              <AForm.Item
                className="mb-0"
                label={t('page.purchase.order.requiredQuantity')}
                name="requiredQuantity"
              >
                <AInputNumber
                  className="w-full"
                  min={0}
                  placeholder={t('page.purchase.order.form.requiredQuantity')}
                />
              </AForm.Item>
            </ACol>
          </ARow>

          <AForm.Item
            className="mb-0"
            label={t('page.purchase.order.remark')}
            name="remark"
          >
            <AInput.TextArea
              placeholder={t('page.purchase.order.form.remark')}
              rows={2}
            />
          </AForm.Item>
        </AForm>
      </AModal>
    );
  }
);

export default PurchaseOrderDetailLineModal;
