import { useQuery } from '@tanstack/react-query';
import { TreeSelect } from 'antd';

import { EnableStatusFormItem } from '@/features/crud';
import { useFormRules } from '@/features/form';
import { fetchGetDepartmentTree } from '@/service/api';
import { QUERY_KEYS } from '@/service/keys';

type RuleKey = 'code' | 'name';

type DepartmentTreeNode = {
  title: string;
  value: string;
};

function flattenDepartmentTree(nodes: Api.Department.Entity[]): DepartmentTreeNode[] {
  return nodes.flatMap(node => [
    { title: node.name, value: node.id },
    ...(node.children?.length ? flattenDepartmentTree(node.children) : [])
  ]);
}

const PurchaserOperateDrawer: FC<Page.OperateDrawerProps> = memo(
  ({ form, handleSubmit, onClose, open, operateType }) => {
    const { t } = useTranslation();

    const { defaultRequiredRule } = useFormRules();

    const { data: departmentTree } = useQuery({
      queryFn: fetchGetDepartmentTree,
      queryKey: QUERY_KEYS.BASE.DEPARTMENT_TREE
    });

    const departmentOptions = useMemo(() => flattenDepartmentTree(departmentTree ?? []), [departmentTree]);

    const rules: Record<RuleKey, App.Global.FormRule> = {
      code: defaultRequiredRule,
      name: defaultRequiredRule
    };

    return (
      <ADrawer
        open={open}
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
        title={
          operateType === 'add' ? t('page.purchase.purchaser.addPurchaser') : t('page.purchase.purchaser.editPurchaser')
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
          />

          <AForm.Item
            label={t('page.purchase.purchaser.name')}
            name="name"
            rules={[rules.name]}
          >
            <AInput placeholder={t('page.purchase.purchaser.form.name')} />
          </AForm.Item>

          <AForm.Item
            label={t('page.purchase.purchaser.code')}
            name="code"
            rules={[rules.code]}
          >
            <AInput placeholder={t('page.purchase.purchaser.form.code')} />
          </AForm.Item>

          <AForm.Item
            label={t('page.purchase.purchaser.phone')}
            name="phone"
          >
            <AInput placeholder={t('page.purchase.purchaser.form.phone')} />
          </AForm.Item>

          <AForm.Item
            label={t('page.purchase.purchaser.departmentId')}
            name="departmentId"
          >
            <TreeSelect
              allowClear
              showSearch
              treeDefaultExpandAll
              placeholder={t('page.purchase.purchaser.form.departmentId')}
              treeData={departmentOptions}
              treeNodeFilterProp="title"
            />
          </AForm.Item>

          <AForm.Item
            label={t('page.purchase.purchaser.remark')}
            name="remark"
          >
            <AInput.TextArea
              placeholder={t('page.purchase.purchaser.form.remark')}
              rows={3}
            />
          </AForm.Item>

          <EnableStatusFormItem label={t('page.purchase.purchaser.status')} />
        </AForm>
      </ADrawer>
    );
  }
);

export default PurchaserOperateDrawer;
