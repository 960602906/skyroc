namespace Domain.Entities.Storage;

/// <summary>
/// 库存业务单据状态，控制单据编辑、审核、库存生效和反审核回滚。
/// </summary>
public enum StockDocumentStatus
{
    /// <summary>
    /// 已删除，仅保留历史记录且不得参与库存计算。
    /// </summary>
    Deleted = -1,

    /// <summary>
    /// 待提交草稿，可继续维护主单和商品明细。
    /// </summary>
    Draft = 1,

    /// <summary>
    /// 待审核，内容已提交且暂不允许普通编辑。
    /// </summary>
    PendingAudit = 2,

    /// <summary>
    /// 已审核，单据对应的库存变更已经生效。
    /// </summary>
    Audited = 3,

    /// <summary>
    /// 已反审核，原库存变更已通过反向流水回滚。
    /// </summary>
    Reversed = 4
}
