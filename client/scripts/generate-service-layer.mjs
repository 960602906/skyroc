import fs from 'node:fs';
import path from 'node:path';

const root = path.resolve('src/service');

const crudModules = [
  {
    allPick: ['id', 'name', 'code'],
    allSuffix: 'Companies',
    createFields: ['name', 'code', 'remark', 'status', 'contactName', 'contactPhone', 'address'],
    fetchPrefix: 'Company',
    fields: [
      'name: string',
      'code: string',
      'remark: string | null',
      'contactName: string | null',
      'contactPhone: string | null',
      'address: string | null'
    ],
    namespace: 'Company',
    path: 'Companies',
    searchParams: 'Api.Base.SearchParams',
    urlsKey: 'COMPANY'
  },
  {
    allPick: ['id', 'name', 'code'],
    allSuffix: 'Customers',
    createFields: [
      'name',
      'code',
      'remark',
      'status',
      'companyId',
      'quotationId',
      'defaultWareId',
      'unifiedSocialCreditCode',
      'legalRepresentative',
      'registeredCapital',
      'establishDate',
      'businessTerm',
      'registrationStatus',
      'registrationAuthority',
      'registeredAddress',
      'businessScope',
      'invoiceTitle',
      'taxpayerIdentificationNumber',
      'invoiceAddress',
      'invoicePhone',
      'bankName',
      'bankAccount',
      'invoiceReceiverName',
      'invoiceReceiverPhone',
      'invoiceReceiverAddress',
      'invoiceEmail',
      'contactName',
      'contactPhone',
      'address',
      'tagIds'
    ],
    fetchPrefix: 'Customer',
    fields: [
      'name: string',
      'code: string',
      'remark: string | null',
      'companyId: string | null',
      'quotationId: string | null',
      'defaultWareId: string | null',
      'unifiedSocialCreditCode: string | null',
      'legalRepresentative: string | null',
      'registeredCapital: string | null',
      'establishDate: string | null',
      'businessTerm: string | null',
      'registrationStatus: string | null',
      'registrationAuthority: string | null',
      'registeredAddress: string | null',
      'businessScope: string | null',
      'invoiceTitle: string | null',
      'taxpayerIdentificationNumber: string | null',
      'invoiceAddress: string | null',
      'invoicePhone: string | null',
      'bankName: string | null',
      'bankAccount: string | null',
      'invoiceReceiverName: string | null',
      'invoiceReceiverPhone: string | null',
      'invoiceReceiverAddress: string | null',
      'invoiceEmail: string | null',
      'contactName: string | null',
      'contactPhone: string | null',
      'address: string | null',
      'tagIds: string[] | null'
    ],
    namespace: 'Customer',
    path: 'Customers',
    searchParams: `CommonType.RecordNullable<
      Api.Base.SearchParams & {
        companyId?: string | null;
        quotationId?: string | null;
        defaultWareId?: string | null;
        taxpayerIdentificationNumber?: string | null;
        unifiedSocialCreditCode?: string | null;
      }
    >`,
    urlsKey: 'CUSTOMER'
  },
  {
    allPick: ['id', 'name', 'code'],
    allSuffix: 'CustomerProtocols',
    createFields: ['name', 'code', 'remark', 'status', 'quotationId', 'effectiveStart', 'effectiveEnd', 'customerIds'],
    fetchPrefix: 'CustomerProtocol',
    fields: [
      'name: string',
      'code: string',
      'remark: string | null',
      'quotationId: string | null',
      'effectiveStart: string',
      'effectiveEnd: string | null',
      'customerIds: string[] | null'
    ],
    namespace: 'CustomerProtocol',
    path: 'CustomerProtocols',
    searchParams: `CommonType.RecordNullable<
      Api.Base.SearchParams & {
        quotationId?: string | null;
      }
    >`,
    urlsKey: 'CUSTOMER_PROTOCOL'
  },
  {
    allPick: ['id', 'customerProtocolId', 'goodsId'],
    allSuffix: 'CustomerProtocolGoods',
    createFields: ['customerProtocolId', 'goodsId', 'goodsUnitId', 'protocolPrice', 'minOrderQuantity', 'remark'],
    fetchPrefix: 'CustomerProtocolGoods',
    fields: [
      'customerProtocolId: string',
      'goodsId: string',
      'goodsUnitId: string',
      'protocolPrice: number',
      'minOrderQuantity: number | null',
      'remark: string | null'
    ],
    namespace: 'CustomerProtocolGoods',
    path: 'CustomerProtocolGoods',
    searchParams: `CommonType.RecordNullable<
      Pick<Common.CommonRecord, 'status'> &
        Common.CommonSearchParams & {
          customerProtocolId?: string | null;
          goodsId?: string | null;
        }
    >`,
    urlsKey: 'CUSTOMER_PROTOCOL_GOODS'
  },
  {
    allPick: ['id', 'username', 'companyId'],
    allSuffix: 'CustomerSubAccounts',
    createFields: [
      'companyId',
      'customerId',
      'username',
      'nickName',
      'phone',
      'email',
      'passwordHash',
      'remark',
      'status'
    ],
    fetchPrefix: 'CustomerSubAccount',
    fields: [
      'companyId: string',
      'customerId: string | null',
      'username: string | null',
      'nickName: string | null',
      'phone: string | null',
      'email: string | null',
      'passwordHash: string | null',
      'remark: string | null'
    ],
    namespace: 'CustomerSubAccount',
    path: 'CustomerSubAccounts',
    searchParams: `CommonType.RecordNullable<
      Pick<Common.CommonRecord, 'status'> &
        Common.CommonSearchParams & {
          companyId?: string | null;
          customerId?: string | null;
          username?: string | null;
          nickName?: string | null;
        }
    >`,
    urlsKey: 'CUSTOMER_SUB_ACCOUNT'
  },
  {
    allPick: ['id', 'name', 'code'],
    allSuffix: 'CustomerTags',
    createFields: ['name', 'code', 'remark', 'status', 'parentId', 'sort'],
    extras: ['tree'],
    fetchPrefix: 'CustomerTag',
    fields: ['name: string', 'code: string', 'remark: string | null', 'parentId: string | null', 'sort: number'],
    namespace: 'CustomerTag',
    path: 'CustomerTags',
    searchParams: `CommonType.RecordNullable<
      Api.Base.SearchParams & {
        parentId?: string | null;
      }
    >`,
    urlsKey: 'CUSTOMER_TAG'
  },
  {
    allPick: ['id', 'name', 'code'],
    allSuffix: 'Goods',
    createFields: [
      'name',
      'code',
      'remark',
      'status',
      'goodsTypeId',
      'baseUnitId',
      'defaultSupplierId',
      'defaultWareId',
      'spec',
      'brand',
      'origin',
      'description',
      'taxRate',
      'isOnSale',
      'supplierIds'
    ],
    extras: ['saleStatus'],
    fetchPrefix: 'Goods',
    fields: [
      'name: string',
      'code: string',
      'remark: string | null',
      'goodsTypeId: string',
      'baseUnitId: string | null',
      'defaultSupplierId: string | null',
      'defaultWareId: string | null',
      'spec: string | null',
      'brand: string | null',
      'origin: string | null',
      'description: string | null',
      'taxRate: number | null',
      'isOnSale: boolean',
      'supplierIds: string[] | null'
    ],
    namespace: 'Goods',
    path: 'Goods',
    searchParams: `CommonType.RecordNullable<
      Api.Base.SearchParams & {
        goodsTypeId?: string | null;
        defaultSupplierId?: string | null;
        defaultWareId?: string | null;
        isOnSale?: boolean | null;
      }
    >`,
    urlsKey: 'GOODS'
  },
  {
    allPick: ['id', 'name', 'code'],
    allSuffix: 'GoodsTypes',
    createFields: [
      'name',
      'code',
      'remark',
      'status',
      'parentId',
      'imageUrl',
      'taxCategoryCode',
      'taxCategoryName',
      'invoiceGoodsShortName',
      'defaultTaxRate',
      'isTaxExempt',
      'taxPolicyBasis',
      'sort'
    ],
    extras: ['tree'],
    fetchPrefix: 'GoodsType',
    fields: [
      'name: string',
      'code: string',
      'remark: string | null',
      'parentId: string | null',
      'imageUrl: string | null',
      'taxCategoryCode: string | null',
      'taxCategoryName: string | null',
      'invoiceGoodsShortName: string | null',
      'defaultTaxRate: number | null',
      'isTaxExempt: boolean',
      'taxPolicyBasis: string | null',
      'sort: number'
    ],
    namespace: 'GoodsType',
    path: 'GoodsTypes',
    searchParams: `CommonType.RecordNullable<
      Api.Base.SearchParams & {
        parentId?: string | null;
        taxCategoryCode?: string | null;
      }
    >`,
    urlsKey: 'GOODS_TYPE'
  },
  {
    allPick: ['id', 'name', 'goodsId'],
    allSuffix: 'GoodsUnits',
    createFields: ['goodsId', 'name', 'code', 'conversionRate', 'isBaseUnit', 'sort', 'remark', 'status'],
    extras: ['byGoods'],
    fetchPrefix: 'GoodsUnit',
    fields: [
      'goodsId: string',
      'name: string | null',
      'code: string | null',
      'conversionRate: number',
      'isBaseUnit: boolean',
      'sort: number',
      'remark: string | null'
    ],
    namespace: 'GoodsUnit',
    path: 'GoodsUnits',
    searchParams: `CommonType.RecordNullable<
      Pick<Common.CommonRecord, 'status'> &
        Common.CommonSearchParams & {
          goodsId?: string | null;
          name?: string | null;
        }
    >`,
    urlsKey: 'GOODS_UNIT'
  },
  {
    allPick: ['id', 'name', 'code'],
    allSuffix: 'Purchasers',
    createFields: ['name', 'code', 'remark', 'status', 'phone', 'userId', 'departmentId'],
    fetchPrefix: 'Purchaser',
    fields: [
      'name: string',
      'code: string',
      'remark: string | null',
      'phone: string | null',
      'userId: string | null',
      'departmentId: string | null'
    ],
    namespace: 'Purchaser',
    path: 'Purchasers',
    searchParams: `CommonType.RecordNullable<
      Api.Base.SearchParams & {
        departmentId?: string | null;
      }
    >`,
    urlsKey: 'PURCHASER'
  },
  {
    allPick: ['id', 'name', 'code'],
    allSuffix: 'PurchaseRules',
    createFields: [
      'name',
      'code',
      'remark',
      'status',
      'supplierId',
      'purchaserId',
      'wareId',
      'goodsTypeId',
      'purchasePattern',
      'goodsIds',
      'customerIds'
    ],
    fetchPrefix: 'PurchaseRule',
    fields: [
      'name: string',
      'code: string',
      'remark: string | null',
      'supplierId: string | null',
      'purchaserId: string | null',
      'wareId: string | null',
      'goodsTypeId: string | null',
      'purchasePattern: number',
      'goodsIds: string[] | null',
      'customerIds: string[] | null'
    ],
    namespace: 'PurchaseRule',
    path: 'PurchaseRules',
    searchParams: `CommonType.RecordNullable<
      Api.Base.SearchParams & {
        supplierId?: string | null;
        purchaserId?: string | null;
        wareId?: string | null;
        goodsTypeId?: string | null;
        purchasePattern?: number | null;
      }
    >`,
    urlsKey: 'PURCHASE_RULE'
  },
  {
    allPick: ['id', 'name', 'code'],
    allSuffix: 'Quotations',
    createFields: [
      'name',
      'code',
      'remark',
      'status',
      'description',
      'effectiveStart',
      'effectiveEnd',
      'isAudited',
      'customerIds'
    ],
    extras: ['audit'],
    fetchPrefix: 'Quotation',
    fields: [
      'name: string',
      'code: string',
      'remark: string | null',
      'description: string | null',
      'effectiveStart: string | null',
      'effectiveEnd: string | null',
      'isAudited: boolean',
      'customerIds: string[] | null'
    ],
    namespace: 'Quotation',
    path: 'Quotations',
    searchParams: `CommonType.RecordNullable<
      Api.Base.SearchParams & {
        isAudited?: boolean | null;
      }
    >`,
    urlsKey: 'QUOTATION'
  },
  {
    allPick: ['id', 'quotationId', 'goodsId'],
    allSuffix: 'QuotationGoods',
    createFields: ['quotationId', 'goodsId', 'goodsUnitId', 'unitPrice', 'minOrderQuantity', 'isOnSale', 'remark'],
    fetchPrefix: 'QuotationGoods',
    fields: [
      'quotationId: string',
      'goodsId: string',
      'goodsUnitId: string',
      'unitPrice: number',
      'minOrderQuantity: number | null',
      'isOnSale: boolean',
      'remark: string | null'
    ],
    namespace: 'QuotationGoods',
    path: 'QuotationGoods',
    searchParams: `CommonType.RecordNullable<
      Pick<Common.CommonRecord, 'status'> &
        Common.CommonSearchParams & {
          quotationId?: string | null;
          goodsId?: string | null;
          isOnSale?: boolean | null;
        }
    >`,
    urlsKey: 'QUOTATION_GOODS'
  },
  {
    allPick: ['id', 'name', 'code'],
    allSuffix: 'Suppliers',
    createFields: [
      'name',
      'code',
      'remark',
      'status',
      'contactName',
      'contactPhone',
      'address',
      'bankName',
      'bankAccount',
      'taxNo'
    ],
    fetchPrefix: 'Supplier',
    fields: [
      'name: string',
      'code: string',
      'remark: string | null',
      'contactName: string | null',
      'contactPhone: string | null',
      'address: string | null',
      'bankName: string | null',
      'bankAccount: string | null',
      'taxNo: string | null'
    ],
    namespace: 'Supplier',
    path: 'Suppliers',
    searchParams: 'Api.Base.SearchParams',
    urlsKey: 'SUPPLIER'
  },
  {
    allPick: ['id', 'name', 'code'],
    allSuffix: 'Wares',
    createFields: ['name', 'code', 'remark', 'status', 'contactName', 'contactPhone', 'address', 'sort'],
    fetchPrefix: 'Ware',
    fields: [
      'name: string',
      'code: string',
      'remark: string | null',
      'contactName: string | null',
      'contactPhone: string | null',
      'address: string | null',
      'sort: number'
    ],
    namespace: 'Ware',
    path: 'Wares',
    searchParams: 'Api.Base.SearchParams',
    urlsKey: 'WARE'
  }
];

