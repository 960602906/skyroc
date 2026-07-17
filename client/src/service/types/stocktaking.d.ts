declare namespace Api {
  namespace Stocktaking {
    /** 业务实体（字段随后端 DTO 演进，页面接入时再细化） */
    type Entity = Common.CommonRecord<Record<string, unknown>>;

    type AllEntity = Pick<Entity, 'id'> & Record<string, unknown>;

    type Payload = Record<string, unknown>;

    type SearchParams = CommonType.RecordNullable<Api.Base.SearchParams & Record<string, unknown>>;

    type List = Common.PaginatingQueryRecord<Entity>;

    type Result = unknown;
  }
}
