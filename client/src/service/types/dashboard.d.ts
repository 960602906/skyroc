declare namespace Api {
  namespace Dashboard {
    /** 首页驾驶舱的 UTC 统计周期；起止时间均为包含边界。 */
    type SearchParams = {
      dateEnd?: string | null;
      dateStart?: string | null;
      rankSize?: number | null;
    };

    /** 统计周期内已签收订单的经营概览。 */
    type Brief = {
      customerCount: number;
      orderCount: number;
      saleAmount: number;
    };

    /** 按订单业务日汇总的销售趋势。 */
    type SalesTrend = {
      customerCount: number;
      orderCount: number;
      reportDate: string;
      saleAmount: number;
    };

    /** 按客户验收销售额降序排列的销售排行。 */
    type CustomerSalesRank = {
      customerId: string;
      customerName: string;
      orderCount: number;
      saleAmount: number;
    };

    /** 按商品分类验收销售额降序排列的销售排行。 */
    type GoodsTypeSalesRank = {
      goodsTypeName: string;
      orderCount: number;
      saleAmount: number;
    };

    /** 按客户账单业务日期汇总的应收、已结与待结金额。 */
    type Reconciliation = {
      billCount: number;
      pendingAmount: number;
      receivableAmount: number;
      settledAmount: number;
    };

    /** 取货任务当前履约状态对应的任务数量。 */
    type PickupStatusSummary = {
      pickupStatus: Api.AfterSale.PickupStatus;
      taskCount: number;
    };
  }
}
