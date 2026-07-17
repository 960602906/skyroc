namespace Shared.Constants;

/// <summary>
/// 业务单据编号种类。每种对应固定前缀与生成规则，由 <c>IDocumentNoGenerator</c> 统一发号。
/// </summary>
public enum DocumentNoKind
{
    /// <summary>销售订单，前缀 SO。</summary>
    SaleOrder = 1,

    /// <summary>采购计划，前缀 PP。</summary>
    PurchasePlan = 2,

    /// <summary>采购单，前缀 PO。</summary>
    PurchaseOrder = 3,

    /// <summary>入库单，前缀 IN。</summary>
    StockIn = 4,

    /// <summary>出库单，前缀 OUT。</summary>
    StockOut = 5,

    /// <summary>库存盘点单，前缀 STK。</summary>
    Stocktaking = 6,

    /// <summary>配送任务，前缀 DT。</summary>
    DeliveryTask = 7,

    /// <summary>签收回单，前缀 OR。</summary>
    OrderReceipt = 8,

    /// <summary>配送异常，前缀 DE。</summary>
    DeliveryException = 9,

    /// <summary>售后单，前缀 AS。</summary>
    AfterSale = 10,

    /// <summary>售后取货任务，前缀 PU。</summary>
    PickupTask = 11,

    /// <summary>客户账单，前缀 CB。</summary>
    CustomerBill = 12,

    /// <summary>供应商待结单据，前缀 SB。</summary>
    SupplierBill = 13,

    /// <summary>客户结款凭证，前缀 CS。</summary>
    CustomerSettlement = 14,

    /// <summary>供应商结算单，前缀 SS。</summary>
    SupplierSettlement = 15,

    /// <summary>检测报告，前缀 IR。</summary>
    InspectionReport = 16,

    /// <summary>溯源记录，前缀 TR。</summary>
    TraceRecord = 17,

    /// <summary>导入导出任务，前缀 IE。</summary>
    ImportExportJob = 18
}
