namespace Domain.Entities.AfterSales;

/// <summary>
/// 售后处理方式，描述审核通过后采取的业务补救措施。
/// </summary>
public enum AfterSaleHandleType
{
    /// <summary>
    /// 对问题商品执行金额减免。
    /// </summary>
    GoodsDiscount = 1,

    /// <summary>
    /// 向客户补送缺少或问题商品。
    /// </summary>
    Replenishment = 2,

    /// <summary>
    /// 回收问题商品并更换合格商品。
    /// </summary>
    Exchange = 3,

    /// <summary>
    /// 将差异纳入客户账单核算。
    /// </summary>
    BillAdjustment = 4,

    /// <summary>
    /// 通过客户沟通完成处理，不产生库存或金额动作。
    /// </summary>
    CustomerCommunication = 5,

    /// <summary>
    /// 使用既有分类无法表达的其他处理方式。
    /// </summary>
    Other = 6
}
