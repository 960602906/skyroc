import { useQuery } from '@tanstack/react-query';

import { EnableStatusFormItem } from '@/features/crud';
import { useFormRules } from '@/features/form';
import { fetchGetCustomerTagTree } from '@/service/api';
import { QUERY_KEYS } from '@/service/keys';

type TagTreeNode = Api.CustomerTag.Entity & { children?: TagTreeNode[] };

type RuleKey = 'code' | 'name' | 'sort';

const CustomerTagOperateDrawer: FC<Page.OperateDrawerProps> = memo(
  ({ form, handleSubmit, onClose, open, operateType }) => {
    const { t } = useTranslation();

    const { defaultRequiredRule } = useFormRules();

    const { data: tagTree } = useQuery({
      enabled: open,
      queryFn: () => fetchGetCustomerTagTree(),
      queryKey: QUERY_KEYS.BASE.CUSTOMER_TAGS
    });

    const rules: Record<RuleKey, App.Global.FormRule> = {
      code: defaultRequiredRule,
      name: defaultRequiredRule,
      sort: defaultRequiredRule
    };

    return (
      <ADrawer
        open={open}
        title={operateType === 'add' ? t('page.customer.tag.addTag') : t('page.customer.tag.editTag')}
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
          />

          <AForm.Item
            label={t('page.customer.tag.name')}
            name="name"
            rules={[rules.name]}
          >
            <AInput placeholder={t('page.customer.tag.form.name')} />
          </AForm.Item>

          <AForm.Item
            label={t('page.customer.tag.code')}
            name="code"
            rules={[rules.code]}
          >
            <AInput placeholder={t('page.customer.tag.form.code')} />
          </AForm.Item>

          <AForm.Item
            label={t('page.customer.tag.parentId')}
            name="parentId"
          >
            <ATreeSelect
              allowClear
              showSearch
              treeDefaultExpandAll
              fieldNames={{ children: 'children', label: 'name', value: 'id' }}
              placeholder={t('page.customer.tag.form.parentId')}
              treeData={(tagTree ?? []) as TagTreeNode[]}
              treeNodeFilterProp="name"
            />
          </AForm.Item>

          <AForm.Item
            label={t('page.customer.tag.sort')}
            name="sort"
            rules={[rules.sort]}
          >
            <AInputNumber
              className="w-full"
              min={0}
              placeholder={t('page.customer.tag.form.sort')}
            />
          </AForm.Item>

          <AForm.Item
            label={t('page.customer.tag.remark')}
            name="remark"
          >
            <AInput.TextArea
              placeholder={t('page.customer.tag.form.remark')}
              rows={3}
            />
          </AForm.Item>

          <EnableStatusFormItem label={t('page.customer.tag.status')} />
        </AForm>
      </ADrawer>
    );
  }
);

export default CustomerTagOperateDrawer;
