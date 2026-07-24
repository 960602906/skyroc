import type { DescriptionsProps, TableColumnsType } from 'antd';
import { Card, Descriptions, Divider, Table, Tooltip } from 'antd';
import { useTranslation } from 'react-i18next';

import { displayDate, displayDateTime, displayText } from '@/features/crud';

interface OtherStockInDetailViewProps {
  detail: Api.StockIn.Entity;
}

/** 其他入库详情内容：基本信息、审核信息和商品明细。 */
const OtherStockInDetailView = ({ detail }: OtherStockInDetailViewProps) => {
  const { t } = useTranslation();

  /** 基本信息项 */
  const basicItems: DescriptionsProps['items'] = [
    { children: displayText(detail.inNo), key: 'inNo', label: t('page.storage.in.inNo') },
    { children: displayText(detail.wareName), key: 'wareName', label: t('page.storage.in.ware') },
    {
      children: displayDateTime(detail.inTime),
      key: 'inTime',
      label: t('page.storage.in.inTime')
    },
    {
      children: displayText(detail.departmentName),
      key: 'departmentName',
      label: t('page.storage.in.other.department')
    }
  ];

  /** 审核信息项 */
  const auditItems: DescriptionsProps['items'] = [
    {
      children: displayText(detail.auditUserName),
      key: 'auditUserName',
      label: t('page.storage.in.other.auditUserName')
    },
    {
      children: displayDateTime(detail.auditTime),
      key: 'auditTime',
      label: t('page.storage.in.other.auditTime')
    },
    {
      children: displayText(detail.reverseUserName),
      key: 'reverseUserName',
      label: t('page.storage.in.other.reverseUserName')
    },
    {
      children: displayDateTime(detail.reverseTime),
      key: 'reverseTime',
      label: t('page.storage.in.other.reverseTime')
    },
    {
      children: displayDateTime(detail.createTime),
      key: 'createTime',
      label: t('page.storage.in.other.createTime')
    },
    {
      children: displayDateTime(detail.updateTime),
      key: 'updateTime',
      label: t('page.storage.in.other.updateTime')
    },
    {
      children: displayText(detail.remark),
      key: 'remark',
      label: t('page.storage.in.remark'),
      span: 'filled'
    }
  ];

  /** 商品明细列定义 */
  const detailColumns: TableColumnsType<Api.StockIn.StockInDetail> = [
    {
      dataIndex: 'goodsName',
      ellipsis: true,
      key: 'goodsName',
      render: value => displayText(value),
      title: t('page.storage.in.other.goodsName'),
      width: 170
    },
    {
      dataIndex: 'goodsCode',
      ellipsis: true,
      key: 'goodsCode',
      render: value => displayText(value),
      title: t('page.storage.in.other.goodsCode'),
      width: 160
    },
    {
      align: 'center',
      dataIndex: 'goodsUnitName',
      key: 'goodsUnitName',
      render: value => displayText(value),
      title: t('page.storage.in.other.goodsUnitName'),
      width: 110
    },
    {
      align: 'right',
      dataIndex: 'quantity',
      key: 'quantity',
      title: t('page.storage.in.other.quantity'),
      width: 120
    },
    {
      align: 'right',
      dataIndex: 'unitPrice',
      key: 'unitPrice',
      render: value => (value as number).toFixed(4),
      title: t('page.storage.in.other.unitPrice'),
      width: 120
    },
    {
      align: 'right',
      dataIndex: 'totalPrice',
      key: 'totalPrice',
      render: value => (value as number).toFixed(4),
      title: t('page.storage.in.other.totalPrice'),
      width: 120
    },
    {
      align: 'center',
      dataIndex: 'batchNo',
      key: 'batchNo',
      render: value => displayText(value),
      title: t('page.storage.in.other.batchNo'),
      width: 140
    },
    {
      align: 'center',
      dataIndex: 'productDate',
      key: 'productDate',
      render: value => displayDate(value),
      title: t('page.storage.in.other.productDate'),
      width: 120
    },
    {
      align: 'center',
      dataIndex: 'expireDate',
      key: 'expireDate',
      render: value => displayDate(value),
      title: t('page.storage.in.other.expireDate'),
      width: 120
    },
    {
      dataIndex: 'remark',
      ellipsis: { showTitle: false },
      key: 'remark',
      render: value => {
        const remark = displayText(value);
        return (
          <Tooltip title={remark}>
            <span className="block truncate">{remark}</span>
          </Tooltip>
        );
      },
      title: t('page.storage.in.remark'),
      width: 220
    }
  ];

  return (
    <>
      <Card
        className="card-wrapper"
        title={t('page.storage.in.other.sectionBasic')}
        variant="borderless"
      >
        <Descriptions
          column={{ lg: 3, md: 2, sm: 1, xs: 1 }}
          items={basicItems}
          size="middle"
        />
        <Divider />
        <Descriptions
          column={{ lg: 3, md: 2, sm: 1, xs: 1 }}
          items={auditItems}
          size="middle"
          title={t('page.storage.in.other.sectionAudit')}
        />
      </Card>

      <Card
        className="card-wrapper"
        title={t('page.storage.in.details')}
        variant="borderless"
      >
        <Table
          columns={detailColumns}
          dataSource={detail.details}
          pagination={false}
          rowKey="id"
          scroll={{ x: 1500 }}
          size="small"
          tableLayout="fixed"
          summary={() => (
            <Table.Summary fixed>
              <Table.Summary.Row>
                <Table.Summary.Cell
                  colSpan={5}
                  index={0}
                >
                  <strong>{t('page.storage.in.other.totalAmount')}</strong>
                </Table.Summary.Cell>
                <Table.Summary.Cell
                  align="right"
                  index={1}
                >
                  <strong>{detail.totalAmount.toFixed(4)}</strong>
                </Table.Summary.Cell>
                <Table.Summary.Cell
                  colSpan={4}
                  index={2}
                />
              </Table.Summary.Row>
            </Table.Summary>
          )}
        />
      </Card>
    </>
  );
};

export default OtherStockInDetailView;
