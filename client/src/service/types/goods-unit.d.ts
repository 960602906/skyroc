declare namespace Api {
  namespace GoodsUnit {
    type Entity = Common.CommonRecord<{
      code: string | null;
      conversionRate: number;
      goodsCode: string | null;
      goodsId: string;
      goodsName: string | null;
      isBaseUnit: boolean;
      name: string | null;
      remark: string | null;
      sort: number;
    }>;

    type CreateParams = CommonType.RecordNullable<
      Pick<Entity, 'code' | 'conversionRate' | 'goodsId' | 'isBaseUnit' | 'name' | 'remark' | 'sort' | 'status'>
    >;

    type UpdateParams = CreateParams & { id: string };

    type SearchParams = CommonType.RecordNullable<
      Pick<Common.CommonRecord, 'status'> &
        Common.CommonSearchParams & {
          goodsId?: string | null;
          name?: string | null;
        }
    >;

    type List = Common.PaginatingQueryRecord<Entity>;

    type AllEntity = Pick<Entity, 'goodsId' | 'id' | 'name'>;
  }
}
