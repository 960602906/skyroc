import { useQuery } from '@tanstack/react-query';

import { EnableStatusFormItem } from '@/features/crud';
import { useFormRules } from '@/features/form';
import { fetchGetGoodsTypeTree } from '@/service/api';

import { mapGoodsTypeTree } from '../../utils/tree';

type RuleKey = 'code' | 'name' | 'status';

const GoodsTypeOperateModal: FC<Page.OperateDrawerProps> = memo(
  ({ form, handleSubmit, onClose, open, operateType }) => {
    const { t } = useTranslation();
    const { defaultRequiredRule } = useFormRules();

    const { data: treeData } = useQuery({
      queryFn: fetchGetGoodsTypeTree,
      queryKey: ['goods', 'typeTree'],
      staleTime: 60_000
    });

    const treeOptions = useMemo(() => mapGoodsTypeTree(treeData), [treeData]);

    const rules: Record<RuleKey, App.Global.FormRule> = {
      code: defaultRequiredRule,
      name: defaultRequiredRule,
      status: defaultRequiredRule
    };

    return (
      <AModal
        destroyOnClose
        open={open}
        title={operateType === 'add' ? t('page.goods.type.add') : t('page.goods.type.edit')}
        width={720}
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

          <ARow gutter={16}>
            <ACol span={12}>
              <AForm.Item
                label={t('page.goods.type.name')}
                name="name"
                rules={[rules.name]}
              >
                <AInput placeholder={t('page.goods.type.form.name')} />
              </AForm.Item>
            </ACol>
            <ACol span={12}>
              <AForm.Item
                label={t('page.goods.type.code')}
                name="code"
                rules={[rules.code]}
              >
                <AInput placeholder={t('page.goods.type.form.code')} />
              </AForm.Item>
            </ACol>
            <ACol span={12}>
              <AForm.Item
                label={t('page.goods.type.parentId')}
                name="parentId"
              >
                <ATreeSelect
                  allowClear
                  showSearch
                  treeDefaultExpandAll
                  placeholder={t('page.goods.type.form.parentId')}
                  treeData={treeOptions}
                />
              </AForm.Item>
            </ACol>
            <ACol span={12}>
              <AForm.Item
                label={t('page.goods.type.sort')}
                name="sort"
              >
                <AInputNumber
                  className="w-full"
                  min={0}
                  placeholder={t('page.goods.type.form.sort')}
                />
              </AForm.Item>
            </ACol>
            <ACol span={12}>
              <AForm.Item
                label={t('page.goods.type.defaultTaxRate')}
                name="defaultTaxRate"
              >
                <AInputNumber
                  className="w-full"
                  max={100}
                  min={0}
                  placeholder={t('page.goods.type.form.defaultTaxRate')}
                />
              </AForm.Item>
            </ACol>
            <ACol span={12}>
              <AForm.Item
                label={t('page.goods.type.isTaxExempt')}
                name="isTaxExempt"
                valuePropName="checked"
              >
                <ASwitch />
              </AForm.Item>
            </ACol>
            <ACol span={12}>
              <AForm.Item
                label={t('page.goods.type.taxCategoryCode')}
                name="taxCategoryCode"
              >
                <AInput placeholder={t('page.goods.type.form.taxCategoryCode')} />
              </AForm.Item>
            </ACol>
            <ACol span={12}>
              <AForm.Item
                label={t('page.goods.type.taxCategoryName')}
                name="taxCategoryName"
              >
                <AInput placeholder={t('page.goods.type.form.taxCategoryName')} />
              </AForm.Item>
            </ACol>
            <ACol span={12}>
              <AForm.Item
                label={t('page.goods.type.invoiceGoodsShortName')}
                name="invoiceGoodsShortName"
              >
                <AInput placeholder={t('page.goods.type.form.invoiceGoodsShortName')} />
              </AForm.Item>
            </ACol>
            <ACol span={24}>
              <AForm.Item
                label={t('page.goods.type.taxPolicyBasis')}
                name="taxPolicyBasis"
              >
                <AInput placeholder={t('page.goods.type.form.taxPolicyBasis')} />
              </AForm.Item>
            </ACol>
            <ACol span={24}>
              <AForm.Item
                label={t('page.goods.type.remark')}
                name="remark"
              >
                <AInput.TextArea
                  placeholder={t('page.goods.type.form.remark')}
                  rows={2}
                />
              </AForm.Item>
            </ACol>
            <ACol span={24}>
              <EnableStatusFormItem label={t('page.goods.type.status')} />
            </ACol>
          </ARow>
        </AForm>
      </AModal>
    );
  }
);

export default GoodsTypeOperateModal;
