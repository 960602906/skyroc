namespace Domain.Entities.Purchases;

/// <summary>
/// 采购单执行状态，控制采购单是否仍可编辑以及能否被后续入库流程引用。
/// </summary>
public enum PurchaseOrderStatus
{
    /// <summary>
    /// 未完成草稿，可继续维护采购商品、价格和预计到货时间。
    /// </summary>
    Draft = 1,

    /// <summary>
    /// 已完成，采购内容已确认并可供采购入库引用。
    /// </summary>
    Completed = 2,

    /// <summary>
    /// 已取消，不再执行且不得生成新的采购入库单。
    /// </summary>
    Cancelled = 3
}
