namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     集中登记业务表的数量分级和字段适用性规则，禁止以无理由忽略绕过质量验收。
/// </summary>
public static class DataQualityRuleCatalog
{
    private static readonly HashSet<string> DuplicateBusinessCodeExemptions = ["sys_login_log.username"];

    private static readonly HashSet<string> MasterDataTables = [
        "goods", "goods_type", "goods_unit", "customer", "company", "supplier", "purchaser", "ware", "quotation", "customer_protocol", "purchase_rule"
    ];

    private static readonly HashSet<string> BusinessDocumentTables = [
        "sale_order", "purchase_plan", "purchase_order", "stock_in_order", "stock_out_order", "stocktaking_order", "delivery_task", "after_sale", "customer_bill", "customer_settlement", "supplier_bill", "supplier_settlement", "inspection_report"
    ];

    /// <summary>
    ///     为模型表返回唯一的数量分类；关系、日志与明细优先按 100–300 条验收。
    /// </summary>
    public static DataQualityTableRule CreateRule(MetadataTableInventory table)
    {
        var category = table.TableName switch
        {
            "sys_setting" => DataQualityTableCategory.Constrained,
            _ when BusinessDocumentTables.Contains(table.TableName) => DataQualityTableCategory.BusinessDocument,
            _ when MasterDataTables.Contains(table.TableName) => DataQualityTableCategory.MasterData,
            _ when table.TableName.StartsWith("sys_", StringComparison.Ordinal) => DataQualityTableCategory.PermissionOrBaseData,
            _ when table.TableName.EndsWith("_detail", StringComparison.Ordinal)
                   || table.TableName.EndsWith("_rel", StringComparison.Ordinal)
                   || table.TableName.EndsWith("_log", StringComparison.Ordinal)
                   || table.TableName.Contains("ledger", StringComparison.Ordinal) => DataQualityTableCategory.DetailRelationOrLog,
            _ => DataQualityTableCategory.General
        };

        return category switch
        {
            DataQualityTableCategory.PermissionOrBaseData => new(category, 30, 50, "用户权限和基础资料目标 30–50 条"),
            DataQualityTableCategory.MasterData => new(category, 30, 80, "商品、客户、供应商、采购员、仓库和定价目标 30–80 条"),
            DataQualityTableCategory.BusinessDocument => new(category, 50, 100, "订单、采购、库存、配送、售后和结算主表目标 50–100 条"),
            DataQualityTableCategory.DetailRelationOrLog => new(category, 100, 300, "明细、关系、审计、流水和日志目标 100–300 条"),
            DataQualityTableCategory.Constrained => new(category, 0, null, "固定枚举、单例配置或唯一键受限表必须填满全部合法记录"),
            _ => new(category, 20, null, "普通业务表验收下限为 20 条")
        };
    }

    /// <summary>
    ///     给每个持久化字段赋予可执行的默认适用性，状态操作字段仅在对应状态下要求填写。
    /// </summary>
    public static DataQualityFieldApplicability GetApplicability(string tableName, MetadataColumnInventory column)
    {
        if (!column.IsNullable)
            return DataQualityFieldApplicability.AlwaysRequired;
        if (column.ColumnName.EndsWith("_time", StringComparison.Ordinal)
            || column.ColumnName.EndsWith("_by", StringComparison.Ordinal)
            || column.ColumnName.EndsWith("_remark", StringComparison.Ordinal))
        {
            return DataQualityFieldApplicability.StateConditional;
        }

        return DataQualityFieldApplicability.BusinessConditional;
    }

    /// <summary>
    ///     返回因业务语义允许重复、且已人工复核的业务编码检测例外。
    /// </summary>
    public static IReadOnlyList<string> QualityRuleExceptionDescriptions =>
        ["sys_login_log.username：登录审计按事件追加，同一账号多次登录是合法历史，不作为重复业务编码。"];

    /// <summary>
    ///     判断字段是否属于集中登记的重复业务编码例外，未登记字段一律参加检测。
    /// </summary>
    public static bool IsDuplicateBusinessCodeExempt(string tableName, string columnName)
    {
        return DuplicateBusinessCodeExemptions.Contains($"{tableName}.{columnName}");
    }
}
