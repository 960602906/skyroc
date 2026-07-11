using Application.DTOs.Files;
using Application.Exceptions;
using Application.Services;
using Application.Mappers;
using AutoMapper;
using Domain.Entities.Files;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Shared.Common;
using Xunit;

namespace SkyRoc.Tests.Files;

/// <summary>
/// 验证受保护文件存储服务的内容校验、物理隔离和创建人访问边界。
/// </summary>
public class FileStorageServiceTests : IDisposable
{
    private readonly string _storageRoot = Path.Combine(Path.GetTempPath(), $"skyroc-file-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task UploadAsync_ValidPdf_PersistsMetadataAndStoresOutsidePublicRoot()
    {
        await using var context = CreateDbContext();
        var service = CreateService(context, TestCurrentUserService.Owner, _storageRoot);
        var payload = "%PDF-1.7\n安全检测报告"u8.ToArray();

        var result = await service.UploadAsync(new FileUploadRequest
        {
            FileName = "检测报告.pdf",
            ContentType = "application/pdf",
            Length = payload.Length,
            Content = new MemoryStream(payload)
        });

        var stored = await context.StoredFiles.SingleAsync();
        Assert.Equal("检测报告.pdf", result.FileName);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal(payload.Length, result.Size);
        Assert.Equal(TestCurrentUserService.OwnerId, stored.CreateBy);
        Assert.DoesNotContain("检测报告", stored.StorageKey, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(_storageRoot, stored.StorageKey.Replace('/', Path.DirectorySeparatorChar))));
    }

    [Fact]
    public async Task UploadAsync_PdfExtensionWithNonPdfPayload_ThrowsBusinessExceptionWithoutWritingMetadataOrFile()
    {
        await using var context = CreateDbContext();
        var service = CreateService(context, TestCurrentUserService.Owner, _storageRoot);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.UploadAsync(new FileUploadRequest
        {
            FileName = "伪装报告.pdf",
            ContentType = "application/pdf",
            Length = 4,
            Content = new MemoryStream([0x89, 0x50, 0x4E, 0x47])
        }));