const fileNameMap = {
  Company: 'company',
  Customer: 'customer',
  CustomerProtocol: 'customer-protocol',
  CustomerProtocolGoods: 'customer-protocol-goods',
  CustomerSubAccount: 'customer-sub-account',
  CustomerTag: 'customer-tag',
  Goods: 'goods',
  GoodsType: 'goods-type',
  GoodsUnit: 'goods-unit',
  Purchaser: 'purchaser',
  PurchaseRule: 'purchase-rule',
  Quotation: 'quotation',
  QuotationGoods: 'quotation-goods',
  Supplier: 'supplier',
  Ware: 'ware'
};

function write(file, content) {
  const full = path.join(root, file);
  fs.mkdirSync(path.dirname(full), { recursive: true });
  fs.writeFileSync(full, content, 'utf8');
  return full;
}

function genTypeFile(mod) {
  const pick = mod.allPick.map(f => `'${f}'`).join(' | ');
  return `declare namespace Api {
  namespace ${mod.namespace} {
    type Entity = Common.CommonRecord<{
      ${mod.fields.join(';\n      ')};
    }>;

    type CreateParams = CommonType.RecordNullable<Pick<Entity, ${mod.createFields.map(f => `'${f}'`).join(' | ')}>>;

    type UpdateParams = CreateParams & { id: string };

    type SearchParams = ${mod.searchParams};

    type List = Common.PaginatingQueryRecord<Entity>;

    type AllEntity = Pick<Entity, ${pick}>;
  }
}
`;
}

