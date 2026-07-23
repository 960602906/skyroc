import type { FormListFieldData } from 'antd/es/form/FormList';
import type { Dayjs } from 'dayjs';
import dayjs from 'dayjs';

import RemoteOptionSelect from '@/components/RemoteOptionSelect';
import { PurchasePatternSelect } from '@/features/crud';
import { useFormRules } from '@/features/form';
import { PurchasePattern } from '@/service/enums';
import {
  SELECTION_OPTION_RESOURCES,
  toOptions,
  useGoodsUnitsByGoodsOptions,
  usePurchaserOptions,
  useWareOptions
} from '@/service/hooks';

/** 采购入库商品行表单值 */
export interface PurchaseStockInDetailFormValue {
  /** 商品批次号；同仓库同商品下定位唯一库存批次 */
  batchNo: string;
  /** 商品到期日期，仅记录自然日；无保质期或未知时为空（DateOnly 格式：yyyy-MM-dd） */
  expireDate?: Dayjs | null | string;
  /** 入库商品主键 */
  goodsId: string;
  /** 入库计量单位主键 */
  goodsUnitId: string;
  /** 已存在的入库商品行主键；为空表示新增商品行 */
  id?: null | string;
  /** 商品生产日期，仅记录自然日；未知时为空（DateOnly 格式：yyyy-MM-dd） */
  productDate?: Dayjs | null | string;
  /** 来源采购单商品明细主键；仅采购入库回填，用于追溯到货来源 */
  purchaseOrderDetailId?: null | string;
  /** 按入库单位计量的入库数量 */
  quantity: number;
  /** 当前入库商品行的业务备注 */
  remark?: null | string;
  /** 入库单价，按系统业务币种和入库单位计量 */
  unitPrice: number;
}

/** 采购入库抽屉表单值 */
export interface PurchaseStockInFormValue {
  /** 发起入库业务的部门主键 */
  departmentId?: null | string;
  /** 采购入库商品行，至少包含一项 */
  details: PurchaseStockInDetailFormValue[];
  /** 预计到货时间（UTC）；尚未确认时可为空 */
  expectedArrivalTime?: Dayjs | null | string;
  /** 待编辑的采购入库单主键 */
  id?: string;
  /** 表单内部索引，仅用于开发调试 */
  index?: number;
  /** 计划或实际入库时间（UTC） */
  inTime: Dayjs | string;
  /** 来源采购单主键；用于回填供应商、采购员和采购模式并支持追溯 */
  purchaseOrderId?: null | string;
  /** 采购模式：供应商直供或市场自采 */
  purchasePattern: Api.StockIn.PurchasePattern;
  /** 负责采购到货的采购员主键 */
  purchaserId?: null | string;
  /** 入库单级业务备注 */
  remark?: null | string;
  /** 供货供应商主键；供应商直供采购入库时必填 */
  supplierId?: null | string;
  /** 接收入库商品的仓库主键 */
  wareId: string;
}

