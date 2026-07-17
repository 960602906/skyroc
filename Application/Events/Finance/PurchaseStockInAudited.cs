using Application.Events;
using Domain.Entities.Storage;

namespace Application.Events.Finance;

/// <summary>
///     采购入库已审核，需同步供应商应付账单。
/// </summary>
/// <param name="Order">已审核的采购入库单。</param>
public sealed record PurchaseStockInAudited(StockInOrder Order) : IApplicationEvent;
