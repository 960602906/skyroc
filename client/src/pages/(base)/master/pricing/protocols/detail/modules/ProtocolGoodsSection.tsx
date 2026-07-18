import type { TableColumnsType } from 'antd';
import { Suspense, lazy } from 'react';

import { DETAIL_EMPTY, displayText } from '@/features/crud';
import {
  fetchAddCustomerProtocolGoods,
  fetchDeleteCustomerProtocolGoods,
  fetchUpdateCustomerProtocolGoods
} from '@/service/api';

const ProtocolGoodsOperateModal = lazy(() => import('../../../protocol-goods/modules/ProtocolGoodsOperateModal'));

interface ProtocolGoodsSectionProps {
  customerProtocolId: string;
  goods: Api.CustomerProtocolGoods.Entity[];
  onChanged: () => void;
}

function ProtocolGoodsSection({ customerProtocolId, goods, onChanged }: ProtocolGoodsSectionProps) {
  const { t } = useTranslation();
  const nav = useNavigate();
  const [form] = AForm.useForm<Api.CustomerProtocolGoods.UpdateParams>();
  const [open, setOpen] = useState(false);
  const [operateType, setOperateType] = useState<AntDesign.TableOperateType>('add');
  const [submitting, setSubmitting] = useState(false);

  function openAdd() {
    setOperateType('add');
    form.resetFields();
    form.setFieldsValue({
      customerProtocolId
    });
    setOpen(true);
  }

  function openEdit(record: Api.CustomerProtocolGoods.Entity) {
    setOperateType('edit');
    form.setFieldsValue({
      customerProtocolId: record.customerProtocolId || customerProtocolId,
      goodsId: record.goodsId,
      goodsUnitId: record.goodsUnitId,
      id: record.id,
      minOrderQuantity: record.minOrderQuantity,
      protocolPrice: record.protocolPrice,
      remark: record.remark
    });
    setOpen(true);
  }

  async function handleSubmit() {
    const values = await form.validateFields();
    setSubmitting(true);
    try {
      if (operateType === 'add') {
        await fetchAddCustomerProtocolGoods({
          ...values,
          customerProtocolId
        });
        window.$message?.success(t('common.addSuccess'));
      } else {
        await fetchUpdateCustomerProtocolGoods({
          ...values,
          customerProtocolId,
          id: values.id!
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
    await fetchDeleteCustomerProtocolGoods(id);
    window.$message?.success(t('common.deleteSuccess'));
    onChanged();
  }

  const columns: TableColumnsType<Api.CustomerProtocolGoods.Entity> = [
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
      title: t('page.customer.protocolGoods.goodsId')
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
      title: t('page.customer.protocolGoods.goodsUnitId')
    },
    {
      align: 'center',
      dataIndex: 'protocolPrice',
      key: 'protocolPrice',
      render: value => displayText(value),
      title: t('page.customer.protocolGoods.protocolPrice'),
      width: 110
    },
    {
      align: 'center',
      dataIndex: 'minOrderQuantity',
      key: 'minOrderQuantity',
      render: value => displayText(value),
      title: t('page.customer.protocolGoods.minOrderQuantity'),
      width: 110
    },
    {
      align: 'center',
      dataIndex: 'remark',
      key: 'remark',
      minWidth: 120,
      render: value => displayText(value),
      title: t('page.customer.protocolGoods.remark')
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
        title={t('page.customer.protocol.sectionGoods')}
        variant="borderless"
        extra={
          <AButton
            size="small"
            type="primary"
            onClick={openAdd}
          >
            {t('page.customer.protocolGoods.add')}
          </AButton>
        }
      >
        <ATable<Api.CustomerProtocolGoods.Entity>
          columns={columns}
          dataSource={goods}
          pagination={false}
          rowKey="id"
          scroll={{ x: 960 }}
          size="small"
        />
      </ACard>

      <Suspense>
        <ProtocolGoodsOperateModal
          lockCustomerProtocolId
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

export default ProtocolGoodsSection;
