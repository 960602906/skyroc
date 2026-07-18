import { Col, Form, Input, Row } from 'antd';

import { EnableStatusSelect, SearchActionsCol, UserGenderSelect } from '@/features/crud';
import { useFormRules } from '@/features/form';

const UserSearch: FC<Page.SearchProps> = memo(({ form, reset, search, searchParams }) => {
  const { t } = useTranslation();
  const {
    patternRules: { email, phone }
  } = useFormRules();

  return (
    <Form
      form={form}
      initialValues={searchParams}
      labelCol={{
        md: 7,
        span: 5
      }}
    >
      <Row
        wrap
        gutter={[16, 16]}
      >
        <Col
          lg={6}
          md={12}
          span={24}
        >
          <Form.Item
            className="m-0"
            label={t('page.manage.user.userName')}
            name="userName"
          >
            <Input placeholder={t('page.manage.user.form.userName')} />
          </Form.Item>
        </Col>

        <Col
          lg={6}
          md={12}
          span={24}
        >
          <Form.Item
            className="m-0"
            label={t('page.manage.user.userGender')}
            name="gender"
          >
            <UserGenderSelect placeholder={t('page.manage.user.form.userGender')} />
          </Form.Item>
        </Col>

        <Col
          lg={6}
          md={12}
          span={24}
        >
          <Form.Item
            className="m-0"
            label={t('page.manage.user.nickName')}
            name="nickName"
          >
            <Input placeholder={t('page.manage.user.form.nickName')} />
          </Form.Item>
        </Col>

        <Col
          lg={6}
          md={12}
          span={24}
        >
          <Form.Item
            className="m-0"
            label={t('page.manage.user.userPhone')}
            name="userPhone"
            rules={[phone]}
          >
            <Input placeholder={t('page.manage.user.form.userPhone')} />
          </Form.Item>
        </Col>

        <Col
          lg={6}
          md={12}
          span={24}
        >
          <Form.Item
            className="m-0"
            label={t('page.manage.user.userEmail')}
            name="userEmail"
            rules={[email]}
          >
            <Input placeholder={t('page.manage.user.form.userEmail')} />
          </Form.Item>
        </Col>

        <Col
          lg={6}
          md={12}
          span={24}
        >
          <Form.Item
            className="m-0"
            label={t('page.manage.user.userStatus')}
            name="status"
          >
            <EnableStatusSelect placeholder={t('page.manage.user.form.userStatus')} />
          </Form.Item>
        </Col>

        <SearchActionsCol
          fieldCount={6}
          onReset={reset}
          onSearch={search}
        />
      </Row>
    </Form>
  );
});

export default UserSearch;
