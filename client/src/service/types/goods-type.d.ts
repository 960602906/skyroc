declare namespace Api {
  namespace GoodsType {
    type Entity = Common.CommonRecord<{
      code: string;
      defaultTaxRate: number | null;
      imageUrl: string | null;
      invoiceGoodsShortName: string | null;
      isTaxExempt: boolean;
      name: string;
      parentId: string | null;
      remark: string | null;
      sort: number;
      taxCategoryCode: string | null;
      taxCategoryName: string | null;
      taxPolicyBasis: string | null;
    }>;

    type CreateParams = CommonType.RecordNullable<
      Pick<
        Entity,
        | 'code'
        | 'defaultTaxRate'
        | 'imageUrl'
        | 'invoiceGoodsShortName'
        | 'isTaxExempt'
        | 'name'
        | 'parentId'
        | 'remark'
        | 'sort'
        | 'status'
        | 'taxCategoryCode'
        | 'taxCategoryName'
        | 'taxPolicyBasis'
      >
    >;

    type UpdateParams = CreateParams & { id: string };

    type SearchParams = CommonType.RecordNullable<
      Api.Base.SearchParams & {
        parentId?: string | null;
        taxCategoryCode?: string | null;
      }
    >;

    type List = Common.PaginatingQueryRecord<Entity>;

    type AllEntity = Pick<Entity, 'code' | 'id' | 'name'>;
  }
}
