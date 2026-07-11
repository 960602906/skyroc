namespace Shared.Constants;

/// <summary>
///     API 权限编码。
///     编码格式固定为 module:resource:action，已发布编码不得复用或改变含义。
/// </summary>
public static class PermissionCodes
{
    public const string All = "*:*:*";

    public static class System
    {
        public static class Users
        {
            public const string Read = "system:user:read";
            public const string Create = "system:user:create";
            public const string Update = "system:user:update";
            public const string Delete = "system:user:delete";
            public const string AssignRoles = "system:user:assign-roles";
        }

        public static class Roles
        {
            public const string Read = "system:role:read";
            public const string Create = "system:role:create";
            public const string Update = "system:role:update";
            public const string Delete = "system:role:delete";
            public const string AssignMenus = "system:role:assign-menus";
        }

        public static class Menus
        {
            public const string Read = "system:menu:read";
            public const string Create = "system:menu:create";
            public const string Update = "system:menu:update";
            public const string Delete = "system:menu:delete";
        }

        public static class MenuButtons
        {
            public const string Read = "system:menu-button:read";
            public const string Create = "system:menu-button:create";
            public const string Update = "system:menu-button:update";
            public const string Delete = "system:menu-button:delete";
        }

        public static class Departments
        {
            public const string Read = "system:department:read";
            public const string Create = "system:department:create";
            public const string Update = "system:department:update";
            public const string Delete = "system:department:delete";
        }

        /// <summary>
        /// 打印模板设计、字段定义及其启停维护权限。
        /// </summary>
        public static class PrintTemplates
        {
            public const string Resource = "system:print-template";
            public const string Read = $"{Resource}:read";
            public const string Create = $"{Resource}:create";
            public const string Update = $"{Resource}:update";
            public const string Delete = $"{Resource}:delete";
        }
    }

    public static class Business
    {
        public static class Goods
        {
            public const string Resource = "business:goods";
            public const string Read = $"{Resource}:read";
            public const string Create = $"{Resource}:create";
            public const string Update = $"{Resource}:update";
            public const string Delete = $"{Resource}:delete";
        }

        public static class Customers
        {
            public const string Resource = "business:customer";
            public const string Read = $"{Resource}:read";
            public const string Create = $"{Resource}:create";
            public const string Update = $"{Resource}:update";
            public const string Delete = $"{Resource}:delete";
        }

        public static class Pricing
        {
            public const string Resource = "business:pricing";
            public const string Read = $"{Resource}:read";
            public const string Create = $"{Resource}:create";
            public const string Update = $"{Resource}:update";
            public const string Delete = $"{Resource}:delete";
            public const string Audit = $"{Resource}:audit";
        }

        public static class Purchases
        {
            public const string Resource = "business:purchase";
            public const string Read = $"{Resource}:read";
            public const string Create = $"{Resource}:create";
            public const string Update = $"{Resource}:update";
            public const string Delete = $"{Resource}:delete";
        }

        public static class Storage
        {
            public const string Resource = "business:storage";
            public const string Read = $"{Resource}:read";
            public const string Create = $"{Resource}:create";
            public const string Update = $"{Resource}:update";
            public const string Delete = $"{Resource}:delete";
        }

        public static class Orders
        {
            public const string Resource = "business:order";
            public const string Read = $"{Resource}:read";
            public const string Create = $"{Resource}:create";
            public const string Update = $"{Resource}:update";
            public const string Delete = $"{Resource}:delete";
            public const string Audit = $"{Resource}:audit";
        }

        public static class Delivery
        {
            public const string Resource = "business:delivery";
            public const string Read = $"{Resource}:read";
            public const string Create = $"{Resource}:create";
            public const string Update = $"{Resource}:update";
            public const string Delete = $"{Resource}:delete";
        }

        /// <summary>
        /// 售后单查询、草稿维护和审核状态流转权限。
        /// </summary>
        public static class AfterSales
        {
            public const string Resource = "business:after-sales";
            public const string Read = $"{Resource}:read";
            public const string Create = $"{Resource}:create";
            public const string Update = $"{Resource}:update";
            public const string Delete = $"{Resource}:delete";
            public const string Audit = $"{Resource}:audit";
        }

