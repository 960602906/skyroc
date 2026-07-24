import type { DescriptionsProps, TableColumnsType } from 'antd';
import { Card, Descriptions, Divider, Table, Tooltip } from 'antd';
import { useTranslation } from 'react-i18next';

import { displayDate, displayDateTime, displayText } from '@/features/crud';

interface SalesReturnStockInDetailViewProps {
  detail: Api.StockIn.Entity;
}

/** 销售退货入库详情内容：基本信息、审核信息和商品明细。 */
const SalesReturnStockInDetailView = ({ detail }: SalesReturnStockInDetailViewProps) => {
  const { t } = useTranslation();

  const basicItems: DescriptionsProps['items'] = [
    { children: displayText(detail.inNo), key: 'inNo', label: t('page.storage.in.inNo') },
    { children: displayText(detail.wareName), key: 'wareName', label: t('page.storage.in.ware') },
    {
      children: displayDateTime(detail.inTime),
      key: 'inTime',
      label: t('page.storage.in.inTime')
    },
    {
      children: displayText(detail.customerName),
      key: 'customerName',
      label: t('page.storage.in.salesReturn.customer')
    },
    {
      children: detail.afterSaleId
        ? t('page.storage.in.salesReturn.sourceAfterSale')
        : t('page.storage.in.salesReturn.sourceManual'),
      key: 'source',
      label: t('page.storage.in.salesReturn.source')
    },
    {
      children: displayText(detail.departmentName),
      key: 'departmentName',
      label: t('page.storage.in.salesReturn.department')
    }
  ];

  const auditItems: DescriptionsProps['items'] = [
    {
      children: displayText(detail.auditUserName),
      key: 'auditUserName',
      label: t('page.storage.in.salesReturn.auditUserName')
    },
    {
      children: displayDateTime(detail.auditTime),
      key: 'auditTime',
      label: t('page.storage.in.salesReturn.auditTime')
    },
    {
      children: displayText(detail.reverseUserName),
      key: 'reverseUserName',
      label: t('page.storage.in.salesReturn.reverseUserName')
    },
    {
      children: displayDateTime(detail.reverseTime),
      key: 'reverseTime',
      label: t('page.storage.in.salesReturn.reverseTime')
    },
    {
      children: displayDateTime(detail.createTime),
      key: 'createTime',
      label: t('page.storage.in.salesReturn.createTime')
    },
    {
      children: displayDateTime(detail.updateTime),
      key: 'updateTime',
      label: t('page.storage.in.salesReturn.updateTime')
    },
    {
      children: displayText(detail.remark),
      key: 'remark',
      label: t('page.storage.in.remark'),
      span: 'filled'
    }
  ];

  const detailColumns: TableColumnsType<Api.StockIn.StockInDetail> = [
    {
      dataIndex: 'pickupTaskNo',
      ellipsis: true,
      key: 'pickupTaskNo',
      render: value => displayText(value),
      title: t('page.storage.in.salesReturn.taskNo'),
      width: 160
    },
    {
      dataIndex: 'goodsName',
      ellipsis: true,
      key: 'goodsName',
      render: value => displayText(value),
      title: t('page.storage.in.salesReturn.goodsName'),
      width: 170
    },
    {
      dataIndex: 'goodsCode',
      ellipsis: true,
      key: 'goodsCode',
      render: value => displayText(value),
      title: t('page.storage.in.salesReturn.goodsCode'),
      width: 160
    },
    {
      align: 'center',
      dataIndex: 'goodsUnitName',
      key: 'goodsUnitName',
      render: value => displayText(value),
      title: t('page.storage.in.salesReturn.goodsUnitName'),
      width: 110
    },
    {
      align: 'right',
      dataIndex: 'quantity',
      key: 'quantity',
      title: t('page.storage.in.salesReturn.quantity'),
      width: 120
    },
    {
      align: 'right',
      dataIndex: 'unitPrice',
      key: 'unitPrice',
      render: value => (value as number).toFixed(4),
      title: t('page.storage.in.salesReturn.unitPrice'),
      width: 120
    },
    {
      align: 'right',
      dataIndex: 'totalPrice',
      key: 'totalPrice',
      render: value => (value as number).toFixed(4),
      title: t('page.storage.in.salesReturn.totalPrice'),
      width: 120
    },
    {
      align: 'center',
      dataIndex: 'batchNo',
      key: 'batchNo',
      render: value => displayText(value),
      title: t('page.storage.in.salesReturn.batchNo')
      // width: 140
    },
    {
      align: 'center',
      dataIndex: 'productDate',
      key: 'productDate',
      render: value => displayDate(value),
      title: t('page.storage.in.salesReturn.productDate'),
      width: 120
    },
    {
      align: 'center',
      dataIndex: 'expireDate',
      key: 'expireDate',
      render: value => displayDate(value),
      title: t('page.storage.in.salesReturn.expireDate'),
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
        title={t('page.storage.in.salesReturn.sectionBasic')}
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
          title={t('page.storage.in.salesReturn.sectionAudit')}
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
          scroll={{ x: 'max-content' }}
          size="small"
          tableLayout="fixed"
          summary={() => (
            <Table.Summary fixed>
              <Table.Summary.Row>
                <Table.Summary.Cell
                  colSpan={6}
                  index={0}
                >
                  <strong>{t('page.storage.in.salesReturn.totalAmount')}</strong>
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

export default SalesReturnStockInDetailView;
