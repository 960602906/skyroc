namespace Domain.Entities.AfterSales;

/// <summary>
/// 售后原因分类，用于责任分析和售后报表统计。
/// </summary>
public enum AfterSaleReasonType
{
    /// <summary>
    /// 商品未在约定时间送达。
    /// </summary>
    LateDelivery = 1,

    /// <summary>
    /// 订单商品发生漏送。
    /// </summary>
    MissingItem = 2,

    /// <summary>
    /// 实际送达商品与订单不符。
    /// </summary>
    WrongItem = 3,

    /// <summary>
    /// 客户或业务人员下单错误。
    /// </summary>
    OrderingError = 4,

    /// <summary>
    /// 实际交付重量或数量不足。
    /// </summary>
    QuantityMismatch = 5,

    /// <summary>
    /// 商品存在质量问题。
    /// </summary>
    QualityIssue = 6,

    /// <summary>
    /// 商品规格与约定不符。
    /// </summary>
    SpecificationMismatch = 7,

    /// <summary>
    /// 配送过程中商品丢失或损坏。
    /// </summary>
    DriverLossOrDamage = 8,

    /// <summary>
    /// 市场缺货且商品尚未出库。
    /// </summary>
    MarketOutOfStock = 9,

    /// <summary>
    /// 系统处理异常造成业务差错。
    /// </summary>
    SystemIssue = 10,

    /// <summary>
    /// 采购环节造成业务差错。
    /// </summary>
    PurchaseIssue = 11,

    /// <summary>
    /// 客观条件导致无法完成配送。
    /// </summary>
    UnableToDeliver = 12,

    /// <summary>
    /// 不属于既有分类的其他原因。
    /// </summary>
    Other = 13
}
