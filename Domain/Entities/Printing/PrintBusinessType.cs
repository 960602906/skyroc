namespace Domain.Entities.Printing;

/// <summary>
/// 可配置模板和打印数据所属的业务单据类型。
/// </summary>
public enum PrintBusinessType
{
    /// <summary>销售订单或客户配送单。</summary>
    SaleOrder = 1,

    /// <summary>采购单。</summary>
    PurchaseOrder = 2,

    /// <summary>入库单。</summary>
    StockInOrder = 3,

    /// <summary>出库单。</summary>
    StockOutOrder = 4,

    /// <summary>客户结款凭证。</summary>
    CustomerSettlement = 5,

    /// <summary>供应商结算单。</summary>
    SupplierSettlement = 6
}