        /// <summary>
        /// 财务账单、客户结款和后续供应商结算权限。
        /// </summary>
        public static class Finance
        {
            public const string Resource = "business:finance";
            public const string Read = $"{Resource}:read";
            public const string Create = $"{Resource}:create";
            public const string Update = $"{Resource}:update";
            public const string Delete = $"{Resource}:delete";
        }

        /// <summary>
        /// 检测报告维护、商品溯源查询和外部报送结果读取权限。
        /// </summary>
        public static class Traceability
        {
            public const string Resource = "business:traceability";
            public const string Read = $"{Resource}:read";
            public const string Create = $"{Resource}:create";
            public const string Update = $"{Resource}:update";
            public const string Delete = $"{Resource}:delete";
        }

        /// <summary>
        /// 销售、售后、库存和采购等报表查询权限。
        /// </summary>
        public static class Reports
        {
            public const string Resource = "business:report";
            public const string Read = $"{Resource}:read";
        }

        /// <summary>
        /// 统一 CSV 模板、导入导出任务和任务状态查询权限。
        /// </summary>
        public static class ImportExport
        {
            public const string Resource = "business:import-export";
            public const string Read = $"{Resource}:read";
            public const string Create = $"{Resource}:create";
        }

        /// <summary>
        /// 受保护上传文件的创建与创建人下载权限。
        /// </summary>
        public static class Files
        {
            public const string Resource = "business:file";
            public const string Read = $"{Resource}:read";
            public const string Create = $"{Resource}:create";
        }

        /// <summary>
        /// 业务单据打印数据读取和正式打印确认权限。
        /// </summary>
        public static class Printing
        {
            public const string Resource = "business:print";
            public const string Read = $"{Resource}:read";
            public const string Update = $"{Resource}:update";
        }
    }

    public static IReadOnlyCollection<string> Defined { get; } =
    [
        System.Users.Read,
        System.Users.Create,
        System.Users.Update,
        System.Users.Delete,
        System.Users.AssignRoles,
        System.Roles.Read,
        System.Roles.Create,
        System.Roles.Update,
        System.Roles.Delete,
        System.Roles.AssignMenus,
        System.Menus.Read,
        System.Menus.Create,
        System.Menus.Update,
        System.Menus.Delete,
        System.MenuButtons.Read,
        System.MenuButtons.Create,
        System.MenuButtons.Update,
        System.MenuButtons.Delete,
        System.Departments.Read,
        System.Departments.Create,
        System.Departments.Update,
        System.Departments.Delete,
        System.PrintTemplates.Read,
        System.PrintTemplates.Create,
        System.PrintTemplates.Update,
        System.PrintTemplates.Delete,
        Business.Goods.Read,
        Business.Goods.Create,
        Business.Goods.Update,
        Business.Goods.Delete,
        Business.Customers.Read,
        Business.Customers.Create,
        Business.Customers.Update,
        Business.Customers.Delete,
        Business.Pricing.Read,
        Business.Pricing.Create,
        Business.Pricing.Update,
        Business.Pricing.Delete,
        Business.Pricing.Audit,
        Business.Purchases.Read,
        Business.Purchases.Create,
        Business.Purchases.Update,
        Business.Purchases.Delete,
        Business.Storage.Read,
        Business.Storage.Create,
        Business.Storage.Update,
        Business.Storage.Delete,
        Business.Orders.Read,
        Business.Orders.Create,
        Business.Orders.Update,
        Business.Orders.Delete,
        Business.Orders.Audit,
        Business.Delivery.Read,
        Business.Delivery.Create,
        Business.Delivery.Update,
        Business.Delivery.Delete,
        Business.AfterSales.Read,
        Business.AfterSales.Create,
        Business.AfterSales.Update,
        Business.AfterSales.Delete,
        Business.AfterSales.Audit,
        Business.Finance.Read,
        Business.Finance.Create,
        Business.Finance.Update,
        Business.Finance.Delete,
        Business.Traceability.Read,
        Business.Traceability.Create,
        Business.Traceability.Update,
        Business.Traceability.Delete,
        Business.Reports.Read,
        Business.ImportExport.Read,
        Business.ImportExport.Create,
        Business.Files.Read,
        Business.Files.Create,
        Business.Printing.Read,
        Business.Printing.Update
    ];
}
