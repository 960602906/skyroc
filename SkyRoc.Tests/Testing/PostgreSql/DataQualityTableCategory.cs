namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     业务表数量验收分类。
/// </summary>
public enum DataQualityTableCategory
{
    /// <summary>普通业务表。</summary>
    General,
    /// <summary>用户权限与基础资料。</summary>
    PermissionOrBaseData,
    /// <summary>商品、客户、供应商、采购员、仓库与定价主数据。</summary>
    MasterData,
    /// <summary>订单、采购、库存、配送、售后与结算主单。</summary>
    BusinessDocument,
    /// <summary>明细、关系、审计、流水与日志。</summary>
    DetailRelationOrLog,
    /// <summary>固定枚举、单例或唯一键受限表。</summary>
    Constrained
}