/** 采购入库商品行：商品远程搜索，单位随当前商品联动加载。 */
function PurchaseStockInDetailRow({
  field,
  form,
  remove
}: {
  field: FormListFieldData;
  form: Page.FormInstance;
  remove: (index: number | number[]) => void;
}) {
  const { t } = useTranslation();
  const goodsId = AForm.useWatch(['details', field.name, 'goodsId'], form) as string | undefined;
  const { data: units = [] } = useGoodsUnitsByGoodsOptions(goodsId);
  const { defaultRequiredRule } = useFormRules();

  return (
    <ARow gutter={8}>
      {/* 商品选择 */}
      <ACol span={6}>
        <AForm.Item
          {...field}
          name={[field.name, 'goodsId']}
          rules={[defaultRequiredRule]}
        >
          <RemoteOptionSelect
            placeholder={t('page.storage.in.form.goodsId')}
            resource={SELECTION_OPTION_RESOURCES.GOODS}
            onChange={() => {
              // 商品变更时清空单位，触发单位下拉刷新
              form.setFieldValue(['details', field.name, 'goodsUnitId'], undefined);
            }}
          />
        </AForm.Item>
      </ACol>

      {/* 计量单位 */}
      <ACol span={4}>
        <AForm.Item
          {...field}
          name={[field.name, 'goodsUnitId']}
          rules={[defaultRequiredRule]}
        >
          <ASelect
            disabled={!goodsId}
            options={units}
            placeholder={t('page.storage.in.form.goodsUnitId')}
          />
        </AForm.Item>
      </ACol>

      {/* 入库数量 */}
      <ACol span={3}>
        <AForm.Item
          {...field}
          name={[field.name, 'quantity']}
          rules={[defaultRequiredRule]}
        >
          <AInputNumber
            className="w-full"
            min={0.000001}
            placeholder={t('page.storage.in.form.quantity')}
            precision={6}
          />
        </AForm.Item>
      </ACol>

      {/* 单价 */}
      <ACol span={3}>
        <AForm.Item
          {...field}
          name={[field.name, 'unitPrice']}
          rules={[defaultRequiredRule]}
        >
          <AInputNumber
            className="w-full"
            min={0}
            placeholder={t('page.storage.in.form.unitPrice')}
            precision={4}
          />
        </AForm.Item>
      </ACol>

      {/* 批次号 */}
      <ACol span={3}>
        <AForm.Item
          {...field}
          name={[field.name, 'batchNo']}
          rules={[defaultRequiredRule]}
        >
          <AInput placeholder={t('page.storage.in.form.batchNo')} />
        </AForm.Item>
      </ACol>

      {/* 生产日期 */}
      <ACol span={3}>
        <AForm.Item
          {...field}
          name={[field.name, 'productDate']}
        >
          <ADatePicker
            className="w-full"
            placeholder={t('page.storage.in.form.productDate')}
          />
        </AForm.Item>
      </ACol>

      {/* 到期日期 */}
      <ACol
        className="hidden"
        span={0}
      >
        <AForm.Item
          {...field}
          name={[field.name, 'expireDate']}
        >
          <ADatePicker
            className="w-full"
            placeholder={t('page.storage.in.form.expireDate')}
          />
        </AForm.Item>
      </ACol>

      {/* 删除行按钮 */}
      <ACol span={1}>
        <AButton
          block
          danger
          onClick={() => remove(field.name)}
        >
          -
        </AButton>
      </ACol>

      {/* 备注字段（整行） */}
      <ACol span={23}>
        <AForm.Item
          {...field}
          name={[field.name, 'remark']}
        >
          <AInput.TextArea
            placeholder={t('page.storage.in.form.detailRemark')}
            rows={1}
          />
        </AForm.Item>
      </ACol>

      {/* 隐藏字段：明细ID（编辑时需要） */}
      <ACol
        className="hidden"
        span={0}
      >
        <AForm.Item
          {...field}
          name={[field.name, 'id']}
        >
          <AInput />
        </AForm.Item>
      </ACol>
    </ARow>
  );
}

interface PurchaseStockInOperateDrawerProps {
  form: Page.FormInstance;
  handleSubmit: () => void;
  onClose: () => void;
  open: boolean;
  operateType: 'add' | 'edit';
}

