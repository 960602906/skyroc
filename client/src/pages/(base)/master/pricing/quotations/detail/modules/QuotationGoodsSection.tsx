import type { TableColumnsType } from 'antd';
import { Suspense, lazy } from 'react';

import { DETAIL_EMPTY, displayText, renderBooleanTag } from '@/features/crud';
import { fetchAddQuotationGoods, fetchDeleteQuotationGoods, fetchUpdateQuotationGoods } from '@/service/api';

const QuotationGoodsOperateModal = lazy(() => import('../../../quotation-goods/modules/QuotationGoodsOperateModal'));

interface QuotationGoodsSectionProps {
  goods: Api.QuotationGoods.Entity[];
  onChanged: () => void;
  quotationId: string;
}

function QuotationGoodsSection({ goods, onChanged, quotationId }: QuotationGoodsSectionProps) {
  const { t } = useTranslation();
  const nav = useNavigate();
  const [form] = AForm.useForm<Api.QuotationGoods.UpdateParams>();
  const [open, setOpen] = useState(false);
  const [operateType, setOperateType] = useState<AntDesign.TableOperateType>('add');
  const [submitting, setSubmitting] = useState(false);

  function openAdd() {
    setOperateType('add');
    form.resetFields();
    form.setFieldsValue({
      isOnSale: true,
      quotationId
    });
    setOpen(true);
  }

  function openEdit(record: Api.QuotationGoods.Entity) {
    setOperateType('edit');
    form.setFieldsValue({
      goodsId: record.goodsId,
      goodsUnitId: record.goodsUnitId,
      id: record.id,
      isOnSale: record.isOnSale,
      minOrderQuantity: record.minOrderQuantity,
      quotationId: record.quotationId || quotationId,
      remark: record.remark,
      unitPrice: record.unitPrice
    });
    setOpen(true);
  }

  async function handleSubmit() {
    const values = await form.validateFields();
    setSubmitting(true);
    try {
      if (operateType === 'add') {
        await fetchAddQuotationGoods({
          ...values,
          isOnSale: values.isOnSale ?? true,
          quotationId
        });
        window.$message?.success(t('common.addSuccess'));
      } else {
        await fetchUpdateQuotationGoods({
          ...values,
          id: values.id!,
          quotationId
        });
        window.$message?.success(t('common.modifySuccess'));
      }
      setOpen(false);
      form.resetFields();
      onChanged();
    } finally {
      setSubmitting(false);
    }
  }

  async function handleDelete(id: string) {
    await fetchDeleteQuotationGoods(id);
    window.$message?.success(t('common.deleteSuccess'));
    onChanged();
  }

  const columns: TableColumnsType<Api.QuotationGoods.Entity> = [
    {
      align: 'center',
      dataIndex: 'goodsName',
      key: 'goodsName',
      minWidth: 140,
      render: (_, record) =>
        record.goodsId ? (
          <AButton
            className="h-auto p-0 leading-normal"
            size="small"
            type="link"
            onClick={() => nav(`/master/goods/detail/${record.goodsId}`)}
          >
            {displayText(record.goodsName || record.goodsId)}
          </AButton>
        ) : (
          DETAIL_EMPTY
        ),
      title: t('page.goods.quotationGoods.goodsId')
    },
    {
      align: 'center',
      dataIndex: 'goodsCode',
      key: 'goodsCode',
      minWidth: 120,
      render: value => displayText(value),
      title: t('page.goods.list.code')
    },
    {
      align: 'center',
      dataIndex: 'goodsUnitName',
      key: 'goodsUnitName',
      minWidth: 100,
      render: (value, record) => displayText(value || record.goodsUnitId),
      title: t('page.goods.quotationGoods.goodsUnitId')
    },
    {
      align: 'center',
      dataIndex: 'unitPrice',
      key: 'unitPrice',
      render: value => displayText(value),
      title: t('page.goods.quotationGoods.unitPrice'),
      width: 110
    },
    {
      align: 'center',
      dataIndex: 'minOrderQuantity',
      key: 'minOrderQuantity',
      render: value => displayText(value),
      title: t('page.goods.quotationGoods.minOrderQuantity'),
      width: 110
    },
    {
      align: 'center',
      dataIndex: 'isOnSale',
      key: 'isOnSale',
      render: value => renderBooleanTag(Boolean(value), t('page.goods.list.onSale'), t('page.goods.list.offSale')),
      title: t('page.goods.quotationGoods.isOnSale'),
      width: 100
    },
    {
      align: 'center',
      dataIndex: 'remark',
      key: 'remark',
      minWidth: 120,
      render: value => displayText(value),
      title: t('page.goods.quotationGoods.remark')
    },
    {
      align: 'center',
      fixed: 'right',
      key: 'operate',
      render: (_, record) => (
        <div className="flex-center gap-8px">
          <AButton
            ghost
            size="small"
            type="primary"
            onClick={() => openEdit(record)}
          >
            {t('common.edit')}
          </AButton>
          <APopconfirm
            title={t('common.confirmDelete')}
            onConfirm={() => handleDelete(record.id)}
          >
            <AButton
              danger
              size="small"
            >
              {t('common.delete')}
            </AButton>
          </APopconfirm>
        </div>
      ),
      title: t('common.operate'),
      width: 150
    }
  ];

  return (
    <>
      <ACard
        className="card-wrapper"
        title={t('page.goods.quotation.sectionGoods')}
        variant="borderless"
        extra={
          <AButton
            size="small"
            type="primary"
            onClick={openAdd}
          >
            {t('page.goods.quotationGoods.add')}
          </AButton>
        }
      >
        <ATable<Api.QuotationGoods.Entity>
          columns={columns}
          dataSource={goods}
          pagination={false}
          rowKey="id"
          scroll={{ x: 960 }}
          size="small"
        />
      </ACard>

      <Suspense>
        <QuotationGoodsOperateModal
          lockQuotationId
          form={form}
          handleSubmit={handleSubmit}
          open={open}
          operateType={operateType}
          onClose={() => {
            if (submitting) return;
            setOpen(false);
            form.resetFields();
          }}
        />
      </Suspense>
    </>
  );
}

export default QuotationGoodsSection;
