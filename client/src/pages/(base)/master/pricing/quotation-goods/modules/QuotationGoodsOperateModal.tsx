import { useFormRules } from '@/features/form';
import { toOptions, useGoodsOptions, useGoodsUnitsByGoodsOptions, useQuotationOptions } from '@/service/hooks';

type RuleKey = 'goodsId' | 'goodsUnitId' | 'quotationId' | 'unitPrice';

type QuotationGoodsOperateModalProps = Page.OperateDrawerProps & {
  /** 固定报价单时隐藏报价单选择，由表单 quotationId 字段承载 */
  lockQuotationId?: boolean;
};

const QuotationGoodsOperateModal: FC<QuotationGoodsOperateModalProps> = memo(
  ({ form, handleSubmit, lockQuotationId = false, onClose, open, operateType }) => {
    const { t } = useTranslation();
    const { defaultRequiredRule } = useFormRules();
    const { data: quotationOptions = [] } = useQuotationOptions();
    const { data: goods } = useGoodsOptions();
    const goodsOptions = toOptions(goods);

    const goodsId = AForm.useWatch('goodsId', form);
    const { data: goodsUnitOptions = [] } = useGoodsUnitsByGoodsOptions(goodsId);
    const prevGoodsIdRef = useRef<string | undefined>(undefined);

    // 仅在用户切换商品时清空单位，避免编辑回填被冲掉
    useEffect(() => {
      if (!open) {
        prevGoodsIdRef.current = undefined;
        return;
      }

      if (prevGoodsIdRef.current !== undefined && prevGoodsIdRef.current !== goodsId) {
        form.setFieldValue('goodsUnitId', undefined);
      }

      prevGoodsIdRef.current = goodsId;
    }, [form, goodsId, open]);

    const rules: Record<RuleKey, App.Global.FormRule> = {
      goodsId: defaultRequiredRule,
      goodsUnitId: defaultRequiredRule,
      quotationId: defaultRequiredRule,
      unitPrice: defaultRequiredRule
    };

    return (
      <AModal
        destroyOnClose
        open={open}
        title={operateType === 'add' ? t('page.goods.quotationGoods.add') : t('page.goods.quotationGoods.edit')}
        width={640}
        onCancel={onClose}
        onOk={handleSubmit}
      >
        <AForm
          form={form}
          layout="vertical"
        >
          <AForm.Item
            hidden
            name="id"
          />

          {lockQuotationId ? (
            <AForm.Item
              hidden
              name="quotationId"
              rules={[rules.quotationId]}
            />
          ) : (
            <AForm.Item
              label={t('page.goods.quotationGoods.quotationId')}
              name="quotationId"
              rules={[rules.quotationId]}
            >
              <ASelect
                options={quotationOptions}
                placeholder={t('page.goods.quotationGoods.form.quotationId')}
              />
            </AForm.Item>
          )}

          <AForm.Item
            label={t('page.goods.quotationGoods.goodsId')}
            name="goodsId"
            rules={[rules.goodsId]}
          >
            <ASelect
              options={goodsOptions}
              placeholder={t('page.goods.quotationGoods.form.goodsId')}
            />
          </AForm.Item>

          <AForm.Item
            label={t('page.goods.quotationGoods.goodsUnitId')}
            name="goodsUnitId"
            rules={[rules.goodsUnitId]}
          >
            <ASelect
              disabled={!goodsId}
              options={goodsUnitOptions}
              placeholder={t('page.goods.quotationGoods.form.goodsUnitId')}
            />
          </AForm.Item>

          <AForm.Item
            label={t('page.goods.quotationGoods.unitPrice')}
            name="unitPrice"
            rules={[rules.unitPrice]}
          >
            <AInputNumber
              className="w-full"
              min={0}
              placeholder={t('page.goods.quotationGoods.form.unitPrice')}
            />
          </AForm.Item>

          <AForm.Item
            label={t('page.goods.quotationGoods.minOrderQuantity')}
            name="minOrderQuantity"
          >
            <AInputNumber
              className="w-full"
              min={0}
              placeholder={t('page.goods.quotationGoods.form.minOrderQuantity')}
            />
          </AForm.Item>

          <AForm.Item
            label={t('page.goods.quotationGoods.isOnSale')}
            name="isOnSale"
            valuePropName="checked"
          >
            <ASwitch />
          </AForm.Item>

          <AForm.Item
            label={t('page.goods.quotationGoods.remark')}
            name="remark"
          >
            <AInput.TextArea
              placeholder={t('page.goods.quotationGoods.form.remark')}
              rows={2}
            />
          </AForm.Item>
        </AForm>
      </AModal>
    );
  }
);

export default QuotationGoodsOperateModal;