function genUrlsFile(mod) {
  const urls = {
    BASE: `'/${mod.path}'`,
    BATCH_DELETE: `'/${mod.path}/batchDelete'`,
    LIST: `'/${mod.path}/list'`
  };
  let extra = '';
  if (mod.extras?.includes('tree')) extra += `,\n  TREE: '/${mod.path}/tree'`;
  if (mod.extras?.includes('byGoods')) extra += `,\n  BY_GOODS: '/${mod.path}/by-goods'`;

  return `/** ${mod.namespace} module URLs */

export const ${mod.urlsKey}_URLS = {
  BASE: ${urls.BASE},
  BATCH_DELETE: ${urls.BATCH_DELETE},
  LIST: ${urls.LIST}${extra}
} as const;
`;
}

/** Apifox 接口 summary，与 OpenAPI Spec 保持一致 */
const APIFOX_COMMENTS = {
  all: '查询全部。',
  batchDelete: '批量删除。',
  create: '创建。',
  delete: '删除。',
  detail: '根据 ID 查询。',
  list: '分页查询。',
  toggleStatus: '启用或禁用。',
  update: '更新。'
};

const APIFOX_EXTRA_COMMENTS = {
  audit: '审核或反审核报价单。',
  byGoods: '查询指定商品的单位列表。',
  saleStatus: '修改商品上下架状态。',
  tree: {
    CustomerTag: '获取客户标签树。',
    GoodsType: '获取商品分类树。'
  }
};

