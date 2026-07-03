using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace SkyRoc.Tests.Architecture;

public class DatabaseCommentTests
{
    [Fact]
    public void PersistedModel_HasCommentsForEveryTableAndColumn()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=skyroc_model_tests;Username=test;Password=test")
            .Options;
        using var context = new ApplicationDbContext(options);

        var model = context.GetService<IDesignTimeModel>().Model;

        foreach (var entityType in model.GetEntityTypes())
        {
            Assert.False(string.IsNullOrWhiteSpace(entityType.GetComment()), $"{entityType.ClrType.Name} 缺少表注释。");

            foreach (var property in entityType.GetProperties())
            {
                Assert.False(
                    string.IsNullOrWhiteSpace(property.GetComment()),
                    $"{entityType.ClrType.Name}.{property.Name} 缺少列注释。");
            }
        }
    }
}
