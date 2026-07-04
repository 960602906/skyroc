namespace Domain.Entities.Storage;

/// <summary>
/// 库存单据打印状态，仅表示是否至少完成过一次正式打印。
/// </summary>
public enum StockPrintStatus
{
    /// <summary>
    /// 尚未打印。
    /// </summary>
    NotPrinted = 0,

    /// <summary>
    /// 已完成过正式打印。
    /// </summary>
    Printed = 1
}
