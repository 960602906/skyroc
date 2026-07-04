namespace Domain.Entities.Storage;

/// <summary>
/// 库存流水增减方向；流水数量始终保存正数，由方向决定余额变化符号。
/// </summary>
public enum StockLedgerDirection
{
    /// <summary>
    /// 增加批次库存。
    /// </summary>
    Increase = 1,

    /// <summary>
    /// 减少批次库存。
    /// </summary>
    Decrease = 2
}
