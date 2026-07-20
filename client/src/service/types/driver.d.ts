declare namespace Api {
  namespace Driver {
    /** 配送或取货任务可分配的司机档案。 */
    type Entity = Common.CommonRecord<{
      carrierId: string | null;
      carrierName: string | null;
      code: string | null;
      licenseNo: string | null;
      name: string | null;
      phone: string | null;
      plateNumber: string | null;
      remark: string | null;
    }>;

    type AllEntity = Pick<Entity, 'id' | 'name' | 'phone' | 'status'>;

    type Payload = Record<string, unknown>;

    type SearchParams = CommonType.RecordNullable<Api.Base.SearchParams & Record<string, unknown>>;

    type List = Common.PaginatingQueryRecord<Entity>;

    type Result = unknown;
  }
}
