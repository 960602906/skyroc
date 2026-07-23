import { useState } from 'react';

import RemoteOptionSelect from '@/components/RemoteOptionSelect';
import {
  CrudPageLayout,
  createDefaultPagination,
  createIndexColumn,
  displayDate,
  renderPurchasePattern,
  renderPurchasePlanStatus
} from '@/features/crud';
import { TableHeaderOperation, useTable } from '@/features/table';
import {
  fetchGeneratePurchasePlan,
  fetchGetPurchasePlanDetail,
  fetchGetPurchasePlanList,
  fetchGetPurchasePlanSplitOrders,
  fetchMergePurchasePlan,
  fetchSplitOrdersPurchasePlan,
  fetchSplitQuantityPurchasePlan,
  fetchUpdatePurchasePlanPurchaser,
  fetchUpdatePurchasePlanSupplier
} from '@/service/api';
import { PurchasePlanStatus } from '@/service/enums';
import { SELECTION_OPTION_RESOURCES, toOptions, usePurchaserOptions } from '@/service/hooks';

import PurchasePlanSearch from './modules/PurchasePlanSearch';

type ModalMode = 'assignPurchaser' | 'assignSupplier' | 'generate' | 'merge' | 'splitOrders' | 'splitQuantity' | null;

const modalTitleKeyMap: Record<Exclude<ModalMode, null>, App.I18n.I18nKey> = {
  assignPurchaser: 'page.purchase.plan.assignPurchaser',
  assignSupplier: 'page.purchase.plan.assignSupplier',
  generate: 'page.purchase.plan.generate',
  merge: 'page.purchase.plan.merge',
  splitOrders: 'page.purchase.plan.splitByOrders',
  splitQuantity: 'page.purchase.plan.splitByQuantity'
};

