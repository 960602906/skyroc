namespace SkyRoc.Tests.Testing.PostgreSql;

/// <summary>
///     汇总一次安全文件与商品图片联调数据生成的新增和复用数量。
/// </summary>
public sealed record DemoDataFileGenerationResult(
    int CreatedStoredFiles,
    int ReusedStoredFiles,
    int CreatedGoodsImages,
    int ReusedGoodsImages);