/** 采购入库新增/编辑抽屉。 */
const PurchaseStockInOperateDrawer: FC<PurchaseStockInOperateDrawerProps> = ({
  form, // eslint-disable-line react/prop-types
  handleSubmit, // eslint-disable-line react/prop-types
  onClose, // eslint-disable-line react/prop-types
  open, // eslint-disable-line react/prop-types
  operateType // eslint-disable-line react/prop-types
}) => {
  const { t } = useTranslation();
  const { defaultRequiredRule } = useFormRules();
  const { data: wares } = useWareOptions();
  const { data: purchasers } = usePurchaserOptions();
  const wareOptions = toOptions(wares);
  const purchaserOptions = toOptions(purchasers);
  const isEdit = operateType === 'edit';

  return (
    <ADrawer
      open={open}
      title={isEdit ? t('page.storage.in.edit') : t('page.storage.in.add')}
      width={1200}
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
        initialValues={{ details: [{}], inTime: dayjs(), purchasePattern: PurchasePattern.SUPPLIER_DIRECT }}
        layout="vertical"
      >
        {/* 隐藏字段：主单ID（编辑时需要） */}
        <AForm.Item
          hidden
          name="id"
        >
          <AInput />
        </AForm.Item>

        {/* 主单字段 */}
        <ARow gutter={16}>
          {/* 仓库（必填） */}
          <ACol span={8}>
            <AForm.Item
              label={t('page.storage.in.wareId')}
              name="wareId"
              rules={[defaultRequiredRule]}
            >
              <ASelect
                allowClear
                options={wareOptions}
                placeholder={t('page.storage.in.form.wareId')}
              />
            </AForm.Item>
          </ACol>

          {/* 采购模式（必填） */}
          <ACol span={8}>
            <AForm.Item
              label={t('page.storage.in.purchasePattern')}
              name="purchasePattern"
              rules={[defaultRequiredRule]}
            >
              <PurchasePatternSelect allowClear={false} />
            </AForm.Item>
          </ACol>

          {/* 入库时间（必填） */}
          <ACol span={8}>
            <AForm.Item
              label={t('page.storage.in.inTime')}
              name="inTime"
              rules={[defaultRequiredRule]}
            >
              <ADatePicker
                showTime
                className="w-full"
                placeholder={t('page.storage.in.form.inTime')}
              />
            </AForm.Item>
          </ACol>

          {/* 供应商（可选） */}
          <ACol span={8}>
            <AForm.Item
              label={t('page.storage.in.supplier')}
              name="supplierId"
            >
              <RemoteOptionSelect
                allowClear
                placeholder={t('page.storage.in.form.supplierId')}
                resource={SELECTION_OPTION_RESOURCES.SUPPLIER}
              />
            </AForm.Item>
          </ACol>

          {/* 采购员（可选） */}
          <ACol span={8}>
            <AForm.Item
              label={t('page.storage.in.purchaser')}
              name="purchaserId"
            >
              <ASelect
                allowClear
                options={purchaserOptions}
                placeholder={t('page.storage.in.form.purchaserId')}
              />
            </AForm.Item>
          </ACol>

          {/* 预计到货时间（可选） */}
          <ACol span={8}>
            <AForm.Item
              label={t('page.storage.in.expectedArrivalTime')}
              name="expectedArrivalTime"
            >
              <ADatePicker
                showTime
                className="w-full"
                placeholder={t('page.storage.in.form.expectedArrivalTime')}
              />
            </AForm.Item>
          </ACol>
        </ARow>

        {/* 商品明细表格 */}
        <AForm.Item label={t('page.storage.in.details')}>
          <AForm.List
            name="details"
            rules={[
              {
                validator: async (_, details) => {
                  if (!details || details.length === 0) {
                    return Promise.reject(new Error(t('page.storage.in.validation.atLeastOneDetail')));
                  }
                  return Promise.resolve();
                }
              }
            ]}
          >
            {(fields, { add, remove }) => (
              <>
                {fields.map(field => (
                  <PurchaseStockInDetailRow
                    field={field}
                    form={form}
                    key={field.key}
                    remove={remove}
                  />
                ))}
                <AButton
                  block
                  type="dashed"
                  onClick={() => add({})}
                >
                  + {t('common.add')}
                </AButton>
              </>
            )}
          </AForm.List>
        </AForm.Item>

        {/* 备注 */}
        <AForm.Item
          label={t('page.storage.in.remark')}
          name="remark"
        >
          <AInput.TextArea
            placeholder={t('page.storage.in.form.remark')}
            rows={2}
          />
        </AForm.Item>
      </AForm>
    </ADrawer>
  );
};

export default PurchaseStockInOperateDrawer;
