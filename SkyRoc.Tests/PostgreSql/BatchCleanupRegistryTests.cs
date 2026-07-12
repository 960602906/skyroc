using Domain.Entities.Customers;
using SkyRoc.Tests.Testing.PostgreSql;
using Xunit;

namespace SkyRoc.Tests.PostgreSql;

/// <summary>
///     验证跨连接临时数据只能按本轮批次精确登记，并按外键依赖逆序清理。
/// </summary>
public class BatchCleanupRegistryTests
{
    /// <summary>
    ///     后登记的子记录应先于父记录进入清理顺序。
    /// </summary>
    [Fact]
    public void GetCleanupOrder_ReturnsEntriesInReverseRegistrationOrder()
    {
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);
        var companyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        registry.Register<Company>(companyId, nameof(Company.Code), $"{batch.Id}-COMPANY");
        registry.Register<Customer>(customerId, nameof(Customer.Code), $"{batch.Id}-CUSTOMER");

        var cleanupOrder = registry.GetCleanupOrder();

        Assert.Collection(
            cleanupOrder,
            entry => Assert.Equal(customerId, entry.EntityId),
            entry => Assert.Equal(companyId, entry.EntityId));
    }

    /// <summary>
    ///     不带当前批次前缀的归属值不得登记，避免清理长期联调数据。
    /// </summary>
    [Fact]
    public void Register_RejectsOwnershipValueOutsideCurrentBatch()
    {
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            registry.Register<Company>(Guid.NewGuid(), nameof(Company.Code), "SKYROC-DEMO-COMPANY-001"));

        Assert.Contains(batch.Id, exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     同一实体主键不得重复登记，防止错误清理计划掩盖调用方缺陷。
    /// </summary>
    [Fact]
    public void Register_RejectsDuplicateEntityRegistration()
    {
        var batch = TestBatchContext.Create();
        var registry = new BatchCleanupRegistry(batch);
        var entityId = Guid.NewGuid();
        var code = $"{batch.Id}-COMPANY";
        registry.Register<Company>(entityId, nameof(Company.Code), code);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            registry.Register<Company>(entityId, nameof(Company.Code), code));

        Assert.Contains(entityId.ToString(), exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     每轮批次标识必须唯一且保持可用于业务编码的稳定格式。
    /// </summary>
    [Fact]
    public void Create_ProducesUniqueSafeBatchIdentifiers()
    {
        var first = TestBatchContext.Create();
        var second = TestBatchContext.Create();

        Assert.StartsWith(TestBatchContext.Prefix, first.Id, StringComparison.Ordinal);
        Assert.NotEqual(first.Id, second.Id);
        Assert.Matches("^[A-Z0-9-]+$", first.Id);
        Assert.InRange(first.Id.Length, 20, 40);
    }
}
