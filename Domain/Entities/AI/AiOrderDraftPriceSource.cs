namespace Domain.Entities.AI;

/// <summary>
/// AI 订单草稿商品价格的解析来源。
/// </summary>
public enum AiOrderDraftPriceSource
{
    /// <summary>
    /// 尚未解析到唯一有效价格，草稿不能确认。
    /// </summary>
    Unresolved = 1,

    /// <summary>
    /// 来自匹配客户、商品、单位和订单日期的客户协议价。
    /// </summary>
    CustomerProtocol = 2,

    /// <summary>
    /// 来自客户绑定的唯一有效默认报价。
    /// </summary>
    DefaultQuotation = 3,

    /// <summary>
    /// 来自用户在当前请求中明确提供的价格。
    /// </summary>
    UserProvided = 4
}