        Assert.Contains("文件内容", exception.Message);
        Assert.Empty(context.StoredFiles);
        Assert.False(Directory.Exists(_storageRoot));
    }

    [Fact]
    public async Task UploadAsync_FileLargerThanTenMiB_ThrowsBusinessExceptionBeforeReadingOrPersisting()
    {
        await using var context = CreateDbContext();
        var service = CreateService(context, TestCurrentUserService.Owner, _storageRoot);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.UploadAsync(new FileUploadRequest
        {
            FileName = "oversized.pdf",
            ContentType = "application/pdf",
            Length = FileStorageOptions.MaxUploadSizeBytes + 1,
            Content = Stream.Null
        }));

        Assert.Contains("10 MiB", exception.Message);
        Assert.Empty(context.StoredFiles);
    }

    [Fact]
    public async Task UploadAsync_ActualStreamLargerThanTenMiB_StopsAtLimitWithoutPersisting()
    {
        await using var context = CreateDbContext();
        var service = CreateService(context, TestCurrentUserService.Owner, _storageRoot);
        var payload = new byte[FileStorageOptions.MaxUploadSizeBytes + 1];
        "%PDF-1.7"u8.CopyTo(payload);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.UploadAsync(new FileUploadRequest
        {
            FileName = "actual-oversized.pdf",
            ContentType = "application/pdf",
            Length = 1,
            Content = new MemoryStream(payload)
        }));

        Assert.Contains("超过 10 MiB", exception.Message);
        Assert.Empty(context.StoredFiles);
    }

    [Theory]
    [MemberData(nameof(ValidImageFiles))]
    public async Task UploadAsync_ValidImageSignature_PersistsVerifiedMimeType(string fileName, string contentType, byte[] payload)
    {
        await using var context = CreateDbContext();
        var service = CreateService(context, TestCurrentUserService.Owner, _storageRoot);

        var result = await service.UploadAsync(new FileUploadRequest
        {
            FileName = fileName,
            ContentType = contentType,
            Length = payload.Length,
            Content = new MemoryStream(payload)
        });

        Assert.Equal(contentType, result.ContentType);
        Assert.Single(context.StoredFiles);
    }

    [Fact]
    public async Task UploadAsync_MismatchedDeclaredMimeType_ThrowsBusinessException()
    {
        await using var context = CreateDbContext();
        var service = CreateService(context, TestCurrentUserService.Owner, _storageRoot);
        var payload = "%PDF-1.7"u8.ToArray();

        var exception = await Assert.ThrowsAsync<BusinessException>(() => service.UploadAsync(new FileUploadRequest
        {
            FileName = "report.pdf",
            ContentType = "application/octet-stream",
            Length = payload.Length,
            Content = new MemoryStream(payload)
        }));

        Assert.Contains("Content-Type", exception.Message);
    }

    [Fact]
    public async Task UploadAsync_MismatchedExtension_ThrowsBusinessException()
    {
        await using var context = CreateDbContext();
        var service = CreateService(context, TestCurrentUserService.Owner, _storageRoot);
        var payload = "%PDF-1.7"u8.ToArray();

        await Assert.ThrowsAsync<BusinessException>(() => service.UploadAsync(new FileUploadRequest
        {
            FileName = "report.png",
            ContentType = "image/png",
            Length = payload.Length,
            Content = new MemoryStream(payload)
        }));
    }

    [Fact]
    public async Task UploadAsync_FileNameContainsBackslash_ThrowsBusinessException()
    {
        await using var context = CreateDbContext();
        var service = CreateService(context, TestCurrentUserService.Owner, _storageRoot);
        var payload = "%PDF-1.7"u8.ToArray();

        await Assert.ThrowsAsync<BusinessException>(() => service.UploadAsync(new FileUploadRequest
        {
            FileName = "folder\\report.pdf",
            ContentType = "application/pdf",
            Length = payload.Length,
            Content = new MemoryStream(payload)
        }));
    }

    [Fact]
    public async Task DownloadAsync_FileCreatedByAnotherUser_ThrowsNotFoundException()
    {
        await using var context = CreateDbContext();
        var ownerService = CreateService(context, TestCurrentUserService.Owner, _storageRoot);
        var payload = "%PDF-1.7\nprivate"u8.ToArray();
        var stored = await ownerService.UploadAsync(new FileUploadRequest
        {
            FileName = "private.pdf",
            ContentType = "application/pdf",
            Length = payload.Length,
            Content = new MemoryStream(payload)
        });
        var otherUserService = CreateService(context, TestCurrentUserService.Other, _storageRoot);

        await Assert.ThrowsAsync<NotFoundException>(() => otherUserService.DownloadAsync(stored.Id));
    }

    [Fact]
    public async Task DownloadAsync_MetadataExistsButPhysicalFileMissing_ThrowsNotFoundException()
    {
        await using var context = CreateDbContext();
        var stored = new StoredFile
        {
            Id = Guid.NewGuid(),
            StorageKey = "2026/07/missing.pdf",
            OriginalFileName = "missing.pdf",
            ContentType = "application/pdf",
            FileSize = 8,
            CreateBy = TestCurrentUserService.OwnerId
        };
        await context.StoredFiles.AddAsync(stored);
        await context.SaveChangesAsync();
        var service = CreateService(context, TestCurrentUserService.Owner, _storageRoot);

        await Assert.ThrowsAsync<NotFoundException>(() => service.DownloadAsync(stored.Id));
    }

    [Fact]
    public async Task UploadAsync_MetadataSaveFails_DeletesWrittenPhysicalFile()
    {
        await using var context = CreateDbContext();
        var service = CreateService(context, TestCurrentUserService.Owner, _storageRoot, new ThrowingUnitOfWork());
        var payload = "%PDF-1.7"u8.ToArray();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadAsync(new FileUploadRequest
        {
            FileName = "rollback.pdf",
            ContentType = "application/pdf",
            Length = payload.Length,
            Content = new MemoryStream(payload)
        }));

        Assert.Empty(Directory.Exists(_storageRoot)
            ? Directory.EnumerateFiles(_storageRoot, "*", SearchOption.AllDirectories)
            : []);
        Assert.Empty(context.StoredFiles);
    }

    [Fact]
    public void Constructor_StorageRootUnderWebRoot_ThrowsInvalidOperationException()
    {
        using var context = CreateDbContext();
        var webRoot = Path.Combine(_storageRoot, "wwwroot");

        Assert.Throws<InvalidOperationException>(() => CreateService(
            context,
            TestCurrentUserService.Owner,
            Path.Combine(webRoot, "uploads"),
            webRoot: webRoot));
    }

    [Fact]
    public void Constructor_StorageRootOnDifferentWindowsDrive_DoesNotTreatItAsUnderWebRoot()
    {
        if (!OperatingSystem.IsWindows()) return;

        using var context = CreateDbContext();
        var webRoot = @"C:\site\wwwroot";
        var storageRoot = @"D:\skyroc-uploads";

        var service = CreateService(context, TestCurrentUserService.Owner, storageRoot, webRoot: webRoot);

        Assert.NotNull(service);
    }

    [Fact]
    public void StoredFileModel_RequiresCreatorOwnership()
    {
        using var context = CreateDbContext();

        var createBy = context.Model.FindEntityType(typeof(StoredFile))!
            .FindProperty(nameof(StoredFile.CreateBy))!;

        Assert.False(createBy.IsNullable);
    }

    public static IEnumerable<object[]> ValidImageFiles =>
    [
        ["report.png", "image/png", new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0 }],
        ["report.jpeg", "image/jpeg", new byte[] { 255, 216, 255, 0 }]
    ];

    public void Dispose()
    {
        if (Directory.Exists(_storageRoot)) Directory.Delete(_storageRoot, recursive: true);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static FileStorageService CreateService(
        ApplicationDbContext context,
        TestCurrentUserService currentUserService,
        string root,
        IUnitOfWork? unitOfWork = null,
        string? webRoot = null)
    {
        return new FileStorageService(
            new StoredFileRepository(context),
            unitOfWork ?? new UnitOfWork(context),
            currentUserService,
            new MapperConfiguration(config => config.AddProfile<FileMappingProfile>()).CreateMapper(),
            Microsoft.Extensions.Options.Options.Create(new FileStorageOptions { StorageRoot = root }),
            new TestFileStoragePathProvider { ContentRootPath = Path.GetTempPath(), WebRootPath = webRoot });
    }

    private sealed class TestCurrentUserService(Guid userId) : Application.interfaces.ICurrentUserService
    {
        public static readonly Guid OwnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        public static readonly TestCurrentUserService Owner = new(OwnerId);
        public static readonly TestCurrentUserService Other = new(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        public Guid? GetUserId() => userId;
        public string? GetUserName() => "file-storage-test";
        public string? GetEmail() => null;
        public string? GetRole() => "admin";
        public IReadOnlyList<string> GetRoles() => ["admin"];
        public bool HasClaim(string claimType, string claimValue) => false;
    }

    private sealed class TestFileStoragePathProvider : Application.interfaces.IFileStoragePathProvider
    {
        public string ContentRootPath { get; set; } = string.Empty;
        public string? WebRootPath { get; set; }
    }

    private sealed class ThrowingUnitOfWork : IUnitOfWork
    {
        public bool HasActiveTransaction => false;
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => throw new InvalidOperationException("模拟元数据保存失败");
        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<int> ExecuteSqlAsync(string sql, params object[] parameters) => Task.FromResult(0);
        public Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default) => action();
        public Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default) => action();
        public void ClearChangeTracking() { }
    }
}
