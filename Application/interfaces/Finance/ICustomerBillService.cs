using Domain.Entities.AfterSales;
using Domain.Entities.Finance;
using Domain.Entities.Orders;

namespace Application.interfaces;

/// <summary>
/// 客户账单同步服务，将订单签收和售后完成事实幂等转换为客户应收账单。
/// </summary>
public interface ICustomerBillService
{
    /// <summary>
    /// 根据已完成签收的销售订单同步订单应收明细；重复调用会重建订单验收行并保留售后调整行。
    /// </summary>
    /// <param name="saleOrder">已锁定并包含商品明细的销售订单。</param>
    /// <returns>同步后的客户账单聚合。</returns>
    Task<CustomerBill> SyncOrderAcceptanceAsync(SaleOrder saleOrder);

    /// <summary>
    /// 根据已完成售后单追加应收调整明细；重复调用不会重复冲减同一售后商品。
    /// </summary>
    /// <param name="afterSale">已锁定并包含售后商品的售后单。</param>
    /// <returns>同步后的客户账单；无来源订单的售后返回 <c>null</c>。</returns>
    Task<CustomerBill?> ApplyAfterSaleAdjustmentAsync(AfterSale afterSale);
}