/** 采购计划分页、生成、分配、合并及拆分页面。 */
const PurchasePlanList = () => {
  const { t } = useTranslation();
  const nav = useNavigate();
  const [form] = AForm.useForm();
  const [mode, setMode] = useState<ModalMode>(null);
  const [selectedRowKeys, setSelectedRowKeys] = useState<React.Key[]>([]);
  const [activePlan, setActivePlan] = useState<Api.PurchasePlan.Entity | null>(null);
  const [splitOrders, setSplitOrders] = useState<Api.PurchasePlan.SplittableOrder[]>([]);
  const { data: purchasers } = usePurchaserOptions();

  const searchParams = useMemo(
    () => ({
      current: 1,
      goodsId: null,
      keyword: null,
      planDateEnd: null,
      planDateStart: null,
      purchasePattern: null,
      purchaserId: null,
      purchaseStatus: null,
      size: 10,
      supplierId: null
    }),
    []
  );

  const { columnChecks, run, searchProps, setColumnChecks, tableProps, tableWrapperRef } = useTable({
    apiFn: fetchGetPurchasePlanList,
    apiParams: searchParams,
    columns: () => [
      createIndexColumn(t),
      {
        align: 'center',
        dataIndex: 'planNo',
        fixed: 'left',
        key: 'planNo',
        render: (value: string, record) => (
          <AButton
            className="p-0"
            type="link"
            onClick={() => nav(`/purchase/plans/detail/${record.id}`)}
          >
            {value}
          </AButton>
        ),
        title: t('page.purchase.plan.planNo'),
        width: 170
      },
      {
        align: 'center',
        dataIndex: 'planDate',
        key: 'planDate',
        render: displayDate,
        title: t('page.purchase.plan.planDate'),
        width: 130
      },
      {
        align: 'center',
        dataIndex: 'purchasePattern',
        key: 'purchasePattern',
        render: renderPurchasePattern,
        title: t('page.purchase.plan.purchasePattern'),
        width: 130
      },
      {
        align: 'center',
        dataIndex: 'supplierName',
        key: 'supplierName',
        title: t('page.purchase.plan.supplier'),
        width: 150
      },
      {
        align: 'center',
        dataIndex: 'purchaserName',
        key: 'purchaserName',
        title: t('page.purchase.plan.purchaser'),
        width: 130
      },
      {
        align: 'center',
        dataIndex: 'purchaseStatus',
        key: 'purchaseStatus',
        render: renderPurchasePlanStatus,
        title: t('page.purchase.plan.purchaseStatus'),
        width: 140
      },
      {
        align: 'center',
        dataIndex: 'details',
        key: 'details',
        render: (details: Api.PurchasePlan.Detail[]) => details.map(item => item.goodsName).join('、'),
        title: t('page.purchase.plan.goods'),
        width: 220
      },
      {
        align: 'center',
        fixed: 'right',
        key: 'operate',
        render: (_, record) =>
          record.purchaseStatus === PurchasePlanStatus.UNPUBLISHED && (
            <div className="flex-center flex-wrap gap-8px">
              <AButton
                size="small"
                type="primary"
                onClick={() => openPlanAction('splitOrders', record)}
              >
                {t('page.purchase.plan.splitByOrders')}
              </AButton>
              <AButton
                size="small"
                onClick={() => openPlanAction('splitQuantity', record)}
              >
                {t('page.purchase.plan.splitByQuantity')}
              </AButton>
            </div>
          ),
        title: t('common.operate'),
        width: 230
      }
    ],
    pagination: createDefaultPagination(),
    scroll: { x: 'max-content' },
    transformParams: params => {
      const next = { ...params } as Api.PurchasePlan.SearchParams;

      if (next.purchaseStatus === null || next.purchaseStatus === undefined) {
        delete next.purchaseStatus;
      } else {
        next.purchaseStatus = Number(next.purchaseStatus) as Api.PurchasePlan.PurchaseStatus;
      }

      return next;
    }
  });

  function selectedIds() {
    return selectedRowKeys.map(String);
  }

  function openModal(nextMode: Exclude<ModalMode, 'splitOrders' | 'splitQuantity'>) {
    form.resetFields();
    setMode(nextMode);
  }

  async function openPlanAction(nextMode: 'splitOrders' | 'splitQuantity', plan: Api.PurchasePlan.Entity) {
    form.resetFields();
    setActivePlan(await fetchGetPurchasePlanDetail(plan.id));
    if (nextMode === 'splitOrders') setSplitOrders(await fetchGetPurchasePlanSplitOrders(plan.id));
    if (nextMode === 'splitQuantity') {
      form.setFieldsValue({ details: plan.details.map(detail => ({ detailId: detail.id })) });
    }
    setMode(nextMode);
  }

  async function submit() {
    const values = await form.validateFields();
    const ids = selectedIds();
    if (mode === 'generate') await fetchGeneratePurchasePlan({ orderIds: values.orderIds, remark: values.remark });
    if (mode === 'assignSupplier')
      await fetchUpdatePurchasePlanSupplier({ planIds: ids, supplierId: values.supplierId || null });
    if (mode === 'assignPurchaser')
      await fetchUpdatePurchasePlanPurchaser({ planIds: ids, purchaserId: values.purchaserId || null });
    if (mode === 'merge') await fetchMergePurchasePlan({ planIds: ids, remark: values.remark });
    if (mode === 'splitOrders' && activePlan)
      await fetchSplitOrdersPurchasePlan({
        planId: activePlan.id,
        remark: values.remark,
        saleOrderIds: values.saleOrderIds
      });
    if (mode === 'splitQuantity' && activePlan)
      await fetchSplitQuantityPurchasePlan({ details: values.details, planId: activePlan.id, remark: values.remark });
    window.$message?.success(t('common.updateSuccess'));
    setMode(null);
    await run(false);
  }

  const selectionRequired = selectedIds().length === 0;
  const purchaserOptions = toOptions(purchasers);

  return (
    <>
      <CrudPageLayout
        search={<PurchasePlanSearch {...searchProps} />}
        tableWrapperRef={tableWrapperRef}
        title={t('page.purchase.plan.title')}
        extra={
          <TableHeaderOperation
            disabledDelete
            add={() => nav('/purchase/plans/operate')}
            columns={columnChecks}
            loading={tableProps.loading}
            refresh={run}
            setColumnChecks={setColumnChecks}
            onDelete={() => undefined}
          >
            <AButton
              size="small"
              type="primary"
              onClick={() => nav('/purchase/plans/operate')}
            >
              {t('page.purchase.plan.add')}
            </AButton>
            <AButton
              disabled={selectionRequired}
              size="small"
              onClick={() => openModal('assignSupplier')}
            >
              {t('page.purchase.plan.assignSupplier')}
            </AButton>
            <AButton
              disabled={selectionRequired}
              size="small"
              onClick={() => openModal('assignPurchaser')}
            >
              {t('page.purchase.plan.assignPurchaser')}
            </AButton>
            <AButton
              disabled={selectedIds().length < 2}
              size="small"
              onClick={() => openModal('merge')}
            >
              {t('page.purchase.plan.merge')}
            </AButton>
            <AButton
              size="small"
              type="primary"
              onClick={() => openModal('generate')}
            >
              {t('page.purchase.plan.generate')}
            </AButton>
          </TableHeaderOperation>
        }
        table={
          <ATable
            rowSelection={{ onChange: setSelectedRowKeys, selectedRowKeys }}
            size="small"
            {...tableProps}
          />
        }
      />
      <AModal
        destroyOnClose
        open={mode !== null}
        title={mode ? t(modalTitleKeyMap[mode]) : ''}
        onCancel={() => setMode(null)}
        onOk={submit}
      >
        <AForm
          form={form}
          layout="vertical"
        >
          {mode === 'generate' && (
            <AForm.Item
              label={t('page.purchase.plan.generate')}
              name="orderIds"
              rules={[{ required: true }]}
            >
              <ASelect
                mode="tags"
                placeholder={t('page.purchase.plan.form.orderIds')}
                tokenSeparators={[',']}
              />
            </AForm.Item>
          )}
          {mode === 'assignSupplier' && (
            <AForm.Item
              label={t('page.purchase.plan.supplier')}
              name="supplierId"
            >
              <RemoteOptionSelect
                allowClear
                placeholder={t('page.purchase.plan.form.supplierId')}
                resource={SELECTION_OPTION_RESOURCES.SUPPLIER}
              />
            </AForm.Item>
          )}
          {mode === 'assignPurchaser' && (
            <AForm.Item
              label={t('page.purchase.plan.purchaser')}
              name="purchaserId"
            >
              <ASelect
                allowClear
                options={purchaserOptions}
                placeholder={t('page.purchase.plan.form.purchaserId')}
              />
            </AForm.Item>
          )}
          {mode === 'splitOrders' && (
            <AForm.Item
              label={t('page.purchase.plan.splitByOrders')}
              name="saleOrderIds"
              rules={[{ required: true }]}
            >
              <ASelect
                mode="multiple"
                placeholder={t('page.purchase.plan.form.saleOrderIds')}
                options={splitOrders.map(item => ({
                  label: `${item.saleOrderNo} (${item.requiredQuantity})`,
                  value: item.saleOrderId
                }))}
              />
            </AForm.Item>
          )}
          {mode === 'splitQuantity' && (
            <AForm.List name="details">
              {() =>
                activePlan?.details.map((detail, index) => (
                  <ARow
                    gutter={8}
                    key={detail.id}
                  >
                    <ACol span={14}>
                      {detail.goodsName} ({detail.purchaseUnitName})
                    </ACol>
                    <ACol span={10}>
                      <AForm.Item
                        hidden
                        initialValue={detail.id}
                        name={[index, 'detailId']}
                      >
                        <AInput />
                      </AForm.Item>
                      <AForm.Item name={[index, 'quantity']}>
                        <AInputNumber
                          className="w-full"
                          max={detail.plannedQuantity}
                          min={0.0001}
                        />
                      </AForm.Item>
                    </ACol>
                  </ARow>
                ))
              }
            </AForm.List>
          )}
          {mode !== 'assignSupplier' && mode !== 'assignPurchaser' && (
            <AForm.Item
              label={t('page.purchase.plan.remark')}
              name="remark"
            >
              <AInput.TextArea />
            </AForm.Item>
          )}
        </AForm>
      </AModal>
    </>
  );
};

export default PurchasePlanList;
