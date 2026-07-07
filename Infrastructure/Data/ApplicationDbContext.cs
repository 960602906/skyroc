using System.Reflection;
using Domain.Entities;
using Domain.Entities.AfterSales;
using Domain.Entities.Customers;
using Domain.Entities.Delivery;
using Domain.Entities.Finance;
using Domain.Entities.Goods;
using Domain.Entities.Orders;
using Domain.Entities.Pricing;
using Domain.Entities.Purchases;
using Domain.Entities.Storage;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data.EntityConfigurations;

namespace Infrastructure.Data;

/// <summary>
///     应用数据库上下文
/// </summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    /// <summary>
    ///     配置模型
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        modelBuilder.ApplyDatabaseComments();
        // ⭐ 应用所有配置
        // modelBuilder.ApplyConfiguration(new UserConfiguration());
        // modelBuilder.ApplyConfiguration(new RoleConfiguration());
        // modelBuilder.ApplyConfiguration(new MenuConfiguration());
        // modelBuilder.ApplyConfiguration(new UserRoleConfiguration());
        // modelBuilder.ApplyConfiguration(new RoleMenuConfiguration());

        //添加种子数据
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        //自动更新时间字段
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            switch (entry.State)
            {
                case EntityState.Modified:
                    {
                        entry.Entity.UpdateTime = DateTime.UtcNow;
                        break;
                    }
                case EntityState.Added:
                    entry.Entity.CreateTime = DateTime.UtcNow;
                    break;
            }

        return base.SaveChangesAsync(cancellationToken);
    }

    #region DbSets

    /// <summary>
    ///     用户表
    /// </summary>
    public DbSet<User> Users { get; set; }

    /// <summary>
    ///     角色表
    /// </summary>
    public DbSet<Role> Roles { get; set; }

    /// <summary>
    ///     菜单表
    /// </summary>
    public DbSet<Menu> Menus { get; set; }

    /// <summary>
    ///  菜单按钮表
    /// </summary>
    public DbSet<MenuButton> MenuButtons { get; set; }

    /// <summary>
    ///     用户角色关联表
    /// </summary>
    public DbSet<UserRole> UserRoles { get; set; }

    /// <summary>
    ///     角色菜单关联表
    /// </summary>
    public DbSet<RoleMenu> RoleMenus { get; set; }

    /// <summary>
    ///  部门表
    /// </summary>
    public DbSet<Department> Departments { get; set; }

    /// <summary>
    ///  操作日志表
    /// </summary>
    public DbSet<OperationLog> OperationLogs { get; set; }

    /// <summary>
    /// 商品分类表
    /// </summary>
    public DbSet<GoodsType> GoodsTypes { get; set; }

    /// <summary>
    /// 商品档案表
    /// </summary>
    public DbSet<Goods> Goods { get; set; }

    /// <summary>
    /// 商品单位表
    /// </summary>
    public DbSet<GoodsUnit> GoodsUnits { get; set; }

    /// <summary>
    /// 商品图片表
    /// </summary>
    public DbSet<GoodsImage> GoodsImages { get; set; }

    /// <summary>
    /// 商品供应商关系表
    /// </summary>
    public DbSet<GoodsSupplierRelation> GoodsSupplierRelations { get; set; }

    /// <summary>
    /// 供应商表
    /// </summary>
    public DbSet<Supplier> Suppliers { get; set; }

    /// <summary>
    /// 采购员表
    /// </summary>
    public DbSet<Purchaser> Purchasers { get; set; }

    /// <summary>
    /// 仓库表
    /// </summary>
    public DbSet<Ware> Wares { get; set; }

    /// <summary>
    /// 公司表
    /// </summary>
    public DbSet<Company> Companies { get; set; }

    /// <summary>
    /// 客户表
    /// </summary>
    public DbSet<Customer> Customers { get; set; }

    /// <summary>
    /// 客户标签表
    /// </summary>
    public DbSet<CustomerTag> CustomerTags { get; set; }

    /// <summary>
    /// 客户标签关系表
    /// </summary>
    public DbSet<CustomerTagRelation> CustomerTagRelations { get; set; }

    /// <summary>
    /// 客户子账号表
    /// </summary>
    public DbSet<CustomerSubAccount> CustomerSubAccounts { get; set; }

    /// <summary>
    /// 报价单表
    /// </summary>
    public DbSet<Quotation> Quotations { get; set; }

    /// <summary>
    /// 报价商品表
    /// </summary>
    public DbSet<QuotationGoods> QuotationGoods { get; set; }

    /// <summary>
    /// 客户报价关系表
    /// </summary>
    public DbSet<CustomerQuotation> CustomerQuotations { get; set; }

    /// <summary>
    /// 客户协议价表
    /// </summary>
    public DbSet<CustomerProtocol> CustomerProtocols { get; set; }

    /// <summary>
    /// 客户协议价商品表
    /// </summary>
    public DbSet<CustomerProtocolGoods> CustomerProtocolGoods { get; set; }

    /// <summary>
    /// 客户协议价客户关系表
    /// </summary>
    public DbSet<CustomerProtocolCustomer> CustomerProtocolCustomers { get; set; }

    /// <summary>
    /// 采购规则表
    /// </summary>
    public DbSet<PurchaseRule> PurchaseRules { get; set; }

    /// <summary>
    /// 采购规则商品关系表
    /// </summary>
    public DbSet<PurchaseRuleGoods> PurchaseRuleGoods { get; set; }

    /// <summary>
    /// 采购规则客户关系表
    /// </summary>
    public DbSet<PurchaseRuleCustomer> PurchaseRuleCustomers { get; set; }

    /// <summary>
    /// 销售订单表
    /// </summary>
    public DbSet<SaleOrder> SaleOrders { get; set; }

    /// <summary>
    /// 销售订单明细表
    /// </summary>
    public DbSet<SaleOrderDetail> SaleOrderDetails { get; set; }

    /// <summary>
    /// 订单审核记录表
    /// </summary>
    public DbSet<OrderAuditLog> OrderAuditLogs { get; set; }

    /// <summary>
    /// 订单签收回单表。
    /// </summary>
    public DbSet<OrderReceipt> OrderReceipts { get; set; }

    /// <summary>
    /// 订单商品验收明细表。
    /// </summary>
    public DbSet<OrderCheckDetail> OrderCheckDetails { get; set; }

    /// <summary>
    /// 采购计划表
    /// </summary>
    public DbSet<PurchasePlan> PurchasePlans { get; set; }

    /// <summary>
    /// 采购计划明细表
    /// </summary>
    public DbSet<PurchasePlanDetail> PurchasePlanDetails { get; set; }

    /// <summary>
    /// 采购计划订单关系表
    /// </summary>
    public DbSet<PurchasePlanOrderRelation> PurchasePlanOrderRelations { get; set; }

    /// <summary>
    /// 采购单表。
    /// </summary>
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }

    /// <summary>
    /// 采购单商品明细表。
    /// </summary>
    public DbSet<PurchaseOrderDetail> PurchaseOrderDetails { get; set; }

    /// <summary>
    /// 采购单明细与采购计划明细关系表。
    /// </summary>
    public DbSet<PurchaseOrderPlanRelation> PurchaseOrderPlanRelations { get; set; }

    /// <summary>
    /// 仓库商品批次库存表。
    /// </summary>
    public DbSet<StockBatch> StockBatches { get; set; }

    /// <summary>
    /// 库存增减流水表。
    /// </summary>
    public DbSet<StockLedger> StockLedgers { get; set; }

    /// <summary>
    /// 入库主单表。
    /// </summary>
    public DbSet<StockInOrder> StockInOrders { get; set; }

    /// <summary>
    /// 入库商品明细表。
    /// </summary>
    public DbSet<StockInDetail> StockInDetails { get; set; }

    /// <summary>
    /// 出库主单表。
    /// </summary>
    public DbSet<StockOutOrder> StockOutOrders { get; set; }

    /// <summary>
    /// 出库商品明细表。
    /// </summary>
    public DbSet<StockOutDetail> StockOutDetails { get; set; }

    /// <summary>
    /// 库存盘点主单表。
    /// </summary>
    public DbSet<StocktakingOrder> StocktakingOrders { get; set; }

    /// <summary>
    /// 库存盘点批次明细表。
    /// </summary>
    public DbSet<StocktakingDetail> StocktakingDetails { get; set; }

    /// <summary>
    /// 承运商表。
    /// </summary>
    public DbSet<Carrier> Carriers { get; set; }

    /// <summary>
    /// 司机表。
    /// </summary>
    public DbSet<Driver> Drivers { get; set; }

    /// <summary>
    /// 配送路线表。
    /// </summary>
    public DbSet<DeliveryRoute> DeliveryRoutes { get; set; }

    /// <summary>
    /// 客户路线关系表。
    /// </summary>
    public DbSet<CustomerRoute> CustomerRoutes { get; set; }

    /// <summary>
    /// 配送异常表。
    /// </summary>
    public DbSet<DeliveryException> DeliveryExceptions { get; set; }

    /// <summary>
    /// 配送任务表。
    /// </summary>
    public DbSet<DeliveryTask> DeliveryTasks { get; set; }

    /// <summary>
    /// 售后单主表。
    /// </summary>
    public DbSet<AfterSale> AfterSales { get; set; }

    /// <summary>
    /// 售后商品明细表。
    /// </summary>
    public DbSet<AfterSaleGoods> AfterSaleGoods { get; set; }

    /// <summary>
    /// 售后审核记录表。
    /// </summary>
    public DbSet<AfterSaleAuditLog> AfterSaleAuditLogs { get; set; }

    /// <summary>
    /// 售后取货任务表。
    /// </summary>
    public DbSet<PickupTask> PickupTasks { get; set; }

    /// <summary>
    /// 客户账单主表。
    /// </summary>
    public DbSet<CustomerBill> CustomerBills { get; set; }

    /// <summary>
    /// 客户账单明细表。
    /// </summary>
    public DbSet<CustomerBillDetail> CustomerBillDetails { get; set; }

    /// <summary>
    /// 客户结款凭证主表。
    /// </summary>
    public DbSet<CustomerSettlement> CustomerSettlements { get; set; }

    /// <summary>
    /// 客户结款凭证明细表。
    /// </summary>
    public DbSet<CustomerSettlementDetail> CustomerSettlementDetails { get; set; }

    /// <summary>
    /// 供应商待结单据主表。
    /// </summary>
    public DbSet<SupplierBill> SupplierBills { get; set; }

    /// <summary>
    /// 供应商待结单据明细表。
    /// </summary>
    public DbSet<SupplierBillDetail> SupplierBillDetails { get; set; }

    /// <summary>
    /// 供应商结算单主表。
    /// </summary>
    public DbSet<SupplierSettlement> SupplierSettlements { get; set; }

    /// <summary>
    /// 供应商结算单明细表。
    /// </summary>
    public DbSet<SupplierSettlementDetail> SupplierSettlementDetails { get; set; }

    #endregion
}
