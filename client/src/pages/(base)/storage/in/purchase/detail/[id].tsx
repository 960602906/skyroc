import type { DescriptionsProps, TableColumnsType } from 'antd';
import { Button, Card, Descriptions, Divider, Flex, Table, Tooltip } from 'antd';
import { useTranslation } from 'react-i18next';
import { type LoaderFunctionArgs, redirect, useLoaderData } from 'react-router-dom';

import { displayDate, displayDateTime, displayText, renderPurchasePattern } from '@/features/crud';
import { renderStockDocumentStatus } from '@/features/crud/render-status';
import { useCloseTabAndNavigate } from '@/features/tab';
import { fetchGetStockInPurchaseDetail } from '@/service/api';

const LIST_PATH = '/storage/in/purchase';

/** 路由元信息：采购入库详情页不显示在菜单中 */
export const handle = {
  hideInMenu: true,
  i18nKey: 'route.(base)_storage_in_purchase',
  keepAlive: false
};

/** 路由切换前加载采购入库详情，入库单不存在或加载失败时返回列表。 */
export async function loader({ params }: LoaderFunctionArgs) {
  const { id } = params;
  if (!id) return redirect(LIST_PATH);

  try {
    const detail = await fetchGetStockInPurchaseDetail(id);
    return detail ?? redirect(LIST_PATH);
  } catch {
    return redirect(LIST_PATH);
  }
}

/** 采购入库基础信息、商品明细和审核轨迹详情页。 */
const PurchaseStockInDetail = () => {
  const { t } = useTranslation();
  const closeTabAndNavigate = useCloseTabAndNavigate();
  const detail = useLoaderData() as Api.StockIn.Entity;

  if (!detail) return null;

  /** 基本信息项 */
  const basicItems: DescriptionsProps['items'] = [
    { children: displayText(detail.inNo), key: 'inNo', label: t('page.storage.in.inNo') },
    {
      children: renderStockDocumentStatus(detail.businessStatus),
      key: 'businessStatus',
      label: t('page.storage.in.businessStatus')
    },
    { children: displayText(detail.wareName), key: 'wareName', label: t('page.storage.in.ware') },
    {
      children: renderPurchasePattern(detail.purchasePattern),
      key: 'purchasePattern',
      label: t('page.storage.in.purchasePattern')
    },
    {
      children: displayDateTime(detail.inTime),
      key: 'inTime',
      label: t('page.storage.in.inTime')
    },
    {
      children: displayDateTime(detail.expectedArrivalTime),
      key: 'expectedArrivalTime',
      label: t('page.storage.in.expectedArrivalTime')
    }
  ];

  /** 业务方信息项 */
  const businessItems: DescriptionsProps['items'] = [
    {
      children: displayText(detail.supplierName),
      key: 'supplierName',
      label: t('page.storage.in.supplier')
    },
    {
      children: displayText(detail.purchaserName),
      key: 'purchaserName',
      label: t('page.storage.in.purchaser')
    },
    {
      children: displayText(detail.departmentName),
      key: 'departmentName',
      label: t('page.storage.in.purchase.department')
    }
  ];

  /** 审核信息项 */
  const auditItems: DescriptionsProps['items'] = [
    {
      children: displayText(detail.auditUserName),
      key: 'auditUserName',
      label: t('page.storage.in.purchase.auditUserName')
    },
    {
      children: displayDateTime(detail.auditTime),
      key: 'auditTime',
      label: t('page.storage.in.purchase.auditTime')
    },
    {
      children: displayText(detail.reverseUserName),
      key: 'reverseUserName',
      label: t('page.storage.in.purchase.reverseUserName')
    },
    {
      children: displayDateTime(detail.reverseTime),
      key: 'reverseTime',
      label: t('page.storage.in.purchase.reverseTime')
    },
    {
      children: displayDateTime(detail.createTime),
      key: 'createTime',
      label: t('page.storage.in.purchase.createTime')
    },
    {
      children: displayDateTime(detail.updateTime),
      key: 'updateTime',
      label: t('page.storage.in.purchase.updateTime')
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
      title: t('page.storage.in.purchase.goodsName'),
      width: 170
    },
    {
      dataIndex: 'goodsCode',
      ellipsis: true,
      key: 'goodsCode',
      render: value => displayText(value),
      title: t('page.storage.in.purchase.goodsCode'),
      width: 160
    },
    {
      align: 'center',
      dataIndex: 'goodsUnitName',
      key: 'goodsUnitName',
      render: value => displayText(value),
      title: t('page.storage.in.purchase.goodsUnitName'),
      width: 110
    },
    {
      align: 'right',
      dataIndex: 'quantity',
      key: 'quantity',
      title: t('page.storage.in.purchase.quantity'),
      width: 120
    },
    {
      align: 'right',
      dataIndex: 'unitPrice',
      key: 'unitPrice',
      render: value => (value as number).toFixed(4),
      title: t('page.storage.in.purchase.unitPrice'),
      width: 120
    },
    {
      align: 'right',
      dataIndex: 'totalPrice',
      key: 'totalPrice',
      render: value => (value as number).toFixed(4),
      title: t('page.storage.in.purchase.totalPrice'),
      width: 120
    },
    {
      align: 'center',
      dataIndex: 'batchNo',
      key: 'batchNo',
      render: value => displayText(value),
      title: t('page.storage.in.purchase.batchNo'),
      width: 140
    },
    {
      align: 'center',
      dataIndex: 'productDate',
      key: 'productDate',
      render: value => displayDate(value),
      title: t('page.storage.in.purchase.productDate'),
      width: 120
    },
    {
      align: 'center',
      dataIndex: 'expireDate',
      key: 'expireDate',
      render: value => displayDate(value),
      title: t('page.storage.in.purchase.expireDate'),
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
    <div className="h-full min-h-500px flex-col-stretch gap-16px overflow-auto">
      {/* 基本信息卡片 */}
      <Card
        className="card-wrapper"
        extra={<Button onClick={() => closeTabAndNavigate(LIST_PATH)}>{t('page.storage.in.purchase.back')}</Button>}
        variant="borderless"
        title={
          <Flex
            wrap
            align="center"
            gap={12}
          >
            <span>{detail.inNo}</span>
            {renderStockDocumentStatus(detail.businessStatus)}
          </Flex>
        }
      >
        <Descriptions
          column={{ lg: 3, md: 2, sm: 1, xs: 1 }}
          items={basicItems}
          size="middle"
          title={t('page.storage.in.purchase.sectionBasic')}
        />
        <Divider />
        <Descriptions
          column={{ lg: 3, md: 2, sm: 1, xs: 1 }}
          items={businessItems}
          size="middle"
          title={t('page.storage.in.purchase.sectionBusiness')}
        />
        <Divider />
        <Descriptions
          column={{ lg: 3, md: 2, sm: 1, xs: 1 }}
          items={auditItems}
          size="middle"
          title={t('page.storage.in.purchase.sectionAudit')}
        />
      </Card>

      {/* 商品明细卡片 */}
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
                  <strong>{t('page.storage.in.purchase.totalAmount')}</strong>
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
    </div>
  );
};

export default PurchaseStockInDetail;
