using Application.Events;
using Domain.Entities.Storage;

namespace Application.Events.Finance;

/// <summary>
///     采购退货出库已审核，需同步供应商应付账单。
/// </summary>
/// <param name="Order">已审核的采购退货出库单。</param>
public sealed record PurchaseReturnStockOutAudited(StockOutOrder Order) : IApplicationEvent;
