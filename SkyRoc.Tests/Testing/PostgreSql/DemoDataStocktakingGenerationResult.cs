namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     库存盘点长期联调数据本轮创建与安全复用数量。
/// </summary>
internal sealed record DemoDataStocktakingGenerationResult(
    int CreatedOrders,
    int ReusedOrders,
    int CreatedDetails,
    int ReusedDetails,
    int CreatedLedgers,
    int ReusedLedgers);
