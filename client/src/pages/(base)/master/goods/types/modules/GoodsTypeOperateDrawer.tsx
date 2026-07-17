import { useQuery } from '@tanstack/react-query';

import { EnableStatusFormItem } from '@/features/crud';
import { useFormRules } from '@/features/form';
import { fetchGetGoodsTypeTree } from '@/service/api';

import { mapGoodsTypeTree } from '../../utils/tree';

type RuleKey = 'code' | 'name' | 'status';

const GoodsTypeOperateDrawer: FC<Page.OperateDrawerProps> = memo(
  ({ form, handleSubmit, onClose, open, operateType }) => {
    const { t } = useTranslation();
    const { defaultRequiredRule } = useFormRules();

    const { data: treeData } = useQuery({
      enabled: open,
      queryFn: fetchGetGoodsTypeTree,
      queryKey: ['goods', 'typeTree'],
      staleTime: 60_000
    });

    const treeOptions = useMemo(() => mapGoodsTypeTree(treeData), [treeData]);

    // 树数据异步到达后，重新写入 parentId，确保 TreeSelect 能解析出上级名称
    useEffect(() => {
      if (!open || !treeOptions.length) {
        return;
      }

      const parentId = form.getFieldValue('parentId') as string | null | undefined;
      if (parentId) {
        form.setFieldsValue({ parentId });
      }
    }, [form, open, treeOptions]);

    const rules: Record<RuleKey, App.Global.FormRule> = {
      code: defaultRequiredRule,
      name: defaultRequiredRule,
      status: defaultRequiredRule
    };

    return (
      <ADrawer
        open={open}
        title={operateType === 'add' ? t('page.goods.type.add') : t('page.goods.type.edit')}
        width={520}
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
          layout="vertical"
        >
          <AForm.Item
            hidden
            name="id"
          >
            <AInput />
          </AForm.Item>

          <AForm.Item
            label={t('page.goods.type.name')}
            name="name"
            rules={[rules.name]}
          >
            <AInput placeholder={t('page.goods.type.form.name')} />
          </AForm.Item>

          <AForm.Item
            label={t('page.goods.type.code')}
            name="code"
            rules={[rules.code]}
          >
            <AInput placeholder={t('page.goods.type.form.code')} />
          </AForm.Item>

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
              treeNodeFilterProp="title"
            />
          </AForm.Item>

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

          <AForm.Item
            label={t('page.goods.type.isTaxExempt')}
            name="isTaxExempt"
            valuePropName="checked"
          >
            <ASwitch />
          </AForm.Item>

          <AForm.Item
            label={t('page.goods.type.taxCategoryCode')}
            name="taxCategoryCode"
          >
            <AInput placeholder={t('page.goods.type.form.taxCategoryCode')} />
          </AForm.Item>

          <AForm.Item
            label={t('page.goods.type.taxCategoryName')}
            name="taxCategoryName"
          >
            <AInput placeholder={t('page.goods.type.form.taxCategoryName')} />
          </AForm.Item>

          <AForm.Item
            label={t('page.goods.type.invoiceGoodsShortName')}
            name="invoiceGoodsShortName"
          >
            <AInput placeholder={t('page.goods.type.form.invoiceGoodsShortName')} />
          </AForm.Item>

          <AForm.Item
            label={t('page.goods.type.taxPolicyBasis')}
            name="taxPolicyBasis"
          >
            <AInput placeholder={t('page.goods.type.form.taxPolicyBasis')} />
          </AForm.Item>

          <AForm.Item
            label={t('page.goods.type.remark')}
            name="remark"
          >
            <AInput.TextArea
              placeholder={t('page.goods.type.form.remark')}
              rows={2}
            />
          </AForm.Item>

          <EnableStatusFormItem label={t('page.goods.type.status')} />
        </AForm>
      </ADrawer>
    );
  }
);

export default GoodsTypeOperateDrawer;