function genApiFile(mod) {
  const ns = mod.namespace;
  const urls = `${mod.urlsKey}_URLS`;
  const fp = mod.fetchPrefix;
  const all = mod.allSuffix;
  let extraFns = '';

  if (mod.extras?.includes('tree')) {
    const treeComment = APIFOX_EXTRA_COMMENTS.tree[mod.namespace] ?? `获取${mod.namespace}树。`;
    extraFns += `\n/** ${treeComment} */\nexport function fetchGet${fp}Tree() {\n  return request<Api.${ns}.Entity[]>({\n    method: 'get',\n    url: ${urls}.TREE\n  });\n}\n`;
  }
  if (mod.extras?.includes('byGoods')) {
    extraFns += `\n/** ${APIFOX_EXTRA_COMMENTS.byGoods} */\nexport function fetchGet${fp}sByGoods(goodsId: string) {\n  return request<Api.${ns}.Entity[]>({\n    method: 'get',\n    url: \`\${${urls}.BY_GOODS}/\${goodsId}\`\n  });\n}\n`;
  }
  if (mod.extras?.includes('saleStatus')) {
    extraFns += `\n/** ${APIFOX_EXTRA_COMMENTS.saleStatus} */\nexport function fetchToggle${fp}SaleStatus(id: string, isOnSale: boolean) {\n  return request<Api.${ns}.Entity>({\n    method: 'patch',\n    params: { isOnSale },\n    url: \`\${${urls}.BASE}/\${id}/sale-status\`\n  });\n}\n`;
  }
  if (mod.extras?.includes('audit')) {
    extraFns += `\n/** ${APIFOX_EXTRA_COMMENTS.audit} */\nexport function fetchAudit${fp}(id: string, isAudited: boolean) {\n  return request<Api.${ns}.Entity>({\n    method: 'patch',\n    params: { isAudited },\n    url: \`\${${urls}.BASE}/\${id}/audit\`\n  });\n}\n`;
  }

  return `import { request } from '../request';
import { ${urls} } from '../urls';

/** ${APIFOX_COMMENTS.list} */
export function fetchGet${fp}List(params?: Api.${ns}.SearchParams) {
  return request<Api.${ns}.List>({
    method: 'get',
    params,
    url: ${urls}.LIST
  });
}

/** ${APIFOX_COMMENTS.all} */
export function fetchGetAll${all}() {
  return request<Api.${ns}.AllEntity[]>({
    method: 'get',
    url: ${urls}.BASE
  });
}

/** ${APIFOX_COMMENTS.detail} */
export function fetchGet${fp}Detail(id: string) {
  return request<Api.${ns}.Entity>({
    method: 'get',
    url: \`\${${urls}.BASE}/\${id}\`
  });
}

/** ${APIFOX_COMMENTS.create} */
export function fetchAdd${fp}(data: Api.${ns}.CreateParams) {
  return request<Api.${ns}.Entity>({
    data,
    method: 'post',
    url: ${urls}.BASE
  });
}

/** ${APIFOX_COMMENTS.update} */
export function fetchUpdate${fp}(data: Api.${ns}.UpdateParams) {
  return request<Api.${ns}.Entity>({
    data,
    method: 'put',
    url: ${urls}.BASE
  });
}

/** ${APIFOX_COMMENTS.delete} */
export function fetchDelete${fp}(id: string) {
  return request<Api.${ns}.Entity>({
    method: 'delete',
    url: \`\${${urls}.BASE}/\${id}\`
  });
}

/** ${APIFOX_COMMENTS.batchDelete} */
export function fetchBatchDelete${fp}(ids: string[]) {
  return request<Api.${ns}.Entity>({
    data: ids,
    method: 'delete',
    url: ${urls}.BATCH_DELETE
  });
}

/** ${APIFOX_COMMENTS.toggleStatus} */
export function fetchToggle${fp}Status(params: Api.Base.ToggleStatusParams) {
  return request<Api.${ns}.Entity>({
    method: 'patch',
    params: { status: params.status },
    url: \`\${${urls}.BASE}/\${params.id}/status\`
  });
}
${extraFns}`;
}

const created = [];

created.push(
  write(
    'types/base.d.ts',
    `declare namespace Api {
  namespace Base {
    type SearchParams = CommonType.RecordNullable<
      Pick<Common.CommonRecord, 'status'> & Common.CommonSearchParams & { code?: string | null; name?: string | null }
    >;

    type ToggleStatusParams = { id: string; status?: Api.Common.EnableStatus | null };

    type ToggleBooleanParams = { id: string };
  }
}
`
  )
);

for (const mod of crudModules) {
  const fileBase = fileNameMap[mod.namespace];
  created.push(write(`types/${fileBase}.d.ts`, genTypeFile(mod)));
  created.push(write(`urls/${fileBase}.ts`, genUrlsFile(mod)));
  created.push(write(`api/${fileBase}.ts`, genApiFile(mod)));
}

console.log(created.join('\n'));
